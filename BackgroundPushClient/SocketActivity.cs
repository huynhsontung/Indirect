using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Transport.Channels;
using InstagramAPI.Push.Packets;

namespace BackgroundPushClient
{
    /// <summary>
    /// A more compact version of InstantMessaging.Notification.PushClient including <see cref="HttpRequestProcessor"/>
    /// </summary>
    public sealed class SocketActivity : IBackgroundTask
    {
        private const int KEEP_ALIVE = 900;

        private readonly StorageFolder _localFolder = ApplicationData.Current.LocalFolder;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            try
            {
                var details = (SocketActivityTriggerDetails) taskInstance.TriggerDetails;
                if (details.Reason == SocketActivityTriggerReason.SocketClosed) return;
                Debug.WriteLine($"{typeof(SocketActivity).FullName}: {details.Reason}");
                var dataStream = details.SocketInformation.Context.Data.AsStream();
                var formatter = new BinaryFormatter();
                var stateData = (StateData) formatter.Deserialize(dataStream);
                var loopGroup = new SingleThreadEventLoop();
                var socket = details.SocketInformation.StreamSocket;
                var streamSocketChannel = new StreamSocketChannel(socket);
                var packetHandler = new PacketHandler(stateData);
                packetHandler.MessageReceived += Utils.OnMessageReceived;
                streamSocketChannel.Pipeline.AddLast(new FbnsPacketEncoder(), new FbnsPacketDecoder(), packetHandler);
                await loopGroup.RegisterAsync(streamSocketChannel);

                // We don't need to handle SocketActivity event. PacketHandler will take care of that.
                if (details.Reason == SocketActivityTriggerReason.KeepAliveTimerExpired)
                {
                    var packet = PingReqPacket.Instance;
                    await streamSocketChannel.WriteAndFlushAsync(packet);
                }

                await Task.Delay(TimeSpan.FromSeconds(5));  // Wait 5s to complete all outstanding IOs (hopefully)
                var updatedState = packetHandler.CurrentState;
                var memoryStream = new MemoryStream();
                formatter.Serialize(memoryStream, updatedState);
                var buffer = CryptographicBuffer.CreateFromByteArray(memoryStream.ToArray());
                await loopGroup.ShutdownGracefullyAsync(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4));
                await socket.CancelIOAsync();
                socket.TransferOwnership(
                    details.SocketInformation.Id, new SocketActivityContext(buffer), TimeSpan.FromSeconds(KEEP_ALIVE - 60));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Debug.WriteLine($"{typeof(SocketActivity).FullName}: Can't finish push cycle. Abort.");
            }
            finally
            {
                deferral.Complete();
            }
        }
    }
}
