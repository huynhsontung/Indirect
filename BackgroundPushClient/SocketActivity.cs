using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using InstagramAPI;
using InstagramAPI.Push;
using InstagramAPI.Utils;

namespace BackgroundPushClient
{
    public sealed class SocketActivity : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            this.Log("-------------- Start of background task --------------");
            try
            {
                var details = (SocketActivityTriggerDetails) taskInstance.TriggerDetails;
                this.Log($"{details.Reason}");
                var internetProfile = NetworkInformation.GetInternetConnectionProfile();
                if (internetProfile == null)
                {
                    this.Log("No internet. Stop.");
                    return;
                }
                if (!await Utils.TryAcquireSyncLock())
                {
                    this.Log("Failed to open SyncLock file. Main application might be running. Exit background task.");
                    var socket = details.SocketInformation.StreamSocket;
                    if (socket == null) return;
                    await socket.CancelIOAsync();
                    socket.TransferOwnership(
                        PushClient.SOCKET_ID,
                        null,
                        TimeSpan.FromSeconds(PushClient.KEEP_ALIVE - 60));
                    return;
                }
                var instagram = Instagram.Instance;
                instagram.PushClient.MessageReceived += Utils.OnMessageReceived;
                switch (details.Reason)
                {
                    case SocketActivityTriggerReason.SocketClosed:
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        if (!await Utils.TryAcquireSyncLock())
                        {
                            this.Log("Failed to open SyncLock file after extended wait. Main application might be running. Exit background task.");
                            return;
                        }
                        await instagram.PushClient.StartFresh();
                        break;
                    }
                    default:
                    {
                        var socket = details.SocketInformation.StreamSocket;
                        instagram.PushClient.StartWithExistingSocket(socket);
                        break;
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(5));  // Wait 5s to complete all outstanding IOs (hopefully)
                instagram.PushClient.ConnectionData.SaveToAppSettings();
                await instagram.PushClient.TransferPushSocket(false);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                this.Log($"{typeof(SocketActivity).FullName}: Can't finish push cycle. Abort.");
            }
            finally
            {
                this.Log("-------------- End of background task --------------");
                deferral.Complete();
            }
        }
    }
}
