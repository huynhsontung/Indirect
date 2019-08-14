using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using DotNetty.Buffers;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using InstaSharper.API;
using InstaSharper.API.Push;
using InstaSharper.API.Push.PacketHelpers;
using InstaSharper.Classes;
using InstaSharper.Classes.DeviceInfo;
using InstaSharper.Helpers;
using Ionic.Zlib;
using Newtonsoft.Json;

namespace InstantMessaging.Notification
{
    class PushClient : SimpleChannelInboundHandler<Packet>
    {
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public FbnsConnectionData ConnectionData { get; set; }
        public StreamSocket Socket { get; private set; }

        private const string HOST_NAME = "mqtt-mini.facebook.com";

        private SingleThreadEventLoop _loopGroup;
        private readonly UserSessionData _user;
        private readonly IHttpRequestProcessor _httpRequestProcessor;
        private readonly AndroidDevice _device;

        private const int KEEP_ALIVE = 900;    // seconds
        private const int TIMEOUT = 5;
        private int _publishId;
        private bool _waitingForPubAck;
        private CancellationTokenSource _timerResetToken;

        public PushClient(IInstaApi api, FbnsConnectionData connectionData)
        {
            ConnectionData = connectionData;

            _user = api.GetLoggedUser();
            _httpRequestProcessor = api.RequestProcessor;
            _device = api.DeviceInfo;

            // If token is older than 24 hours then discard it
            if ((DateTime.Now - ConnectionData.FbnsTokenLastUpdated).TotalHours > 24) ConnectionData.FbnsToken = "";

            // Build user agent for first time setup
            if (string.IsNullOrEmpty(ConnectionData.UserAgent))
                ConnectionData.UserAgent = FbnsUserAgent.BuildFbUserAgent(_device);
        }

        public async Task Start()
        {
            if (_loopGroup != null) await _loopGroup.ShutdownGracefullyAsync();
            _loopGroup = new SingleThreadEventLoop();

            var connectPacket = new FbnsConnectPacket
            {
                Payload = await PayloadProcessor.BuildPayload(ConnectionData)
            };

            Socket = new StreamSocket();
            Socket.Control.KeepAlive = true;
            Socket.Control.NoDelay = true;
            // todo: enable ownership transfer stream socket for background push client

            await Socket.ConnectAsync(new HostName(HOST_NAME), "443", SocketProtectionLevel.Tls12);

            var streamSocketChannel = new StreamSocketChannel(Socket);
            streamSocketChannel.Pipeline.AddLast(new FbnsPacketEncoder(), new FbnsPacketDecoder(), this);

            await _loopGroup.RegisterAsync(streamSocketChannel);
            await streamSocketChannel.WriteAndFlushAsync(connectPacket);
        }

        public async Task Shutdown()
        {
            if (_loopGroup != null) await _loopGroup.ShutdownGracefullyAsync();
        }

        public enum TopicIds
        {
            Message = 76,   // "/fbns_msg"
            RegReq = 79,    // "/fbns_reg_req"
            RegResp = 80    // "/fbns_reg_resp"
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            // If connection is closed, reconnect
            Task.Delay(TimeSpan.FromSeconds(TIMEOUT)).ContinueWith(async task =>
            {
                _waitingForPubAck = false;
                _timerResetToken?.Cancel();
                await Start();
            });

        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, Packet msg)
        {
            switch (msg.PacketType)
            {
                case PacketType.CONNACK:
                    ConnectionData.UpdateAuth(((FbnsConnAckPacket)msg).Authentication);
                    RegisterMqttClient(ctx);
                    break;

                case PacketType.PUBLISH:
                    var publishPacket = (PublishPacket)msg;
                    if (publishPacket.QualityOfService == QualityOfService.AtLeastOnce)
                    {
                        ctx.WriteAndFlushAsync(PubAckPacket.InResponseTo(publishPacket));
                    }
                    var payload = DecompressPayload(publishPacket.Payload);
                    var json = Encoding.UTF8.GetString(payload);
                    switch (Enum.Parse(typeof(TopicIds), publishPacket.TopicName))
                    {
                        case TopicIds.Message:
                            var message = JsonConvert.DeserializeObject<MessageReceivedEventArgs>(json);
                            message.Json = json;
                            OnMessageReceived(message);
                            break;
                        case TopicIds.RegResp:
                            OnRegisterResponse(json);
                            ResetTimer(ctx);
                            break;
                        default:
                            Debug.WriteLine($"Unknown topic received: {publishPacket.TopicName}", "Warning");
                            break;
                    }
                    break;

                case PacketType.PUBACK:
                    _waitingForPubAck = false;
                    break;

                // todo: PingResp never arrives even though data was received. Decoder problem?
                case PacketType.PINGRESP:
                    ResetTimer(ctx);
                    break;
            }
        }

        /// Referencing from https://github.com/mgp25/Instagram-API/blob/master/src/Push.php
        /// <summary>
        ///     After receiving the token, proceed to register over Instagram API
        /// </summary>
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
            if (ConnectionData.FbnsToken == token)
            {
                ConnectionData.FbnsToken = token;
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

            ConnectionData.FbnsToken = token;
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
                PacketId = ++_publishId,
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

        private void ResetTimer(IChannelHandlerContext ctx)
        {
            _timerResetToken?.Cancel();
            _timerResetToken = new CancellationTokenSource();

            // Create new cancellation token for timer reset
            var cancellationToken = _timerResetToken.Token;

            Task.Delay(TimeSpan.FromSeconds(KEEP_ALIVE - 60),
                    cancellationToken) // wait for _keepAliveDuration - 60 seconds
                .ContinueWith(async task =>
                {
                    // Then send a PingReq packet
                    var packet = PingReqPacket.Instance;
                    await ctx.WriteAndFlushAsync(packet);
                    Debug.WriteLine("PingReq sent");
                }, cancellationToken)
                .ContinueWith(async task =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken); // Wait for another 60s for PingResp
                        if (!cancellationToken.IsCancellationRequested)
                            ExceptionCaught(ctx, new Exception());   // If still no PingResp then signal channel inactive and restart the client
                    }
                    catch (TaskCanceledException)
                    {
                        Debug.WriteLine("Keep alive timer reset.");
                    }
                }, cancellationToken);

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

        private void OnMessageReceived(MessageReceivedEventArgs args)
        {
            MessageReceived?.Invoke(this, args);
        }
    }
}
