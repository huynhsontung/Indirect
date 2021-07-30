using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using InstagramAPI.Classes.Core;
using InstagramAPI.Classes.Mqtt.Packets;
using InstagramAPI.Fbns;
using InstagramAPI.Fbns.Packets;
using InstagramAPI.Push;
using InstagramAPI.Realtime.Subscriptions;
using InstagramAPI.Realtime.Thrift;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI.Realtime
{
    public class RealtimeClient
    {
        public event EventHandler<List<MessageSyncEventArgs>> MessageReceived;
        public event EventHandler<PubsubEventArgs> ActivityIndicatorChanged;
        public event EventHandler<UserPresenceEventArgs> UserPresenceChanged;
        public event EventHandler ShuttingDown;

        public bool Running => !(_runningTokenSource?.IsCancellationRequested ?? true);

        private const string HostName = "edge-mqtt.facebook.com";
        private const int KeepAlive = 60;   // seconds

        private readonly Instagram _instagram;
        private readonly RealtimeConnectionData _connectionData;
        private readonly object _lockObj = new object();
        private CancellationTokenSource _runningTokenSource;
        private DataWriter _outboundWriter;
        private DataReader _inboundReader;
        private StreamSocket _socket;
        private ushort _packetId = 1;
        private long _seqId;
        private DateTimeOffset _snapshotAt;

        public RealtimeClient(Instagram instagram)
        {
            _instagram = instagram;
            _connectionData = new RealtimeConnectionData(instagram.Device, ApiVersion.Current);
        }

        public async Task Start(long seqId, DateTimeOffset snapshotAt)
        {
            lock (_lockObj)
            {
                if (Running || !_instagram.IsUserAuthenticated)
                {
                    return;
                }
            }

            this.Log("Starting");
            _seqId = seqId;
            _snapshotAt = snapshotAt;
            _connectionData.SetCredential(_instagram.Session);
            var tokenSource = new CancellationTokenSource();
            var connectPacket = new FbnsConnectPacket
            {
                KeepAliveInSeconds = KeepAlive,
                Payload = await PayloadProcessor.BuildPayload(_connectionData, tokenSource.Token)
            };

            var socket = new StreamSocket();
            await socket.ConnectAsync(new HostName(HostName), "443", SocketProtectionLevel.Tls12);

            var reader = new DataReader(socket.InputStream) {ByteOrder = ByteOrder.BigEndian};
            var writer = new DataWriter(socket.OutputStream) {ByteOrder = ByteOrder.BigEndian};

            await FbnsPacketEncoder.EncodePacket(connectPacket, writer);

            lock (_lockObj)
            {
                _inboundReader = reader;
                _outboundWriter = writer;
                _runningTokenSource = tokenSource;
                _socket = socket;
            }

            StartPollingLoop();
        }

        public void Shutdown()
        {
            lock (_lockObj)
            {
                if (!Running)
                {
                    return;
                }

                var tokenSource = _runningTokenSource;
                tokenSource?.Cancel();
                _runningTokenSource = null;
                tokenSource?.Dispose();
                _outboundWriter?.Dispose();
                _inboundReader?.Dispose();
                _socket?.Dispose();
            }

            ShuttingDown?.Invoke(this, EventArgs.Empty);
        }

        public async Task SendMessage(JObject json)
        {
            var jsonBuffer = CryptographicBuffer.ConvertStringToBinary(json.ToString(Formatting.None), BinaryStringEncoding.Utf8);
            var publishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                PacketId = _packetId++,
                TopicName = "/ig_send_message",
                Payload = jsonBuffer
            };

            await FbnsPacketEncoder.EncodePacket(publishPacket, _outboundWriter);
        }

        private async Task SendPing()
        {
            try
            {
                if (!Running) return;
                var packet = PingReqPacket.Instance;
                this.Log("Pinging Realtime server");
                await FbnsPacketEncoder.EncodePacket(packet, _outboundWriter);
            }
            catch (Exception)
            {
                this.Log("Failed to ping Realtime server");
            }
        }

        private async void StartPollingLoop()
        {
            while (Running)
            {
                var reader = _inboundReader;
                Packet packet;
                try
                {
                    await reader.LoadAsync(FbnsPacketDecoder.PACKET_HEADER_LENGTH);
                    packet = await FbnsPacketDecoder.DecodePacket(reader);
                }
                catch (Exception e)
                {
                    if (Running)
                    {
                        DebugLogger.LogException(e, false);
                    }

                    Shutdown();
                    return;
                }

                try
                {
                    await OnPacketReceived(packet);
                }
                catch (OperationCanceledException)
                {
                    // pass
                }
                catch (Exception e)
                {
                    DebugLogger.LogException(e);
                }
            }
        }

        private async Task OnPacketReceived(Packet msg)
        {
            if (!Running) return;
            this.Log(msg.PacketType);
            switch (msg.PacketType)
            {
                case PacketType.CONNACK:
                    var connackPacket = (FbnsConnAckPacket) msg;
                    this.Log($"Connack return code: {connackPacket.ReturnCode}");
                    if (connackPacket.ReturnCode == ConnectReturnCode.Accepted)
                    {
                        await OnConnack();
                        StartPingingLoop();
                    }
                    else
                    {
                        Shutdown();
                    }

                    break;

                case PacketType.PUBLISH:
                    var publishPacket = (PublishPacket)msg;
                    if (publishPacket.QualityOfService == QualityOfService.AtLeastOnce && Running)
                    {
                        await FbnsPacketEncoder.EncodePacket(PubAckPacket.InResponseTo(publishPacket), _outboundWriter);
                    }

                    await OnPublish(publishPacket);
                    break;

                case PacketType.PUBACK:
                    break;

                case PacketType.PINGRESP:
                    break;

                case PacketType.SUBACK:
                    break;

                default:
                    throw new NotSupportedException($"Packet type {msg.PacketType} is not supported.");
            }
        }

        private async Task OnPublish(PublishPacket publishPacket)
        {
            CancellationToken cancellationToken;
            lock (_lockObj)
            {
                if (!Running)
                {
                    return;
                }

                cancellationToken = _runningTokenSource.Token;
            }

            if (publishPacket.Payload == null)
            {
                throw new Exception($"{nameof(PushClient)}: Publish packet received but payload is null");
            }

            this.Log($"Publish to {publishPacket.TopicName} ({publishPacket.PacketId})");
            switch (Enum.Parse(typeof(RealtimeTopicId), publishPacket.TopicName))
            {
                case RealtimeTopicId.MessageSync:
                    var iris = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, publishPacket.Payload);
                    this.Log($"Message sync: {iris}");
                    var messageSyncPayload = JsonConvert.DeserializeObject<List<MessageSyncEventArgs>>(iris);
                    var latest = messageSyncPayload.Last();
                    if (latest.SeqId > _seqId)
                    {
                        _seqId = latest.SeqId;
                    }

                    if (latest.Realtime)
                    {
                        _snapshotAt = DateTimeOffset.Now;
                    }

                    messageSyncPayload = messageSyncPayload.Where(x => x.Data?.Count > 0).ToList();
                    if (messageSyncPayload.Count == 0)
                    {
                        break;
                    }

                    MessageReceived?.Invoke(this, messageSyncPayload);
                    break;

                case RealtimeTopicId.PubSub:
                    var skywalkerMessage = new SkywalkerMessage();
                    await PayloadProcessor.DeserializeObject(publishPacket.Payload, skywalkerMessage, cancellationToken);
                    this.Log($"Skywalker message ({skywalkerMessage.Topic}): {skywalkerMessage.Payload}");
                    HandleSkywalkerMessage(skywalkerMessage);
                    break;

                case RealtimeTopicId.RealtimeSub:
                    var realtimeMessage = new GraphQLMessage();
                    await PayloadProcessor.DeserializeObject(publishPacket.Payload, realtimeMessage, cancellationToken);
                    this.Log($"GraphQL message ({realtimeMessage.Topic}): {realtimeMessage.Payload}");
                    HandleGraphQLMessage(realtimeMessage);
                    break;

                case RealtimeTopicId.IrisSubResponse:
                    var json = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, publishPacket.Payload);
                    this.Log($"Iris sub response: {json}");
                    var response = JsonConvert.DeserializeObject<JObject>(json);
                    if (!(response["succeeded"]?.ToObject<bool>() ?? false))
                    {
                        Shutdown();
                    }

                    break;

                default:
#if DEBUG
                    try
                    {
                        this.Log($"Raw payload: {CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, publishPacket.Payload)}");
                    }
                    catch (Exception e)
                    {
                        DebugLogger.LogException(e, false);
                    }
#endif
                    break;
            }
        }


        private async Task OnConnack()
        {
            await SubscribeForDirectMessageSync().ConfigureAwait(false);
            await RealtimeSub().ConfigureAwait(false);
            await PubSub().ConfigureAwait(false);
            await IrisSub().ConfigureAwait(false);
        }

        private async void StartPingingLoop()
        {
            var pingDelay = KeepAlive / 2;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(pingDelay), _runningTokenSource.Token).ConfigureAwait(false);
                while (Running)
                {
                    await SendPing().ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(pingDelay), _runningTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                this.Log("Stopped pinging sync server");
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e, false);
            }
        }

        private Task SubscribeForDirectMessageSync()
        {
            var messageSync = new SubscriptionRequest("/ig_message_sync", QualityOfService.AtLeastOnce);
            var sendMessageResp = new SubscriptionRequest("/ig_send_message_response", QualityOfService.AtLeastOnce);
            var subIrisResp = new SubscriptionRequest("/ig_sub_iris_response", QualityOfService.AtLeastOnce);
            var subscribePacket = new SubscribePacket(_packetId++, messageSync, sendMessageResp, subIrisResp);

            return FbnsPacketEncoder.EncodePacket(subscribePacket, _outboundWriter);
        }

        private Task RealtimeSub()
        {
            var user = _instagram.Session.LoggedInUser;
            var jsonObject = new JObject
            {
                {
                    "sub", new JArray
                    {
                        GraphQLSubscription.GetAppPresenceSubscription(),
                        //GraphQLSubscription.GetZeroProvisionSubscription(_instagram.Device.Uuid.ToString()),
                        GraphQLSubscription.GetDirectStatusSubscription(),
                        //GraphQLSubscription.GetDirectTypingSubscription(user.Pk),
                        //GraphQLSubscription.GetAsyncAdSubscription(user.Pk)
                    }
                }
            };

            var json = jsonObject.ToString(Formatting.None);
            this.Log($"RealtimeSub: {json}");
            var publishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                Payload = CryptographicBuffer.ConvertStringToBinary(json, BinaryStringEncoding.Utf8),
                PacketId = _packetId++,
                TopicName = "/ig_realtime_sub"
            };

            return FbnsPacketEncoder.EncodePacket(publishPacket, _outboundWriter);
        }

        private Task PubSub()
        {
            var user = _instagram.Session.LoggedInUser;
            var jsonObject = new JObject
            {
                {
                    "sub", new JArray
                    {
                        SkywalkerSubscription.GetDirectSubscription(user.Pk),
                        //SkywalkerSubscription.GetLiveSubscription(user.Pk)
                    }
                }
            };

            var json = jsonObject.ToString(Formatting.None);
            this.Log($"PubSub: {json}");
            var publishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                Payload = CryptographicBuffer.ConvertStringToBinary(json, BinaryStringEncoding.Utf8),
                PacketId = _packetId++,
                TopicName = "/pubsub"
            };

            return FbnsPacketEncoder.EncodePacket(publishPacket, _outboundWriter);
        }


        private Task IrisSub()
        {
            var jsonObject = new JObject
            {
                {"seq_id", _seqId},
                {"sub", new JArray()},
                {"snapshot_at_ms", _snapshotAt.ToUnixTimeMilliseconds()},
                {"snapshot_app_version", ApiVersion.Current.AppVersion}
            };

            var json = jsonObject.ToString(Formatting.None);
            this.Log($"IrisSub: {json}");
            var publishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                Payload = CryptographicBuffer.ConvertStringToBinary(json, BinaryStringEncoding.Utf8),
                PacketId = _packetId++,
                TopicName = "/ig_sub_iris"
            };

            return FbnsPacketEncoder.EncodePacket(publishPacket, _outboundWriter);
        }

        private void HandleGraphQLMessage(GraphQLMessage message)
        {
            switch (message.Topic)
            {
                case GraphQLQueryId.AppPresence:
                    var container = JsonConvert.DeserializeObject<JObject>(message.Payload);
                    if (container.TryGetValue("presence_event", out var presenceEventJson))
                    {
                        var presenceEvent = presenceEventJson.ToObject<UserPresenceEventArgs>();
                        UserPresenceChanged?.Invoke(this, presenceEvent);
                    }

                    break;

                default:
                    this.Log($"Unsupported GraphQL message topic: {message.Topic}");
                    break;
            }
        }

        private void HandleSkywalkerMessage(SkywalkerMessage message)
        {
            var pubsub = JsonConvert.DeserializeObject<PubsubEventArgs>(message.Payload);
            if (pubsub.Data[0].Path.Contains("activity_indicator_id"))
            {
                ActivityIndicatorChanged?.Invoke(this, pubsub);
            }
        }
    }
}
