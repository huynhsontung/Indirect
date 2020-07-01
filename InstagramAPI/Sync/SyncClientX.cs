using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Windows.Web.Http;
using InstagramAPI.Classes.Mqtt.Packets;
using InstagramAPI.Push.Packets;
using InstagramAPI.Utils;
using Ionic.Zlib;
using Newtonsoft.Json;
using System.Diagnostics;
using InstagramAPI.Sync.Subs;
using InstagramAPI.Sync;
using Thrift.Transport.Client;
using Thrift.Protocol.Entities;
using Thrift.Protocol;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace InstagramAPI.Push
{
    public class SyncClientX
    {
        private const string DEFAULT_HOST = "edge-mqtt.facebook.com";

        public event EventHandler<List<MessageSyncEventArgs>> MessageReceived;
        public event EventHandler<PubsubEventArgs> ActivityIndicatorChanged;
        public event EventHandler<UserPresenceEventArgs> UserPresenceChanged;
        public event EventHandler<Exception> FailedToStart;
        
        readonly FbnsConnectionData ConnectionData = new FbnsConnectionData();
        public StreamSocket Socket { get; private set; }
        public bool Running => !(_runningTokenSource?.IsCancellationRequested ?? true);
        public const int KEEP_ALIVE = 900;    // seconds
        private const int TIMEOUT = 5;
        private CancellationTokenSource _runningTokenSource;
        private DataReader _inboundReader;
        private DataWriter _outboundWriter;
        private readonly Instagram _instaApi;
        private long _seqId;
        private DateTimeOffset _snapshotAt;
        public SyncClientX(Instagram api)
        {
            _instaApi = api ?? throw new ArgumentException("Api can't be null", nameof(api));

            NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
        }

        private async void OnNetworkStatusChanged(object sender)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            if (!Instagram.InternetAvailable() || Running) return;
            await StartFresh();
        }
   
        public async Task Start(long seqId, DateTimeOffset snapshotAt)
        {
            if (!_instaApi.IsUserAuthenticated || Running) return;

            if (seqId == 0)
                throw new ArgumentException("Invalid seqId. Have you fetched inbox for the first time?",
                    nameof(seqId));
            _seqId = seqId;
            _snapshotAt = snapshotAt;
            try
            {
                await StartFresh().ConfigureAwait(false);
            }
            catch (Exception)
            {
                await StartFresh().ConfigureAwait(false);
            }
        }

        public void StartWithExistingSocket(StreamSocket socket)
        {
            try
            {
                this.Log("Starting with existing socket");
                if (Running) throw new Exception("Push client is already running");
                Socket = socket;
                _inboundReader = new DataReader(socket.InputStream);
                _outboundWriter = new DataWriter(socket.OutputStream);
                _inboundReader.ByteOrder = ByteOrder.BigEndian;
                _inboundReader.InputStreamOptions = InputStreamOptions.Partial;
                _outboundWriter.ByteOrder = ByteOrder.BigEndian;
                _runningTokenSource = new CancellationTokenSource();

                StartPollingLoop();
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
            }
        }

        public async Task StartFresh()
        {
            this.Log("Starting fresh");
            if (!Instagram.InternetAvailable())
            {
                this.Log("Internet not available. Exiting.");
                return;
            }

            if (Running) throw new Exception("Push client is already running");
            var connectPacket = new FbnsConnectPacket
            {
                Payload = await RealtimePayload.BuildPayload(_instaApi)
            };

            Socket = new StreamSocket();
            Socket.Control.KeepAlive = true;
            Socket.Control.NoDelay = true;
            try
            {
                await Socket.ConnectAsync(new HostName(DEFAULT_HOST), "443", SocketProtectionLevel.Tls12);
                _inboundReader = new DataReader(Socket.InputStream);
                _outboundWriter = new DataWriter(Socket.OutputStream);
                _inboundReader.ByteOrder = ByteOrder.BigEndian;
                _inboundReader.InputStreamOptions = InputStreamOptions.Partial;
                _outboundWriter.ByteOrder = ByteOrder.BigEndian;
                _runningTokenSource = new CancellationTokenSource();
                await FbnsPacketEncoder.EncodePacket(connectPacket, _outboundWriter);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                Restart();
                return;
            }
            StartPollingLoop();
        }
        public async Task SendDirectTextAsync(string threadId, string text)
        {
            var data = new Dictionary<string, string>
            {
                {"action", "send_item"},
                {"client_context", Guid.NewGuid().ToString()},
                {"thread_id",  threadId},
                {"item_type", "text"},
                {"text", text},
            };

            var json = JsonConvert.SerializeObject(data);
            var bytes = Encoding.UTF8.GetBytes(json);


            var dataStream = new MemoryStream(512);
            using (var zlibStream = new ZlibStream(dataStream, CompressionMode.Compress, CompressionLevel.Level9, true))
            {
                await zlibStream.WriteAsync(bytes, 0, bytes.Length);
            }
            var compressed = dataStream.GetWindowsRuntimeBuffer(0, (int)dataStream.Length);
            var publishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                Payload = compressed,
                PacketId = (ushort)CryptographicBuffer.GenerateRandomNumber(),
                TopicName = "132"
            };

            await FbnsPacketEncoder.EncodePacket(publishPacket, _outboundWriter);

        }
        async Task SubscribeForDM()
        {
            var messageSync = new SubscriptionRequest("/ig_message_sync", QualityOfService.AtLeastOnce);
            var sendMessageResp = new SubscriptionRequest("/ig_send_message_response", QualityOfService.AtLeastOnce);
            var subIrisResp = new SubscriptionRequest("/ig_sub_iris_response", QualityOfService.AtLeastOnce);
            var subscribePacket = new SubscribePacket((ushort)CryptographicBuffer.GenerateRandomNumber(), messageSync, sendMessageResp, subIrisResp);

            await FbnsPacketEncoder.EncodePacket(subscribePacket, _outboundWriter);
        }

        async Task RealtimeSub()
        {
            var user = _instaApi.Session.LoggedInUser;
            var dic = new Dictionary<string, List<string>>
            {
                {  "sub",
                    new List<string>
                    {
                        GraphQLSubscriptions.GetAppPresenceSubscription(),
                        GraphQLSubscriptions.GetZeroProvisionSubscription(_instaApi.Device.Uuid.ToString()),
                        GraphQLSubscriptions.GetDirectStatusSubscription(),
                        GraphQLSubscriptions.GetDirectTypingSubscription(user?.Pk.ToString()),
                        GraphQLSubscriptions.GetAsyncAdSubscription(user?.Pk.ToString())
                    }
                }
            };
            var json = JsonConvert.SerializeObject(dic);
            var bytes = Encoding.UTF8.GetBytes(json);
            var dataStream = new MemoryStream(512);
            using (var zlibStream = new ZlibStream(dataStream, CompressionMode.Compress, CompressionLevel.Level9, true))
            {
                await zlibStream.WriteAsync(bytes, 0, bytes.Length);
            }

            var compressed = dataStream.GetWindowsRuntimeBuffer(0, (int)dataStream.Length);
            var publishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                Payload = compressed,
                PacketId = (ushort)CryptographicBuffer.GenerateRandomNumber(),
                TopicName = "/ig_realtime_sub"
            };
            await FbnsPacketEncoder.EncodePacket(publishPacket, _outboundWriter);
        }


        async Task PubSub()
        {
            var user = _instaApi.Session.LoggedInUser;
            var dic = new Dictionary<string, List<string>>
            {
                {  "sub",
                    new List<string>
                    {
                        SkyWalker.DirectSubscribe(user?.Pk.ToString()),
                        SkyWalker.LiveSubscribe(user?.Pk.ToString()),
                    }
                }
            };
            var json = JsonConvert.SerializeObject(dic);
            var bytes = Encoding.UTF8.GetBytes(json);
            var dataStream = new MemoryStream(512);
            using (var zlibStream = new ZlibStream(dataStream, CompressionMode.Compress, CompressionLevel.Level9, true))
            {
                await zlibStream.WriteAsync(bytes, 0, bytes.Length);
            }

            var compressed = dataStream.GetWindowsRuntimeBuffer(0, (int)dataStream.Length);
            var publishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                Payload = compressed,
                PacketId = (ushort)CryptographicBuffer.GenerateRandomNumber(),
                TopicName = "/pubsub"
            };
            await FbnsPacketEncoder.EncodePacket(publishPacket, _outboundWriter);
        }


        async Task IrisSub()
        {
            var dic = new Dictionary<string, object>
            {
                {"seq_id", _seqId.ToString()},
                {"sub", new List<string>()},
                {"snapshot_at_ms", _snapshotAt.ToUnixTimeMilliseconds()}
            };
            var json = JsonConvert.SerializeObject(dic);
            var bytes = Encoding.UTF8.GetBytes(json);
            var dataStream = new MemoryStream(512);
            using (var zlibStream = new ZlibStream(dataStream, CompressionMode.Compress, CompressionLevel.Level9, true))
            {
                await zlibStream.WriteAsync(bytes, 0, bytes.Length);
            }

            var compressed = dataStream.GetWindowsRuntimeBuffer(0, (int)dataStream.Length);
            var publishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                Payload = compressed,
                PacketId = (ushort)CryptographicBuffer.GenerateRandomNumber(),
                TopicName = "/ig_sub_iris"
            };
            await FbnsPacketEncoder.EncodePacket(publishPacket, _outboundWriter);
        }
        public void Shutdown()
        {
            this.Log("Stopping sync server");
            NetworkInformation.NetworkStatusChanged -= OnNetworkStatusChanged;
            _runningTokenSource?.Cancel();
            _inboundReader?.Dispose();
            _outboundWriter?.DetachStream();
            _outboundWriter?.Dispose();
        }

        private async void Restart()
        {
            this.Log("Restarting sync server");
            NetworkInformation.NetworkStatusChanged -= OnNetworkStatusChanged;
            _runningTokenSource?.Cancel();
            _inboundReader?.Dispose();
            _outboundWriter?.DetachStream();
            _outboundWriter?.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(3));
            if (Running) return;
            await StartFresh();
            NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
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
                        DebugLogger.LogException(e);
                        Restart();
                    }
                    return;
                }
                await OnPacketReceived(packet);
            }
        }
        public enum TopicIds
        {
            MessageSync = 146,          //      /ig_message_sync
            PubSub = 88,                //      /pubsub,
            RealtimeSub = 149,          //      /ig_realtime_sub
            SendMessageResponse = 133,  //      /ig_send_message_response,
        }

        private async Task OnPacketReceived(Packet msg)
        {
            if (!Running) return;
            var writer = _outboundWriter;
            try
            {
                switch (msg.PacketType)
                {
                    case PacketType.CONNACK:
                        this.Log("Received CONNACK");
                        await SubscribeForDM();
                        await RealtimeSub();
                        await PubSub();
                        await IrisSub();
                        break;

                    case PacketType.PUBLISH:
                        this.Log("Received PUBLISH");
                        var publishPacket = (PublishPacket)msg;
                        if (publishPacket.Payload == null)
                            throw new Exception($"{nameof(PushClient)}: Publish packet received but payload is null");
                        if (publishPacket.QualityOfService == QualityOfService.AtLeastOnce)
                        {
                            await FbnsPacketEncoder.EncodePacket(PubAckPacket.InResponseTo(publishPacket), writer);
                        }


                        var payload = DecompressPayload(publishPacket.Payload);
                        var json = await GetJsonFromThrift(payload);
                        this.Log($"MQTT json: {json}");
                        if (string.IsNullOrEmpty(json)) break;

                        switch (Enum.Parse(typeof(TopicIds), publishPacket.TopicName))
                        {
                            case TopicIds.MessageSync:
                                {
                                    var messageSyncPayload = JsonConvert.DeserializeObject<List<MessageSyncEventArgs>>(json);
                                    var latest = messageSyncPayload.LastOrDefault();
                                    if (latest.SeqId > _seqId && latest.Data.Count > 0)
                                    {
                                        _seqId = latest.SeqId;
                                        if (latest.Data[0].Op != "remove")
                                            _snapshotAt = latest.Data[0].Item.Timestamp;
                                    }
                                    MessageReceived?.Invoke(this, messageSyncPayload);
                                }
                                break;
                            case TopicIds.PubSub:
                                json = json.Substring(json.IndexOf('{'));  // pubsub is weird. It has a few non string bytes before the actual data.
                                var pubsub = JsonConvert.DeserializeObject<PubsubEventArgs>(json);
                                if (pubsub.Data[0].Path.Contains("activity_indicator_id"))
                                {
                                    ActivityIndicatorChanged?.Invoke(this, pubsub);
                                }
                                break;

                            case TopicIds.RealtimeSub:
                                json = json.Substring(json.IndexOf('{'));
                                var container = JsonConvert.DeserializeObject<JObject>(json);
                                if (container["presence_event"] != null)
                                {
                                    var presenceEvent = container["presence_event"].ToObject<UserPresenceEventArgs>();
                                    UserPresenceChanged?.Invoke(this, presenceEvent);
                                }
                                else if (container["event"] != null)
                                {
                                    //{
                                    //  "event": "patch",
                                    //  "data": [
                                    //    {
                                    //      "op": "add",
                                    //      "path": "/direct_v2/threads/1111111111111111111111/activity_indicator_id/6684234061789256886",
                                    //      "value": "{\"timestamp\": \"1593645589193235\", \"sender_id\": \"XXXXXX\", \"ttl\": 12000, \"activity_status\": 1}"
                                    //    }
                                    //  ]
                                    //}

                                    //var messageSyncPayload = JsonConvert.DeserializeObject<List<MessageSyncEventArgs>>(json);
                                    //var latest = messageSyncPayload.LastOrDefault();
                                    //if (latest.SeqId > _seqId && latest.Data.Count > 0)
                                    //{
                                    //    _seqId = latest.SeqId;
                                    //    if (latest.Data[0].Op != "remove")
                                    //        _snapshotAt = latest.Data[0].Item.Timestamp;
                                    //}
                                    //MessageReceived?.Invoke(this, messageSyncPayload);
                                }
                                break;
                            case TopicIds.SendMessageResponse:
                                //whatever
                                break;
                        }
                        break;

                    case PacketType.PUBACK:
                        this.Log("Received PUBACK");
                        break;

                    case PacketType.PINGRESP:
                        this.Log("Received PINGRESP");
                        break;

                    default:
                        Debug.WriteLine($"Unknown topic received:{msg.PacketType}");
                        break;
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
            }
        }
        async Task<string> GetJsonFromThrift(byte[] bytes)
        {
            try
            {
                var _memoryBufferTransport = new TMemoryBufferTransport(bytes);
                var _thrift = new TCompactProtocol(_memoryBufferTransport);
                while (true)
                {
                    var field = await _thrift.ReadFieldBeginAsync(CancellationToken.None);
                    if (field.Type == TType.Stop)
                        break;

                    if (field.Type == TType.String)
                    {
                        var json = await _thrift.ReadStringAsync(CancellationToken.None);
                        if (!string.IsNullOrEmpty(json))
                            if (json.Contains("{") && json.EndsWith("}"))
                                return json;
                    }
                    await _thrift.ReadFieldEndAsync(CancellationToken.None);
                }
            }
            catch { }
            return Encoding.UTF8.GetString(bytes);
        }


        private byte[] DecompressPayload(IBuffer payload)
        {
            var compressedStream = payload.AsStream();

            var decompressedStream = new MemoryStream(256);
            using (var zlibStream = new ZlibStream(compressedStream, CompressionMode.Decompress, true))
            {
                zlibStream.CopyTo(decompressedStream);
            }

            var data = decompressedStream.GetWindowsRuntimeBuffer(0, (int)decompressedStream.Length);
            return data.ToArray();
        }
    }
}
