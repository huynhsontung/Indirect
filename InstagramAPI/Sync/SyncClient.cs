using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using InstagramAPI.Classes.Mqtt.Packets;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI.Sync
{
    public class SyncClient
    {
        public event EventHandler<List<MessageSyncEventArgs>> MessageReceived;
        public event EventHandler<PubsubEventArgs> ActivityIndicatorChanged;
        public event EventHandler<UserPresenceEventArgs> UserPresenceChanged; 
        public event EventHandler<Exception> FailedToStart;

        private ushort _packetId = 1;
        private CancellationTokenSource _pinging;
        private readonly Instagram _instaApi;
        private long _seqId;
        private DateTimeOffset _snapshotAt;
        private MessageWebSocket _socket;

        public bool IsRunning => !(_pinging?.IsCancellationRequested ?? true);

        public SyncClient(Instagram api)
        {
            _instaApi = api;
            NetworkInformation.NetworkStatusChanged += OnNetworkChanged;
        }

        // Shutdown the client by stop pinging the server
        public async void Shutdown()
        {
            if (!IsRunning) return;
            _pinging.Cancel();
            var disconnectPacket = DisconnectPacket.Instance;
            var buffer = StandalonePacketEncoder.EncodePacket(disconnectPacket);
            try
            {
                await _socket.OutputStream.WriteAsync(buffer);
                await _socket.OutputStream.FlushAsync();
            }
            catch (Exception e)
            {
                this.Log(e);
            }
        }

        public async Task Start(long seqId, DateTimeOffset snapshotAt, bool force = false)
        {
            try
            {
                if (IsRunning && !force)
                {
                    this.Log("Sync client is already running");
                    return;
                }
                this.Log("Sync client starting");
                if (seqId == 0)
                    throw new ArgumentException("Invalid seqId. Have you fetched inbox for the first time?",
                        nameof(seqId));
                _seqId = seqId;
                _snapshotAt = snapshotAt;
                _pinging?.Cancel();
                _pinging = new CancellationTokenSource();
                _packetId = 1;
                var device = _instaApi.Device;

                var connectPacket = new ConnectPacket
                {
                    CleanSession = true,
                    ClientId = "mqttwsclient",
                    HasUsername = true,
                    HasPassword = false,
                    KeepAliveInSeconds = 10,
                    ProtocolLevel = 3,
                    ProtocolName = "MQIsdp"
                };
                
                var userAgent =
                    $"Mozilla/5.0 (Linux; Android 10; {device.DeviceName}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Mobile Safari/537.36 Edg/87.0.664.66";
                var json = new JObject(
                    new JProperty("u", _instaApi.Session.LoggedInUser.Pk),
                    new JProperty("s", GenerateDigitsRandom(16)),
                    new JProperty("cp", 1),
                    new JProperty("ecp", 0),
                    new JProperty("chat_on", true),
                    new JProperty("fg", true),
                    new JProperty("d", device.PhoneId.ToString()),
                    new JProperty("ct", "cookie_auth"),
                    new JProperty("mqtt_sid", ""),
                    new JProperty("aid", GenerateDigitsRandom(16)),
                    new JProperty("st", new JArray()),
                    new JProperty("pm", new JArray()),
                    new JProperty("dc", ""),
                    new JProperty("no_auto_fg", true),
                    new JProperty("asi", new JObject(new JProperty("Accept-Language", "en"))),
                    new JProperty("a", userAgent)
                );
                var username = JsonConvert.SerializeObject(json, Formatting.None);
                connectPacket.Username = username;
                // var buffer = StandalonePacketEncoder.EncodePacket(connectPacket);
                var messageWebsocket = new MessageWebSocket();
                messageWebsocket.Control.MessageType = SocketMessageType.Binary;
                messageWebsocket.SetRequestHeader("User-Agent", userAgent);
                messageWebsocket.SetRequestHeader("Origin", "https://www.instagram.com");
                messageWebsocket.MessageReceived += OnMessageReceived;
                // messageWebsocket.Closed += OnClosed;
                var buffer = StandalonePacketEncoder.EncodePacket(connectPacket);
                await messageWebsocket.ConnectAsync(new Uri("wss://edge-chat.instagram.com/chat"));
                await messageWebsocket.OutputStream.WriteAsync(buffer);
                await messageWebsocket.OutputStream.FlushAsync();
                _socket = messageWebsocket;
            }
            catch (Exception e)
            {
                this.Log(e);
                this.Log("Failed to start");
                FailedToStart?.Invoke(this, e);
            }
        }

        public async Task SendMessage(JObject json)
        {
            if (!IsRunning) return;
            json = MakeSendMessageJson(json);
            var jsonBytes = GetJsonBytes(json);
            var publishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                PacketId = _packetId++,
                TopicName = "/ig_send_message",
                Payload = jsonBytes.AsBuffer()
            };

            await WriteAndFlushPacketAsync(publishPacket, _socket.OutputStream);
        }

        private JObject MakeSendMessageJson(JObject content)
        {
            var message = new JObject
            {
                {"client_context", DateTime.UtcNow.Ticks.ToString()},
                {"device_id", _instaApi.Device.PhoneId.ToString().ToUpper()},
                {"action", "send_item"}
            };
            message.Merge(content);
            return message;
        }

        private async void OnNetworkChanged(object sender)
        {
            var internetProfile = NetworkInformation.GetInternetConnectionProfile();
            if (internetProfile == null || _seqId == default || _snapshotAt == default ||
                _pinging.IsCancellationRequested) return;
            try
            {
                // Disconnect to make sure there is no duplicate 
                var disconnectPacket = DisconnectPacket.Instance;
                var buffer = StandalonePacketEncoder.EncodePacket(disconnectPacket);
                await _socket.OutputStream.WriteAsync(buffer);
                await _socket.OutputStream.FlushAsync();
            }
            catch (Exception)
            {
                // Ignore if fail
            }
            this.Log("Internet connection available. Reconnecting.");
            await Start(_seqId, _snapshotAt, true);
        }

        private async void OnMessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            if (!IsRunning) return;
            try
            {
                var dataReader = args.GetDataReader();
                Packet packet;
                try
                {
                    packet = StandalonePacketDecoder.DecodePacket(dataReader);
                }
                catch (Exception e)
                {
                    this.Log(e);
                    this.Log("Failed to decode packet.");
                    return;
                }

                this.Log("Received " + packet.PacketType);
                switch (packet.PacketType)
                {
                    case PacketType.CONNACK:
                        await OnConnack(sender);
                        return;

                    case PacketType.PUBLISH:
                        await OnPublish(sender, packet);
                        return;

                    case PacketType.PINGRESP:
                        break;
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                this.Log("Exception occurred when processing incoming sync message.");
            }
        }

        private async Task OnPublish(MessageWebSocket ws, Packet packet)
        {
            var outStream = ws.OutputStream;
            var publishPacket = (PublishPacket)packet;
            if (publishPacket.Payload == null)
                throw new Exception($"{nameof(SyncClient)}: Publish packet received but payload is null");
            var payload = Encoding.UTF8.GetString(publishPacket.Payload.ToArray());
            this.Log($"Publish to {publishPacket.TopicName} ({publishPacket.PacketId}) with payload: {payload}");
            switch (publishPacket.TopicName)
            {
                case "/ig_message_sync":
                    var messageSyncPayload = JsonConvert.DeserializeObject<List<MessageSyncEventArgs>>(payload);
                    var latest = messageSyncPayload.Last();
                    try
                    {
                        if (latest.SeqId > _seqId)
                        {
                            _seqId = latest.SeqId;
                        }

                        if (latest.Data?.Count > 0 && latest.Data[0].Op != "remove")
                        {
                            _snapshotAt = latest.Data[0].Item.Timestamp;
                        }

                        messageSyncPayload = messageSyncPayload.Where(x => x.Data?.Count > 0).ToList();
                        if (messageSyncPayload.Count == 0)
                        {
                            break;
                        }

                        MessageReceived?.Invoke(this, messageSyncPayload);
                    }
                    catch (Exception e)
                    {
                        // DEBUG CODE - TO BE REMOVED IN THE FUTURE
                        if (latest.Data?.Count > 0)
                        {
                            string value;
                            var itemSyncData = latest.Data[0];
                            var token = JToken.Parse(itemSyncData.Value);
                            if (token.Type == JTokenType.Object)
                            {
                                var jProps = JObject.Parse(itemSyncData.Value).Properties().Select(p => p.Name);
                                value = string.Join(",", jProps);
                            }
                            else
                            {
                                value = token.Type.ToString();
                            }

                            DebugLogger.LogException(e, properties: new Dictionary<string, string>
                            {
                                {"Op", itemSyncData.Op},
                                {"Path", itemSyncData.Path.StripSensitive()},
                                {"Value", value}
                            });
                        }
                        else
                        {
                            DebugLogger.LogException(e);
                        }
                    }

                    break;

                case "/pubsub":
                    payload = payload.Substring(payload.IndexOf('{'));  // pubsub is weird. It has a few non string bytes before the actual data.
                    var pubsub = JsonConvert.DeserializeObject<PubsubEventArgs>(payload);
                    if (pubsub.Data[0].Path.Contains("activity_indicator_id"))
                    {
                        ActivityIndicatorChanged?.Invoke(this, pubsub);
                    }

                    break;

                case "/ig_realtime_sub":
                    payload = payload.Substring(payload.IndexOf('{'));
                    var container = JsonConvert.DeserializeObject<JObject>(payload);
                    var presenceEvent = container["presence_event"].ToObject<UserPresenceEventArgs>();
                    UserPresenceChanged?.Invoke(this, presenceEvent);
                    break;

                case "/ig_sub_iris_response":
                    break;

                case "/ig_send_message_response":
                    break;
            }


            if (publishPacket.QualityOfService == QualityOfService.AtLeastOnce)
            {
                await WriteAndFlushPacketAsync(PubAckPacket.InResponseTo(publishPacket), outStream);
            }
        }

        private async Task OnConnack(MessageWebSocket ws)
        {
            var outStream = ws.OutputStream;
            var loggedInUser = _instaApi.Session.LoggedInUser;
            var subscribePacket = new SubscribePacket(
                _packetId++,
                new SubscriptionRequest("/ig_message_sync", QualityOfService.AtMostOnce),
                new SubscriptionRequest("/ig_send_message_response", QualityOfService.AtMostOnce)
            );
            await WriteAndFlushPacketAsync(subscribePacket, outStream);

            var unsubPacket = new UnsubscribePacket(_packetId++, "/ig_sub_iris_response");
            await WriteAndFlushPacketAsync(unsubPacket, outStream);
            subscribePacket = new SubscribePacket(_packetId++,
                new SubscriptionRequest("/ig_sub_iris_response", QualityOfService.AtMostOnce));
            await WriteAndFlushPacketAsync(subscribePacket, outStream);
            var json = new JObject(
                new JProperty("seq_id", _seqId),
                new JProperty("snapshot_at_ms", _snapshotAt.ToUnixTimeMilliseconds()),
                new JProperty("snapshot_app_version", "web"),
                new JProperty("subscription_type", "message"));
            var jsonBytes = GetJsonBytes(json);
            var irisPublishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                PacketId = _packetId++,
                TopicName = "/ig_sub_iris",
                Payload = jsonBytes.AsBuffer()
            };
            await WriteAndFlushPacketAsync(irisPublishPacket, outStream);

            json = new JObject(new JProperty("unsub", new JArray($"ig/u/v1/{loggedInUser.Pk}")));
            jsonBytes = GetJsonBytes(json);
            var pubsubPublishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                PacketId = _packetId++,
                TopicName = "/pubsub",
                Payload = jsonBytes.AsBuffer()
            };
            await WriteAndFlushPacketAsync(pubsubPublishPacket, outStream);
            unsubPacket = new UnsubscribePacket(_packetId++, "/pubsub");
            await WriteAndFlushPacketAsync(unsubPacket, outStream);
            subscribePacket = new SubscribePacket(_packetId++,
                new SubscriptionRequest("/pubsub", QualityOfService.AtMostOnce));
            await WriteAndFlushPacketAsync(subscribePacket, outStream);
            json = new JObject(new JProperty("sub", new JArray($"ig/u/v1/{loggedInUser.Pk}")));
            jsonBytes = GetJsonBytes(json);
            pubsubPublishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                PacketId = _packetId++,
                TopicName = "/pubsub",
                Payload = jsonBytes.AsBuffer()
            };
            await WriteAndFlushPacketAsync(pubsubPublishPacket, outStream);

            var clientSubscriptionId = Guid.NewGuid().ToString();
            json = new JObject(new JProperty("unsub",
                new JArray($"1/graphqlsubscriptions/17846944882223835/{{\"input_data\":{{\"client_subscription_id\":\"{clientSubscriptionId}\"}}}}")));
            jsonBytes = GetJsonBytes(json);
            var realtimeSubPublishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                PacketId = _packetId++,
                TopicName = "/ig_realtime_sub",
                Payload = jsonBytes.AsBuffer()
            };
            await WriteAndFlushPacketAsync(realtimeSubPublishPacket, outStream);
            unsubPacket = new UnsubscribePacket(_packetId++, "/ig_realtime_sub");
            await WriteAndFlushPacketAsync(unsubPacket, outStream);
            subscribePacket = new SubscribePacket(_packetId++,
                new SubscriptionRequest("/ig_realtime_sub", QualityOfService.AtMostOnce));
            await WriteAndFlushPacketAsync(subscribePacket, outStream);
            json = new JObject(new JProperty("sub",
                new JArray($"1/graphqlsubscriptions/17846944882223835/{{\"input_data\":{{\"client_subscription_id\":\"{clientSubscriptionId}\"}}}}")));
            jsonBytes = GetJsonBytes(json);
            realtimeSubPublishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                PacketId = _packetId++,
                TopicName = "/ig_realtime_sub",
                Payload = jsonBytes.AsBuffer()
            };
            await WriteAndFlushPacketAsync(realtimeSubPublishPacket, outStream);
            subscribePacket = new SubscribePacket(_packetId++,
                new SubscriptionRequest("/ig_send_message_response", QualityOfService.AtMostOnce));
            await WriteAndFlushPacketAsync(subscribePacket, outStream);
            
            StartPingingLoop(ws);
        }

        private async void StartPingingLoop(MessageWebSocket ws)
        {
            try
            {
                while (!_pinging.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(8), _pinging.Token).ConfigureAwait(false);
                    var pingPacket = PingReqPacket.Instance;
                    var pingBuffer = StandalonePacketEncoder.EncodePacket(pingPacket);
                    await ws.OutputStream.WriteAsync(pingBuffer);
                    await ws.OutputStream.FlushAsync();
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
            finally
            {
                var tokenSource = _pinging;
                _pinging = null;
                tokenSource?.Dispose();
            }
        }

        private static async Task WriteAndFlushPacketAsync(Packet packet, IOutputStream outStream)
        {
            DebugLogger.Log(nameof(SyncClient), $"Sending {packet.PacketType}");
            await outStream.WriteAsync(StandalonePacketEncoder.EncodePacket(packet));
            await outStream.FlushAsync();

            if (packet is PublishPacket publishPacket)
            {
                var json = Encoding.UTF8.GetString(publishPacket.Payload.ToArray());
                DebugLogger.Log(nameof(SyncClient),
                    $"Publish to {publishPacket.TopicName} ({publishPacket.PacketId}) with payload: {json}");
            }
        }

        private static byte[] GetJsonBytes(JObject json)
        {
            var jsonString = JsonConvert.SerializeObject(json, Formatting.None);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        // Generate random number without 0s
        private static ulong GenerateDigitsRandom(int length)
        {
            var result = "";
            var random = new Random();
            for (int i = 0; i < length; i++)
            {
                result += random.Next(1, 9).ToString();
            }

            return ulong.Parse(result);
        }
    }
}
