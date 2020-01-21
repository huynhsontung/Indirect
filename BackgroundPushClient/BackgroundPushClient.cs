using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Web;
using Windows.ApplicationModel.Background;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Transport.Channels;
using InstaSharper.API.Push.PacketHelpers;
using InstaSharper.Classes;
using InstaSharper.API.Push;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;

namespace BackgroundPushClient
{
    /// <summary>
    /// A more compact version of InstantMessaging.Notification.PushClient including <see cref="HttpRequestProcessor"/>
    /// </summary>
    public sealed class BackgroundPushClient : IBackgroundTask
    {
        private const string HOST_NAME = "mqtt-mini.facebook.com";
        private const string SOCKET_ID = "mqtt_fbns";
        private const int KEEP_ALIVE = 900;

        private BackgroundTaskDeferral _deferral;
        private ApplicationDataContainer _settings = ApplicationData.Current.LocalSettings;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            try
            {
                var details = (SocketActivityTriggerDetails) taskInstance.TriggerDetails;
                var dataStream = details.SocketInformation.Context.Data.AsStream();
                var formatter = new BinaryFormatter();
                var stateData = (StateData) formatter.Deserialize(dataStream);
                var loopGroup = new SingleThreadEventLoop();
                var socket = details.Reason == SocketActivityTriggerReason.SocketClosed ? 
                    await SetupNewSocket(taskInstance) : details.SocketInformation.StreamSocket;
                var streamSocketChannel = new StreamSocketChannel(socket);
                var packetHandler = new PacketHandler(stateData);
                packetHandler.MessageReceived += OnMessageReceived;
                streamSocketChannel.Pipeline.AddLast(new FbnsPacketEncoder(), new FbnsPacketDecoder(), packetHandler);
                await loopGroup.RegisterAsync(streamSocketChannel);

                switch (details.Reason)
                {
                    case SocketActivityTriggerReason.SocketClosed:
                        stateData.FbnsConnectionData.FbnsToken = "";
                        if (string.IsNullOrEmpty(stateData.FbnsConnectionData.UserAgent))
                            stateData.FbnsConnectionData.UserAgent = FbnsUserAgent.BuildFbUserAgent(stateData.DeviceInfo);
                        var connectPacket = new FbnsConnectPacket
                        {
                            Payload = await PayloadProcessor.BuildPayload(stateData.FbnsConnectionData)
                        };
                        await streamSocketChannel.WriteAndFlushAsync(connectPacket);
                        break;

                    case SocketActivityTriggerReason.KeepAliveTimerExpired:
                        var packet = PingReqPacket.Instance;
                        await streamSocketChannel.WriteAndFlushAsync(packet);
                        break;

                    // We don't need to handle SocketActivity event. PacketHandler will take care of that.
                }

                await Task.Delay(TimeSpan.FromSeconds(5));  // Wait 5s to complete all outstanding IOs (hopefully)
                var updatedState = packetHandler.CurrentState;
                var memoryStream = new MemoryStream();
                formatter.Serialize(memoryStream, updatedState);
                var buffer = CryptographicBuffer.CreateFromByteArray(memoryStream.ToArray());
                await loopGroup.ShutdownGracefullyAsync(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10));
                await socket.CancelIOAsync();
                socket.TransferOwnership(
                    details.SocketInformation.Id, new SocketActivityContext(buffer), TimeSpan.FromSeconds(KEEP_ALIVE - 60));
            }
            catch (Exception)
            {
                Debug.WriteLine("Can't finish push cycle. Abort.");
            }
            finally
            {
                _deferral.Complete();
            }
        }

        private async Task<StreamSocket> SetupNewSocket(IBackgroundTaskInstance taskInstance)
        {
            var socket = new StreamSocket();
            socket.Control.KeepAlive = true;
            socket.Control.NoDelay = true;
            try
            {
                socket.EnableTransferOwnership(taskInstance.Task.TaskId, SocketActivityConnectedStandbyAction.Wake);
            }
            catch (Exception)
            {
                Debug.WriteLine("System does not support connected standby.");
                socket.EnableTransferOwnership(taskInstance.Task.TaskId, SocketActivityConnectedStandbyAction.DoNotWake);
            }

            await socket.ConnectAsync(new HostName(HOST_NAME), "443", SocketProtectionLevel.Tls12);
            return socket;
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            var notificationContent = args.NotificationContent;
            var igAction = notificationContent.IgAction;
            var querySeparatorIndex = igAction.IndexOf('?');
            var targetType = igAction.Substring(0, querySeparatorIndex);
            var queryParams = HttpUtility.ParseQueryString(igAction.Substring(querySeparatorIndex));
            var threadId = queryParams["id"];
            var itemId = queryParams["x"];
            var toastContent = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = notificationContent.Message
                            }
                        },
                        AppLogoOverride = string.IsNullOrEmpty(args.NotificationContent.OptionalAvatarUrl)
                            ? null
                            : new ToastGenericAppLogo()
                            {
                                Source = args.NotificationContent.OptionalAvatarUrl,
                                HintCrop = ToastGenericAppLogoCrop.Circle,
                                AlternateText = "Profile picture"
                            }
                    }
                }
            };

            // Create the toast notification
            var toast = new ToastNotification(toastContent.GetXml())
            {
                Group = threadId,
                Tag = itemId, 
                ExpiresOnReboot = false
            };
            // And send the notification
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
