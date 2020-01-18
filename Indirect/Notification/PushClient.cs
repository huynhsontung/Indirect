using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using DotNetty.Buffers;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Transport.Channels;
using InstaSharper.API;
using InstaSharper.API.Push;
using InstaSharper.API.Push.PacketHelpers;
using InstaSharper.Classes;
using InstaSharper.Classes.DeviceInfo;
using InstaSharper.Helpers;
using Ionic.Zlib;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json;

namespace Indirect.Notification
{
    class PushClient : SimpleChannelInboundHandler<Packet>
    {
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public FbnsConnectionData ConnectionData { get; set; }
        public StreamSocket Socket { get; private set; }
        public override bool IsSharable { get; } = true;

        private const string HOST_NAME = "mqtt-mini.facebook.com";
        private const string BACKGROUND_TASK_NAME = "BackgroundPushClient";
        private const string BACKGROUND_TASK_ENTRY_POINT = "BackgroundPushClient.BackgroundPushClient";
        private const string SOCKET_ID = "mqtt_fbns";

        private SingleThreadEventLoop _loopGroup;
        private readonly UserSessionData _user;
        private readonly IHttpRequestProcessor _httpRequestProcessor;
        private readonly AndroidDevice _device;
        private IBackgroundTaskRegistration _task;

        public const int KEEP_ALIVE = 900;    // seconds
        private const int TIMEOUT = 5;
        private bool _waitingForPubAck;
        private CancellationTokenSource _timerResetToken;
        private IChannelHandlerContext _context;
        private readonly InstaApi _instaApi;

        public PushClient(InstaApi api, FbnsConnectionData connectionData)
        {
            _instaApi = api;
            ConnectionData = connectionData;
            _user = api.UserSession;
            _httpRequestProcessor = api.RequestProcessor;
            _device = api.DeviceInfo;

            // If token is older than 24 hours then discard it
            if ((DateTime.Now - ConnectionData.FbnsTokenLastUpdated).TotalHours > 24) ConnectionData.FbnsToken = "";

            // Build user agent for first time setup
            if (string.IsNullOrEmpty(ConnectionData.UserAgent))
                ConnectionData.UserAgent = FbnsUserAgent.BuildFbUserAgent(_device);
        }

        public override void ChannelRegistered(IChannelHandlerContext context)
        {
            _context = context;
        }

        private async Task<bool> RequestBackgroundAccess()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == BACKGROUND_TASK_NAME)
                {
                    task.Value.Unregister(false);
                    break;
                }
            }

            var permissionResult = await BackgroundExecutionManager.RequestAccessAsync();
            if (permissionResult == BackgroundAccessStatus.DeniedByUser ||
                permissionResult == BackgroundAccessStatus.DeniedBySystemPolicy ||
                permissionResult == BackgroundAccessStatus.Unspecified)
                return false;
            var backgroundTaskBuilder = new BackgroundTaskBuilder
            {
                Name = BACKGROUND_TASK_NAME,
                TaskEntryPoint = BACKGROUND_TASK_ENTRY_POINT
            };
            backgroundTaskBuilder.SetTrigger(new SocketActivityTrigger());
            _task = backgroundTaskBuilder.Register();
            return true;
        }

        /// <summary>
        /// Transfer socket as well as necessary context for background push notification client. 
        /// Transfer only happens if user is logged in.
        /// </summary>
        public async Task TransferPushSocket()
        {
            if (!_instaApi.IsUserAuthenticated) return;

            // Hand over MQTT socket to socket broker
            var memoryStream = new MemoryStream();
            var formatter = new BinaryFormatter();
            var state = _instaApi.GetStateData();
            state.FbnsConnectionData = ConnectionData;
            formatter.Serialize(memoryStream, state);
            var buffer = CryptographicBuffer.CreateFromByteArray(memoryStream.ToArray());
            await SendPing();
            await Shutdown();
            await Socket.CancelIOAsync();
            Socket.TransferOwnership(
                SOCKET_ID,
                new SocketActivityContext(buffer),
                TimeSpan.FromSeconds(KEEP_ALIVE - 60));
        }

        public async void Start()
        {
            if (!_instaApi.IsUserAuthenticated) return;
            try
            {
                if (SocketActivityInformation.AllSockets.TryGetValue(SOCKET_ID, out var socketInformation))
                {
                    var dataStream = socketInformation.Context.Data.AsStream();
                    var formatter = new BinaryFormatter();
                    var stateData = (StateData)formatter.Deserialize(dataStream);
                    ConnectionData = stateData.FbnsConnectionData;
                    var socket = socketInformation.StreamSocket;
                    if (string.IsNullOrEmpty(ConnectionData.FbnsToken)) // if we don't have any push data, start fresh
                        await StartFresh();
                    else
                        await StartWithExistingSocket(socket);
                }
                else
                {
                    await StartFresh();
                }
            }
            catch (Exception)
            {
                await StartFresh();
            }
        }

        private async Task StartWithExistingSocket(StreamSocket socket)
        {
            Socket = socket;
            if (_loopGroup != null) await _loopGroup.ShutdownGracefullyAsync();
            _loopGroup = new SingleThreadEventLoop();

            var streamSocketChannel = new StreamSocketChannel(Socket);
            streamSocketChannel.Pipeline.AddLast(new FbnsPacketEncoder(), new FbnsPacketDecoder(), this);

            await _loopGroup.RegisterAsync(streamSocketChannel);
            await SendPing();
            ResetTimer(_context);
        }

        private async Task StartFresh()
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
            if (await RequestBackgroundAccess())
            {
                try
                {
                    Socket.EnableTransferOwnership(_task.TaskId, SocketActivityConnectedStandbyAction.Wake);
                }
                catch (Exception connectedStandby)
                {
                    Debug.WriteLine(connectedStandby);
                    try
                    {
                        Socket.EnableTransferOwnership(_task.TaskId, SocketActivityConnectedStandbyAction.DoNotWake);
                    }
                    catch (Exception e)
                    {
#if !DEBUG
                        Crashes.TrackError(e);
#endif
                        Debug.WriteLine(e);
                        Debug.WriteLine("Failed to transfer socket completely!");
                    }
                }
            }

            await Socket.ConnectAsync(new HostName(HOST_NAME), "443", SocketProtectionLevel.Tls12);

            var streamSocketChannel = new StreamSocketChannel(Socket);
            streamSocketChannel.Pipeline.AddLast(new FbnsPacketEncoder(), new FbnsPacketDecoder(), this);

            await _loopGroup.RegisterAsync(streamSocketChannel);
            await streamSocketChannel.WriteAndFlushAsync(connectPacket);
        }

        public async Task Shutdown()
        {
            _timerResetToken?.Cancel();
            if (_loopGroup != null) 
                await _loopGroup.ShutdownGracefullyAsync(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
        }

        public async Task SendPing()
        {
            var packet = PingReqPacket.Instance;
            await _context.WriteAndFlushAsync(packet);
            Debug.WriteLine("Pinging Push server");
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
                Debug.WriteLine("Reconnecting.");
                _waitingForPubAck = false;
                _timerResetToken?.Cancel();
                await StartFresh();
            });

        }

        protected override async void ChannelRead0(IChannelHandlerContext ctx, Packet msg)
        {
            _context = ctx; // Save context for manual Ping later
            switch (msg.PacketType)
            {
                case PacketType.CONNACK:
                    Debug.WriteLine($"{DateTime.Now.ToString(CultureInfo.CurrentCulture)}:\tCONNACK received.");
                    ConnectionData.UpdateAuth(((FbnsConnAckPacket)msg).Authentication);
                    RegisterMqttClient(ctx);
                    break;

                case PacketType.PUBLISH:
                    Debug.WriteLine($"{DateTime.Now.ToString(CultureInfo.CurrentCulture)}:\tPUBLISH received.");
                    var publishPacket = (PublishPacket)msg;
                    if (publishPacket.QualityOfService == QualityOfService.AtLeastOnce)
                    {
                        await ctx.WriteAndFlushAsync(PubAckPacket.InResponseTo(publishPacket));
                    }
                    var payload = DecompressPayload(publishPacket.Payload);
                    var json = Encoding.UTF8.GetString(payload);
                    Debug.WriteLine($"{DateTime.Now.ToString(CultureInfo.CurrentCulture)}:\tMQTT json: {json}");
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
                    Debug.WriteLine($"{DateTime.Now.ToString(CultureInfo.CurrentCulture)}:\tPUBACK received.");
                    _waitingForPubAck = false;
                    break;

                // todo: PingResp never arrives even though data was received. Decoder problem?
                case PacketType.PINGRESP:
                    Debug.WriteLine($"{DateTime.Now.ToString(CultureInfo.CurrentCulture)}:\tPINGRESP received.");
                    //ResetTimer(ctx);
                    break;

                default:
                    throw new NotSupportedException($"Packet type {msg.PacketType} is not supported.");
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
                PacketId = (int) CryptographicBuffer.GenerateRandomNumber(),
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

        private async void ResetTimer(IChannelHandlerContext ctx)
        {
            _timerResetToken?.Cancel();
            _timerResetToken = new CancellationTokenSource();

            // Create new cancellation token for timer reset
            var cancellationToken = _timerResetToken.Token;

            try
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(KEEP_ALIVE - 60), cancellationToken); // wait for _keepAliveDuration - 60 seconds
                    if (cancellationToken.IsCancellationRequested) break;
                    await SendPing();
                }
            }
            catch (Exception)
            {
                // ignored
            }

            Debug.WriteLine("Stopped pinging push server");
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
