using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using DotNetty.Buffers;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Transport.Channels;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Android;
using InstagramAPI.Push;
using InstagramAPI.Push.Packets;
using InstagramAPI.Utils;
using Ionic.Zlib;
using Newtonsoft.Json;

namespace BackgroundPushClient
{
    internal enum TopicIds
    {
        Message = 76,   // "/fbns_msg"
        RegReq = 79,    // "/fbns_reg_req"
        RegResp = 80    // "/fbns_reg_resp"
    }

    internal class PacketHandler : SimpleChannelInboundHandler<Packet>
    {
        private const string BASE_INSTAGRAM_URL = "https://i.instagram.com";
        private const int TIMEOUT = 5;

        public event EventHandler<PushReceivedEventArgs> MessageReceived;

        private bool _waitingForPubAck;
        private UserSessionData _user;
        private IHttpRequestProcessor _httpRequestProcessor;
        private AndroidDevice _device;
        private FbnsConnectionData _fbnsConnectionData;
        private readonly StateData _state;
        public StateData CurrentState
        {
            get
            {
                _state.FbnsConnectionData = _fbnsConnectionData;
                
                return _state;
            }
        }

        public PacketHandler(StateData stateData)
        {
            _state = stateData;
            LoadData(stateData);
        }

        private void LoadData(StateData stateData)
        {
            _user = stateData.Session;
            _device = stateData.Device;
            var httpHandler = new HttpClientHandler { CookieContainer = stateData.Cookies };
            var httpClient = new HttpClient(httpHandler) { BaseAddress = new Uri(BASE_INSTAGRAM_URL) };
            var requestMessage = new ApiRequestMessage
            {
                PhoneId = stateData.Device.PhoneId.ToString(),
                Guid = stateData.Device.Uuid,
                Password = stateData.Session?.Password,
                Username = stateData.Session?.UserName,
                DeviceId = stateData.Device.DeviceId,
                AdId = stateData.Device.AdId.ToString()
            };

            _httpRequestProcessor = new HttpRequestProcessor(
                RequestDelay.Empty(), httpClient, httpHandler, requestMessage, new DebugLogger(LogLevel.All));

            _fbnsConnectionData = stateData.FbnsConnectionData;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, Packet msg)
        {
            switch (msg.PacketType)
            {
                case PacketType.CONNACK:
                    Debug.WriteLine($"{typeof(PacketHandler).FullName}: CONNACK received.");
                    _fbnsConnectionData.UpdateAuth(((FbnsConnAckPacket)msg).Authentication);
                    RegisterMqttClient(ctx);
                    break;

                case PacketType.PUBLISH:
                    Debug.WriteLine($"{typeof(PacketHandler).FullName}: PUBLISH received.");
                    var publishPacket = (PublishPacket)msg;
                    if (publishPacket.QualityOfService == QualityOfService.AtLeastOnce)
                    {
                        ctx.WriteAndFlushAsync(PubAckPacket.InResponseTo(publishPacket));
                    }
                    var payload = DecompressPayload(publishPacket.Payload);
                    var json = Encoding.UTF8.GetString(payload);
                    Debug.WriteLine($"{typeof(PacketHandler).FullName}:\tMQTT json: {json}");

                    switch (Enum.Parse(typeof(TopicIds), publishPacket.TopicName))
                    {
                        case TopicIds.Message:
                            var message = JsonConvert.DeserializeObject<PushReceivedEventArgs>(json);
                            message.Json = json;
                            MessageReceived?.Invoke(this, message);
                            break;
                        case TopicIds.RegResp:
                            OnRegisterResponse(json);
                            break;
                        default:
                            Debug.WriteLine($"Unknown topic received: {publishPacket.TopicName}", "Warning");
                            break;
                    }
                    break;

                case PacketType.PUBACK:
                    Debug.WriteLine($"{typeof(PacketHandler).FullName}: PUBACK received.");
                    _waitingForPubAck = false;
                    break;

                // todo: PingResp never arrives even though data was received. Decoder problem?
                case PacketType.PINGRESP:
                    Debug.WriteLine($"{typeof(PacketHandler).FullName}: PINGRESP received.");
                    break;
            }
        }

        private byte[] DecompressPayload(IByteBuffer payload)
        {
            var totalLength = payload.ReadableBytes;

            var decompressedStream = new MemoryStream(256);
            using (var compressedStream = new MemoryStream(totalLength))
            {
                payload.GetBytes(0, compressedStream, totalLength);
                compressedStream.Position = 0;
                using (var zlibStream = new ZlibStream(compressedStream, CompressionMode.Decompress, true))
                {
                    zlibStream.CopyTo(decompressedStream);
                }
            }

            var data = new byte[decompressedStream.Length];
            decompressedStream.Position = 0;
            decompressedStream.Read(data, 0, data.Length);
            decompressedStream.Dispose();
            return data;
        }

        private async void OnRegisterResponse(string json)
        {
            var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (!string.IsNullOrEmpty(response["error"]))
            {
                Debug.WriteLine("FBNS error: " + response["error"], "Error");
                return;
            }

            var token = response["token"];

            await RegisterClient(token);
        }

        private async Task RegisterClient(string token)
        {
            if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));
            if (_fbnsConnectionData.FbnsToken == token)
            {
                _fbnsConnectionData.FbnsToken = token;
                return;
            }

            var uri = new Uri("https://i.instagram.com/api/v1/push/register/");
            var fields = new Dictionary<string, string>
            {
                {"device_type", "android_mqtt"},
                {"is_main_push_channel", "true"},
                {"phone_id", _device.PhoneId.ToString()},
                {"device_token", token},
                {"_csrftoken", _user.CsrfToken },
                {"guid", _device.Uuid.ToString() },
                {"_uuid", _device.Uuid.ToString() },
                {"users", _user.LoggedInUser.Pk.ToString() }
            };
            var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, uri, _device);
            request.Content = new FormUrlEncodedContent(fields);
            var result = await _httpRequestProcessor.SendAsync(request);

            _fbnsConnectionData.FbnsToken = token;
        }


        /// <summary>
        ///     Register this client on the MQTT side stating what application this client is using.
        ///     The server will then return a token for registering over Instagram API side.
        /// </summary>
        /// <param name="ctx"></param>
        private void RegisterMqttClient(IChannelHandlerContext ctx)
        {
            var message = new Dictionary<string, string>
            {
                {"pkg_name", "com.instagram.android"},
                {"appid", "567067343352427"}
            };

            var json = JsonConvert.SerializeObject(message);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            byte[] compressed;
            using (var compressedStream = new MemoryStream(jsonBytes.Length))
            {
                using (var zlibStream =
                    new ZlibStream(compressedStream, CompressionMode.Compress, CompressionLevel.Level9, true))
                {
                    zlibStream.Write(jsonBytes, 0, jsonBytes.Length);
                }
                compressed = new byte[compressedStream.Length];
                compressedStream.Position = 0;
                compressedStream.Read(compressed, 0, compressed.Length);
            }

            var publishPacket = new PublishPacket(QualityOfService.AtLeastOnce, false, false)
            {
                Payload = Unpooled.CopiedBuffer(compressed),
                PacketId = (int)CryptographicBuffer.GenerateRandomNumber(),
                TopicName = ((byte)TopicIds.RegReq).ToString()
            };

            // Send PUBLISH packet then wait for PUBACK
            // Retry after TIMEOUT seconds
            ctx.WriteAndFlushAsync(publishPacket);
            _waitingForPubAck = true;
            Task.Delay(TimeSpan.FromSeconds(TIMEOUT)).ContinueWith(retry =>
            {
                if (_waitingForPubAck)
                {
                    RegisterMqttClient(ctx);
                }
            });
        }
    }
}
