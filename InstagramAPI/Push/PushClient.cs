using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Web.Http;
using DotNetty.Buffers;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Transport.Channels;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Android;
using InstagramAPI.Push.Packets;
using Ionic.Zlib;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json;

namespace InstagramAPI.Push
{
    public class PushClient : SimpleChannelInboundHandler<Packet>
    {
        public event EventHandler<PushReceivedEventArgs> MessageReceived;
        public FbnsConnectionData ConnectionData { get; } = new FbnsConnectionData();
        public StreamSocket Socket { get; private set; }
        public override bool IsSharable { get; } = true;

        private const string HOST_NAME = "mqtt-mini.facebook.com";
        private const string BACKGROUND_SOCKET_ACTIVITY_NAME = "BackgroundPushClient.SocketActivity";
        private const string BACKGROUND_INTERNET_AVAILABLE_NAME = "BackgroundPushClient.InternetAvailable";
        private const string SOCKET_ACTIVITY_ENTRY_POINT = "BackgroundPushClient.SocketActivity";
        private const string INTERNET_AVAILABLE_ENTRY_POINT = "BackgroundPushClient.InternetAvailable";
        private const string SOCKET_ID = "mqtt_fbns";

        private SingleThreadEventLoop _loopGroup;
        private readonly UserSessionData _user;
        private readonly AndroidDevice _device;
        private IBackgroundTaskRegistration _socketActivityTask;
        private IBackgroundTaskRegistration _internetAvailableTask;

        public const int KEEP_ALIVE = 900;    // seconds
        private const int TIMEOUT = 5;
        private bool _waitingForPubAck;
        private IChannelHandlerContext _context;
        private readonly Instagram _instaApi;

        public PushClient(Instagram api, bool tryLoadData = true)
        {
            _instaApi = api ?? throw new ArgumentException("Api can't be null", nameof(api));
            _user = api.Session;
            _device = api.Device;

            if (tryLoadData) ConnectionData.LoadFromAppSettings();

            // If token is older than 24 hours then discard it
            if ((DateTimeOffset.Now - ConnectionData.FbnsTokenLastUpdated).TotalHours > 24) ConnectionData.FbnsToken = "";

            // Build user agent for first time setup
            if (string.IsNullOrEmpty(ConnectionData.UserAgent))
                ConnectionData.UserAgent = FbnsUserAgent.BuildFbUserAgent(_device);

            NetworkInformation.NetworkStatusChanged += async sender =>
            {
                var internetProfile = NetworkInformation.GetInternetConnectionProfile();
                if (internetProfile == null || _loopGroup == null) return;
                await Shutdown();
                await StartFresh();
            };
        }

        public override void ChannelRegistered(IChannelHandlerContext context)
        {
            _context = context;
        }

        public void UnregisterTasks()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                switch (task.Value.Name)
                {
                    case BACKGROUND_SOCKET_ACTIVITY_NAME:
                        task.Value.Unregister(false);
                        break;
                    case BACKGROUND_INTERNET_AVAILABLE_NAME:
                        task.Value.Unregister(false);
                        break;
                }
            }
        }

        private async Task<bool> RequestBackgroundAccess()
        {
            UnregisterTasks();

            var permissionResult = await BackgroundExecutionManager.RequestAccessAsync();
            if (permissionResult == BackgroundAccessStatus.DeniedByUser ||
                permissionResult == BackgroundAccessStatus.DeniedBySystemPolicy ||
                permissionResult == BackgroundAccessStatus.Unspecified)
                return false;
            var backgroundTaskBuilder = new BackgroundTaskBuilder
            {
                Name = BACKGROUND_SOCKET_ACTIVITY_NAME,
                TaskEntryPoint = SOCKET_ACTIVITY_ENTRY_POINT
            };
            backgroundTaskBuilder.SetTrigger(new SocketActivityTrigger());
            _socketActivityTask = backgroundTaskBuilder.Register();

            backgroundTaskBuilder = new BackgroundTaskBuilder
            {
                Name = INTERNET_AVAILABLE_ENTRY_POINT,
                TaskEntryPoint = INTERNET_AVAILABLE_ENTRY_POINT
            };
            backgroundTaskBuilder.SetTrigger(new SystemTrigger(SystemTriggerType.InternetAvailable, false));
            _internetAvailableTask = backgroundTaskBuilder.Register();
            return true;
        }

        /// <summary>
        /// Transfer socket as well as necessary context for background push notification client. 
        /// Transfer only happens if user is logged in.
        /// </summary>
        public async Task TransferPushSocket()
        {
            if (!_instaApi.IsUserAuthenticated || _loopGroup == null) return;

            // Hand over MQTT socket to socket broker
            Debug.WriteLine($"{nameof(PushClient)}: Transferring sockets.");
            await SendPing();
            await Shutdown();
            await Socket.CancelIOAsync();
            Socket.TransferOwnership(
                SOCKET_ID,
                null,
                TimeSpan.FromSeconds(KEEP_ALIVE - 60));
        }

        public async void Start()
        {
            if (!_instaApi.IsUserAuthenticated) return;
            try
            {
                if (SocketActivityInformation.AllSockets.TryGetValue(SOCKET_ID, out var socketInformation))
                {
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
            Debug.WriteLine($"{nameof(PushClient)}: Starting with existing socket.");
            Socket = socket;
            if (_loopGroup != null) await _loopGroup.ShutdownGracefullyAsync();
            _loopGroup = new SingleThreadEventLoop();

            var streamSocketChannel = new StreamSocketChannel(Socket);
            streamSocketChannel.Pipeline.AddLast(new FbnsPacketEncoder(), new FbnsPacketDecoder(), this);

            await _loopGroup.RegisterAsync(streamSocketChannel);
            await SendPing();
            try
            {
                await _context.Executor.Schedule(KeepAliveLoop, TimeSpan.FromSeconds(KEEP_ALIVE - 60));
            }
            catch (TaskCanceledException)
            {
                // pass
            }
        }

        private async Task StartFresh()
        {
            Debug.WriteLine($"{nameof(PushClient)}: Starting fresh.");
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
                    Socket.EnableTransferOwnership(_socketActivityTask.TaskId, SocketActivityConnectedStandbyAction.Wake);
                }
                catch (Exception connectedStandby)
                {
                    Debug.WriteLine(connectedStandby);
                    Debug.WriteLine($"{nameof(PushClient)}: Connected standby not available.");
                    try
                    {
                        Socket.EnableTransferOwnership(_socketActivityTask.TaskId, SocketActivityConnectedStandbyAction.DoNotWake);
                    }
                    catch (Exception e)
                    {
#if !DEBUG
                        Crashes.TrackError(e);
#endif
                        Debug.WriteLine(e);
                        Debug.WriteLine($"{nameof(PushClient)}: Failed to transfer socket completely!");
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
            if (_loopGroup != null)
            {
                Debug.WriteLine("Stopped pinging push server");
                var loopGroup = _loopGroup;
                _loopGroup = null;
                await loopGroup.ShutdownGracefullyAsync(TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(1))
                    .ConfigureAwait(false);
            }
        }

        public async Task SendPing()
        {
            try
            {
                var packet = PingReqPacket.Instance;
                await _context.WriteAndFlushAsync(packet);
                Debug.WriteLine("Pinging Push server");
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to ping Push server. Shutting down.");
                _ = Shutdown();
            }
        }

        public enum TopicIds
        {
            Message = 76,   // "/fbns_msg"
            RegReq = 79,    // "/fbns_reg_req"
            RegResp = 80    // "/fbns_reg_resp"
        }

        protected override async void ChannelRead0(IChannelHandlerContext ctx, Packet msg)
        {
            _context = ctx; // Save context for manual Ping later
            switch (msg.PacketType)
            {
                case PacketType.CONNACK:
                    Debug.WriteLine($"{nameof(PushClient)}:\tCONNACK received.");
                    ConnectionData.UpdateAuth(((FbnsConnAckPacket)msg).Authentication);
                    RegisterMqttClient(ctx);
                    break;

                case PacketType.PUBLISH:
                    Debug.WriteLine($"{nameof(PushClient)}:\tPUBLISH received.");
                    var publishPacket = (PublishPacket)msg;
                    if (publishPacket.QualityOfService == QualityOfService.AtLeastOnce)
                    {
                        await ctx.WriteAndFlushAsync(PubAckPacket.InResponseTo(publishPacket));
                    }
                    var payload = DecompressPayload(publishPacket.Payload);
                    var json = Encoding.UTF8.GetString(payload);
                    Debug.WriteLine($"{nameof(PushClient)}:\tMQTT json: {json}");
                    switch (Enum.Parse(typeof(TopicIds), publishPacket.TopicName))
                    {
                        case TopicIds.Message:
                            var message = JsonConvert.DeserializeObject<PushReceivedEventArgs>(json);
                            message.Json = json;
                            OnMessageReceived(message);
                            break;
                        case TopicIds.RegResp:
                            OnRegisterResponse(json);
                            try
                            {
                                await _context.Executor.Schedule(KeepAliveLoop, TimeSpan.FromSeconds(KEEP_ALIVE - 60));
                            }
                            catch (TaskCanceledException)
                            {
                                // pass
                            }
                            break;
                        default:
                            Debug.WriteLine($"Unknown topic received: {publishPacket.TopicName}", "Warning");
                            break;
                    }
                    break;

                case PacketType.PUBACK:
                    Debug.WriteLine($"{nameof(PushClient)}:\tPUBACK received.");
                    _waitingForPubAck = false;
                    break;

                // todo: PingResp never arrives even though data was received. Decoder problem?
                case PacketType.PINGRESP:
                    Debug.WriteLine($"{nameof(PushClient)}:\tPINGRESP received.");
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
            try
            {
                var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                if (!string.IsNullOrEmpty(response["error"]))
                {
                    Debug.WriteLine($"{nameof(PushClient)}: {response["error"]}");
                    await Shutdown().ConfigureAwait(false);
                }

                var token = response["token"];

                await RegisterClient(token);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
#if !DEBUG
                Crashes.TrackError(e);
#endif
                await Shutdown().ConfigureAwait(false);
            }
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
            var result = await _instaApi.PostAsync(uri, new HttpFormUrlEncodedContent(fields));

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

        private async void KeepAliveLoop()
        {
            await SendPing();
            try
            {
                await _context.Executor.Schedule(KeepAliveLoop, TimeSpan.FromSeconds(KEEP_ALIVE - 60));
            }
            catch (TaskCanceledException)
            {
                // pass
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

        private void OnMessageReceived(PushReceivedEventArgs args)
        {
            MessageReceived?.Invoke(this, args);
        }
    }
}
