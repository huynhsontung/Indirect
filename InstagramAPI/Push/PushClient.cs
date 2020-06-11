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
using InstagramAPI.Classes;
using InstagramAPI.Classes.Android;
using InstagramAPI.Classes.Mqtt.Packets;
using InstagramAPI.Push.Packets;
using InstagramAPI.Utils;
using Ionic.Zlib;
using Newtonsoft.Json;

namespace InstagramAPI.Push
{
    public class PushClient
    {
        public event EventHandler<PushReceivedEventArgs> MessageReceived;
        public FbnsConnectionData ConnectionData { get; } = new FbnsConnectionData();
        public StreamSocket Socket { get; private set; }

        private const string HOST_NAME = "mqtt-mini.facebook.com";
        private const string BACKGROUND_SOCKET_ACTIVITY_NAME = "BackgroundPushClient.SocketActivity";
        private const string BACKGROUND_INTERNET_AVAILABLE_NAME = "BackgroundPushClient.InternetAvailable";
        private const string SOCKET_ACTIVITY_ENTRY_POINT = "BackgroundPushClient.SocketActivity";
        private const string INTERNET_AVAILABLE_ENTRY_POINT = "BackgroundPushClient.InternetAvailable";
        private const string SOCKET_ID = "mqtt_fbns";

        private readonly UserSessionData _user;
        private readonly AndroidDevice _device;
        private IBackgroundTaskRegistration _socketActivityTask;
        private IBackgroundTaskRegistration _internetAvailableTask;

        public const int KEEP_ALIVE = 900;    // seconds
        private const int TIMEOUT = 5;
        private bool _waitingForPubAck;
        private CancellationTokenSource _runningTokenSource;
        private DataReader _inboundReader;
        private DataWriter _outboundWriter;
        private readonly Instagram _instaApi;
        private bool RunningAndReadable => !(_runningTokenSource?.IsCancellationRequested ?? false) && _inboundReader != null;

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
                if (internetProfile == null || _runningTokenSource.IsCancellationRequested) return;
                await StartFresh();
            };
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
            if (!_instaApi.IsUserAuthenticated || _runningTokenSource.IsCancellationRequested) return;

            // Hand over MQTT socket to socket broker
            this.Log("Transferring sockets");
            await SendPing().ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(2));  // grace period
            Shutdown();
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
                        await StartFresh().ConfigureAwait(false);
                    else
                        await StartWithExistingSocket(socket).ConfigureAwait(false);
                }
                else
                {
                    await StartFresh().ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                await StartFresh().ConfigureAwait(false);
            }
        }

        public async Task StartWithExistingSocket(StreamSocket socket)
        {
            try
            {
                this.Log("Starting with existing socket");
                if (RunningAndReadable) Shutdown();
                Socket = socket;
                _inboundReader = new DataReader(socket.InputStream);
                _outboundWriter = new DataWriter(socket.OutputStream);
                _inboundReader.ByteOrder = ByteOrder.BigEndian;
                _outboundWriter.ByteOrder = ByteOrder.BigEndian;
                _runningTokenSource = new CancellationTokenSource();
                
                StartPollingLoop();
                await SendPing().ConfigureAwait(false);
                StartKeepAliveLoop();
            }
            catch (TaskCanceledException)
            {
                // pass
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                Shutdown();
            }
        }

        public async Task StartFresh()
        {
            try
            {
                this.Log("Starting fresh");
                if (RunningAndReadable) Shutdown();

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
                        this.Log(connectedStandby);
                        this.Log("Connected standby not available");
                        try
                        {
                            Socket.EnableTransferOwnership(_socketActivityTask.TaskId, SocketActivityConnectedStandbyAction.DoNotWake);
                        }
                        catch (Exception e)
                        {
                            DebugLogger.LogException(e);
                            this.Log("Failed to transfer socket completely!");
                            Shutdown();
                            return;
                        }
                    }
                }

                await Socket.ConnectAsync(new HostName(HOST_NAME), "443", SocketProtectionLevel.Tls12);

                _inboundReader = new DataReader(Socket.InputStream);
                _outboundWriter = new DataWriter(Socket.OutputStream);
                _inboundReader.ByteOrder = ByteOrder.BigEndian;
                _outboundWriter.ByteOrder = ByteOrder.BigEndian;
                _runningTokenSource = new CancellationTokenSource();

                await FbnsPacketEncoder.EncodePacket(connectPacket, _outboundWriter);
                StartPollingLoop();
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                Shutdown();
            }
            
        }

        public void Shutdown()
        {
            _runningTokenSource?.Cancel();
            _inboundReader = null;
            _outboundWriter?.DetachStream();
            _outboundWriter?.Dispose();
            _outboundWriter = null;
            this.Log("Stopped pinging push server");
        }

        private async void StartPollingLoop()
        {
            while (RunningAndReadable)
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
                    // If client is still active then retry
                    if (RunningAndReadable)
                    {
                        DebugLogger.LogException(e);
                        Shutdown();
                        Start();
                    }
                    return;
                }
                await OnPacketReceived(packet);
            }
        }

        public async Task SendPing()
        {
            try
            {
                var packet = PingReqPacket.Instance;
                await FbnsPacketEncoder.EncodePacket(packet, _outboundWriter);
                this.Log("Pinging Push server");
            }
            catch (Exception)
            {
                this.Log("Failed to ping Push server. Shutting down.");
                Shutdown();
            }
        }

        public enum TopicIds
        {
            Message = 76,   // "/fbns_msg"
            RegReq = 79,    // "/fbns_reg_req"
            RegResp = 80    // "/fbns_reg_resp"
        }

        private async Task OnPacketReceived(Packet msg)
        {
            try
            {
                switch (msg.PacketType)
                {
                    case PacketType.CONNACK:
                        this.Log("Received CONNACK");
                        ConnectionData.UpdateAuth(((FbnsConnAckPacket) msg).Authentication);
                        await RegisterMqttClient();
                        break;

                    case PacketType.PUBLISH:
                        this.Log("Received PUBLISH");
                        var publishPacket = (PublishPacket) msg;
                        if (publishPacket.Payload == null)
                            throw new Exception($"{nameof(PushClient)}: Publish packet received but payload is null");
                        if (publishPacket.QualityOfService == QualityOfService.AtLeastOnce)
                        {
                            await FbnsPacketEncoder.EncodePacket(PubAckPacket.InResponseTo(publishPacket), _outboundWriter);
                        }

                        var payload = DecompressPayload(publishPacket.Payload);
                        var json = Encoding.UTF8.GetString(payload);
                        this.Log($"MQTT json: {json}");
                        switch (Enum.Parse(typeof(TopicIds), publishPacket.TopicName))
                        {
                            case TopicIds.Message:
                                var message = JsonConvert.DeserializeObject<PushReceivedEventArgs>(json);
                                message.Json = json;
                                if (message.NotificationContent.CollapseKey == "direct_v2_message")
                                    MessageReceived?.Invoke(this, message);
                                break;
                            case TopicIds.RegResp:
                                await OnRegisterResponse(json);
                                StartKeepAliveLoop();
                                break;
                            default:
                                this.Log($"Unknown topic received: {publishPacket.TopicName}");
                                break;
                        }

                        break;

                    case PacketType.PUBACK:
                        this.Log("Received PUBACK");
                        _waitingForPubAck = false;
                        break;

                    // todo: PingResp never arrives even though data was received. Decoder problem?
                    case PacketType.PINGRESP:
                        this.Log("Received PINGRESP");
                        break;

                    default:
                        throw new NotSupportedException($"Packet type {msg.PacketType} is not supported.");
                }
            }
            catch (Exception e)
            {
                // Something went wrong with Push client. Shutting down.
                DebugLogger.LogException(e);
                Shutdown();
            }
        }

        /// Referencing from https://github.com/mgp25/Instagram-API/blob/master/src/Push.php
        /// <summary>
        ///     After receiving the token, proceed to register over Instagram API
        /// </summary>
        private async Task OnRegisterResponse(string json)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                if (!string.IsNullOrEmpty(response["error"]))
                {
                    this.Log($"{response["error"]}");
                    Shutdown();
                }

                var token = response["token"];

                await RegisterClient(token);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                Shutdown();
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
                {"device_sub_type", "2" },
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
        private async Task RegisterMqttClient()
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
                Payload = compressed.AsBuffer(),
                PacketId = (ushort) CryptographicBuffer.GenerateRandomNumber(),
                TopicName = ((byte)TopicIds.RegReq).ToString()
            };

            // Send PUBLISH packet then wait for PUBACK
            // Retry after TIMEOUT seconds
            await FbnsPacketEncoder.EncodePacket(publishPacket, _outboundWriter);
            WaitForPubAck();
        }

        private async void WaitForPubAck()
        {
            _waitingForPubAck = true;
            await Task.Delay(TimeSpan.FromSeconds(TIMEOUT));
            if (_waitingForPubAck)
            {
                await RegisterMqttClient();
            }
        }

        private async void StartKeepAliveLoop()
        {
            if (_runningTokenSource == null) return;
            try
            {
                while (!_runningTokenSource.IsCancellationRequested)
                {
                    await SendPing();
                    await Task.Delay(TimeSpan.FromSeconds(KEEP_ALIVE - 60), _runningTokenSource.Token);
                }
            }
            catch (TaskCanceledException)
            {
                // pass
            }
        }

        private byte[] DecompressPayload(IBuffer payload)
        {
            var compressedStream = payload.AsStream();

            var decompressedStream = new MemoryStream(256);
            using (var zlibStream = new ZlibStream(compressedStream, CompressionMode.Decompress, true))
            {
                zlibStream.CopyTo(decompressedStream);
            }

            var data = decompressedStream.GetWindowsRuntimeBuffer(0, (int) decompressedStream.Length);
            return data.ToArray();
        }
    }
}
