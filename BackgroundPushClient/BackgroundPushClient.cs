using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
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
                        // Todo: Implement reconnect
                        break;

                    case SocketActivityTriggerReason.KeepAliveTimerExpired:
                        var packet = PingReqPacket.Instance;
                        await streamSocketChannel.WriteAndFlushAsync(packet);
                        break;
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
            catch
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
                                Text = notificationContent.Title
                            },
                            new AdaptiveText()
                            {
                                Text = notificationContent.Message
                            }
                        }
                    }
                }
            };

            // Create the toast notification
            var toast = new ToastNotification(toastContent.GetXml());

            // And send the notification
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
