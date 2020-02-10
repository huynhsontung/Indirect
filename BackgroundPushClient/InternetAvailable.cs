using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking;
using Windows.Networking.Sockets;
using DotNetty.Transport.Channels;
using InstagramAPI.Push;
using InstagramAPI.Push.Packets;

namespace BackgroundPushClient
{
    public sealed class InternetAvailable : IBackgroundTask
    {
        private const string HOST_NAME = "mqtt-mini.facebook.com";
        private const string SOCKET_ID = "mqtt_fbns";
        private const int KEEP_ALIVE = 900;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            try
            {
                var stateData = await Utils.LoadStateAsync();
                stateData.FbnsConnectionData.FbnsToken = "";
                if (string.IsNullOrEmpty(stateData.FbnsConnectionData.UserAgent))
                    stateData.FbnsConnectionData.UserAgent = FbnsUserAgent.BuildFbUserAgent(stateData.DeviceInfo);
                var connectPacket = new FbnsConnectPacket
                {
                    Payload = await PayloadProcessor.BuildPayload(stateData.FbnsConnectionData)
                };
                var loopGroup = new SingleThreadEventLoop();
                var socket = await SetupNewSocket(taskInstance);
                var streamSocketChannel = new StreamSocketChannel(socket);
                var packetHandler = new PacketHandler(stateData);
                packetHandler.MessageReceived += Utils.OnMessageReceived;
                streamSocketChannel.Pipeline.AddLast(new FbnsPacketEncoder(), new FbnsPacketDecoder(), packetHandler);
                await loopGroup.RegisterAsync(streamSocketChannel);
                await streamSocketChannel.WriteAndFlushAsync(connectPacket);

                await Task.Delay(TimeSpan.FromSeconds(5));  // Wait 5s to complete all outstanding IOs (hopefully)
                var updatedState = packetHandler.CurrentState;
                var buffer = await Utils.SaveStateAsync(updatedState);
                await loopGroup.ShutdownGracefullyAsync(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4));
                await socket.CancelIOAsync();
                socket.TransferOwnership(SOCKET_ID, new SocketActivityContext(buffer),
                    TimeSpan.FromSeconds(KEEP_ALIVE - 60));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Debug.WriteLine($"{typeof(InternetAvailable).FullName}: Can't finish push cycle. Abort.");
            }
            finally
            {
                deferral.Complete();
            }
        }

        private static async Task<StreamSocket> SetupNewSocket(IBackgroundTaskInstance taskInstance)
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
    }
}
