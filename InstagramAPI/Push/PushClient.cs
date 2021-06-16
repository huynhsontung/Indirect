using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Metadata;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Windows.Web.Http;
using InstagramAPI.Classes.Core;
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
        public event EventHandler<UnhandledExceptionEventArgs> ExceptionsCaught; 

        public FbnsConnectionData ConnectionData => _instaApi.Session.PushData;
        public StreamSocket Socket { get; private set; }
        public bool Running => !(_runningTokenSource?.IsCancellationRequested ?? true);
        private string SocketId => SocketIdPrefix + _instaApi.Session.SessionName;

        private const string HostName = "mqtt-mini.facebook.com";
        private const string BackgroundSocketActivityName = "BackgroundPushClient.SocketActivity";
        private const string SocketActivityEntryPoint = "BackgroundPushClient.SocketActivity";
        private const string BackgroundReplyName = "BackgroundPushClient.ReplyAction";
        private const string BackgroundReplyEntryPoint = "BackgroundPushClient.ReplyAction";
        public const string SocketIdPrefix = "mqtt_fbns_";
        public const string SocketIdLegacy = "mqtt_fbns";    // TODO: handle multiple socket IDs for multiple profiles
        public const int KeepAlive = 900;    // seconds
        public const int WaitTime = 5;      // seconds

        private bool _transferred;
        private CancellationTokenSource _runningTokenSource;
        private DataReader _inboundReader;
        private DataWriter _outboundWriter;
        private readonly Instagram _instaApi;

        public PushClient(Instagram api)
        {
            _instaApi = api ?? throw new ArgumentException("Api can't be null", nameof(api));
        }

        public static void UnregisterTasks()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                task.Value.Unregister(true);
            }
        }

        public bool SocketRegistered()
        {
            try
            {
                if (SocketActivityInformation.AllSockets.ContainsKey(SocketId))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                DebugLogger.Log(nameof(PushClient), e);
            }

            return false;
        }

        public static bool TaskExists(string name)
        {
            return BackgroundTaskRegistration.AllTasks.Any(pair => pair.Value.Name == name);
        }

        public static bool TryRegisterBackgroundTaskOnce(string name, string entryPoint, IBackgroundTrigger trigger, out IBackgroundTaskRegistration registeredTask)
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == name)
                {
                    registeredTask = task.Value;
                    return true;
                }
            }

            var taskBuilder = new BackgroundTaskBuilder
            {
                Name = name,
                TaskEntryPoint = entryPoint,
                IsNetworkRequested = true,
                CancelOnConditionLoss = false
            };
            taskBuilder.SetTrigger(trigger);
            taskBuilder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
            try
            {
                registeredTask = taskBuilder.Register();
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                registeredTask = null;
                return false;
            }

            return true;
        }

        public static bool TasksRegistered()
        {
            return TaskExists(BackgroundSocketActivityName) && TaskExists(BackgroundReplyName);
        }

        public static async Task<IBackgroundTaskRegistration> RequestBackgroundAccess()
        {
            if (!TaskExists(BackgroundSocketActivityName) || !TaskExists(BackgroundReplyName))
            {
                try
                {
                    var permissionResult = await BackgroundExecutionManager.RequestAccessAsync();
                    if (permissionResult == BackgroundAccessStatus.DeniedByUser ||
                        permissionResult == BackgroundAccessStatus.DeniedBySystemPolicy ||
                        permissionResult == BackgroundAccessStatus.Unspecified)
                    {
                        return null;
                    }
                }
                catch (Exception)
                {
                    return null;
                }

                DebugLogger.Log(nameof(PushClient), "Request background access successful!");
            }
            else
            {
                DebugLogger.Log(nameof(PushClient), "Background tasks already registered.");
            }

            TryRegisterBackgroundTaskOnce(BackgroundSocketActivityName, SocketActivityEntryPoint,
                new SocketActivityTrigger(), out var socketActivityTask);

            if (ApiInformation.IsTypePresent("Windows.ApplicationModel.Background.ToastNotificationActionTrigger"))
            {
                try
                {
                    TryRegisterBackgroundTaskOnce(BackgroundReplyName, BackgroundReplyEntryPoint,
                        new Windows.ApplicationModel.Background.ToastNotificationActionTrigger(), out _);
                }
                catch (Exception)
                {
                    // ToastNotificationActionTrigger is present but cannot instantiate
                }
            }

            return socketActivityTask;
        }

        /// <summary>
        /// Transfer socket as well as necessary context for background push notification client. 
        /// Transfer only happens if user is logged in.
        /// </summary>
        public async Task TransferPushSocket()
        {
            lock (this)
            {
                // Only transfer once
                if (!_instaApi.IsUserAuthenticated || !Running || _transferred) return;
                _transferred = true;
            }

            // Hand over MQTT socket to socket broker
            var socketId = SocketId;
            var socket = Socket;
            this.Log("Transferring sockets");
            Shutdown();
            await socket.CancelIOAsync();
            socket.TransferOwnership(
                socketId,
                null,
                TimeSpan.FromSeconds(KeepAlive - 60));
            socket.Dispose();
        }

        public async Task StartFromMainView()
        {
            try
            {
                if (!SocketRegistered())
                {
                    //UnregisterTasks();
                    await StartFresh();
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
            }
        }

        public async Task StartWithExistingSocket(StreamSocket socket)
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
            await SendPing();
        }

        public async Task StartFresh(IBackgroundTaskInstance taskInstance = null)
        {
            if (!_instaApi.IsUserAuthenticated || Running || _transferred)
            {
                return;
            }

            this.Log("Starting fresh");

            // Build user agent for first time setup
            if (string.IsNullOrEmpty(ConnectionData.UserAgent))
                ConnectionData.UserAgent = FbnsUserAgent.BuildFbUserAgent(_instaApi.Device);

            var tokenSource = new CancellationTokenSource();
            var connectPacket = new FbnsConnectPacket
            {
                Payload = await PayloadProcessor.BuildPayload(ConnectionData, tokenSource.Token)
            };

            var socket = new StreamSocket();
            var socketActivityTask = taskInstance?.Task ?? await RequestBackgroundAccess();
            EnableTransferOwnershipOnSocket(socket, socketActivityTask);

            await socket.ConnectAsync(new HostName(HostName), "443", SocketProtectionLevel.Tls12);
            _inboundReader = new DataReader(socket.InputStream);
            _outboundWriter = new DataWriter(socket.OutputStream);
            _inboundReader.ByteOrder = ByteOrder.BigEndian;
            _inboundReader.InputStreamOptions = InputStreamOptions.Partial;
            _outboundWriter.ByteOrder = ByteOrder.BigEndian;
            _runningTokenSource = tokenSource;
            Socket = socket;
            await FbnsPacketEncoder.EncodePacket(connectPacket, _outboundWriter);
            StartPollingLoop();
        }

        private static void EnableTransferOwnershipOnSocket(StreamSocket socket, IBackgroundTaskRegistration task)
        {
            try
            {
                socket.EnableTransferOwnership(task.TaskId, SocketActivityConnectedStandbyAction.Wake);
            }
            catch (Exception connectedStandby)
            {
                DebugLogger.Log(nameof(PushClient), connectedStandby);
                DebugLogger.Log(nameof(PushClient), "Connected standby not available");
                socket.EnableTransferOwnership(task.TaskId, SocketActivityConnectedStandbyAction.DoNotWake);
            }
        }

        public void Shutdown()
        {
            this.Log("Stopping push server");
            var tokenSource = _runningTokenSource;
            tokenSource?.Cancel();
            _runningTokenSource = null;
            tokenSource?.Dispose();
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
                }
                catch (Exception e)
                {
                    if (Running)
                    {
                        DebugLogger.LogException(e, false);
                    }

                    return;
                }

                try
                {
                    packet = await FbnsPacketDecoder.DecodePacket(reader);
                    await OnPacketReceived(packet);
                }
                catch (Exception e)
                {
                    ExceptionsCaught?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
                }
            }
        }

        public async Task SendPing()
        {
            try
            {
                var packet = PingReqPacket.Instance;
                if (!Running) return;
                await FbnsPacketEncoder.EncodePacket(packet, _outboundWriter);
                this.Log("Pinging Push server");
            }
            catch (Exception)
            {
                this.Log("Failed to ping Push server");
            }
        }

        public enum TopicIds
        {
            Message = 76,   // "/fbns_msg"
            RegReq = 79,    // "/fbns_reg_req"
            RegResp = 80    // "/fbns_reg_resp"
        }

        private async void DelayTransferAsync()
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(WaitTime + 1), _runningTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (Running)
            {
                try
                {
                    await TransferPushSocket();
                }
                catch (Exception e)
                {
                    ExceptionsCaught?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
                }
            }
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
                        var auth = ((FbnsConnAckPacket) msg).Authentication;
                        ConnectionData.UpdateAuth(auth);
                        await RegisterMqttClient();
                        break;

                    case PacketType.PUBLISH:
                        this.Log("Received PUBLISH");
                        var publishPacket = (PublishPacket) msg;
                        if (publishPacket.Payload == null)
                            throw new Exception($"{nameof(PushClient)}: Publish packet received but payload is null");
                        if (publishPacket.QualityOfService == QualityOfService.AtLeastOnce)
                        {
                            await FbnsPacketEncoder.EncodePacket(PubAckPacket.InResponseTo(publishPacket), writer);
                        }

                        var payload = DecompressPayload(publishPacket.Payload);
                        var json = Encoding.UTF8.GetString(payload);
                        this.Log($"MQTT json: {json}");
                        switch (Enum.Parse(typeof(TopicIds), publishPacket.TopicName))
                        {
                            case TopicIds.Message:
                                try
                                {
                                    var message = JsonConvert.DeserializeObject<PushReceivedEventArgs>(json);
                                    message.Json = json;
                                    if (message.NotificationContent.CollapseKey == "direct_v2_message")
                                        MessageReceived?.Invoke(this, message);
                                }
                                catch (Exception e)
                                {
                                    // If something wrong happens here we don't need to shut down the whole push client
                                    DebugLogger.LogException(e);
                                }
                                break;
                            case TopicIds.RegResp:
                                this.Log("Received token to register");
                                await OnRegisterResponse(json);
                                DelayTransferAsync();
                                //StartKeepAliveLoop();
                                break;
                            default:
                                this.Log($"Unknown topic received: {publishPacket.TopicName}");
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
                        throw new NotSupportedException($"Packet type {msg.PacketType} is not supported.");
                }
            }
            catch (Exception e)
            {
                e.Data["PacketType"] = msg.PacketType.ToString();
                ExceptionsCaught?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
            }
        }

        /// Referencing from https://github.com/mgp25/Instagram-API/blob/master/src/Push.php
        /// <summary>
        ///     After receiving the token, proceed to register over Instagram API
        /// </summary>
        private async Task OnRegisterResponse(string json)
        {
            var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (!string.IsNullOrEmpty(response?["error"]))
            {
                throw new Exception(response["error"]);
            }

            var token = response?["token"];

            if (string.IsNullOrEmpty(token))
            {
                throw new Exception($"Push token is invalid: {json}");
            }

            await RegisterClient(token);
        }

        private async Task RegisterClient(string token)
        {
            if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            var uri = new Uri("https://i.instagram.com/api/v1/push/register/");
            var fields = new Dictionary<string, string>
            {
                {"device_type", "android_mqtt"},
                {"is_main_push_channel", "true"},
                {"device_sub_type", "2" },
                {"device_token", token},
                {"_csrftoken", _instaApi.Session.CsrfToken },
                {"guid", _instaApi.Device.Uuid.ToString() },
                {"_uuid", _instaApi.Device.Uuid.ToString() },
                {"users", _instaApi.Session.LoggedInUser.Pk.ToString() }
            };

            var result = await _instaApi.PostAsync(uri, new HttpFormUrlEncodedContent(fields)).ConfigureAwait(false);
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
                {"pkg_name", ApiVersion.PackageName},
                {"appid", ApiVersion.AppId}
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

            await FbnsPacketEncoder.EncodePacket(publishPacket, _outboundWriter);
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
