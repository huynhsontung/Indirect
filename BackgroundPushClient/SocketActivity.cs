using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using InstagramAPI;
using InstagramAPI.Utils;

namespace BackgroundPushClient
{
    /// <summary>
    /// A more compact version of InstantMessaging.Notification.PushClient including <see cref="HttpRequestProcessor"/>
    /// </summary>
    public sealed class SocketActivity : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            try
            {
                var details = (SocketActivityTriggerDetails) taskInstance.TriggerDetails;
                if (details.Reason == SocketActivityTriggerReason.None) return;
                this.Log($"{typeof(SocketActivity).FullName}: {details.Reason}");
                var internetProfile = NetworkInformation.GetInternetConnectionProfile();
                if (internetProfile == null)
                {
                    this.Log("No internet. Stop.");
                    return;
                }
                var instagram = Instagram.Instance;
                instagram.PushClient.MessageReceived += Utils.OnMessageReceived;
                if (details.Reason == SocketActivityTriggerReason.SocketClosed)
                {
                    await instagram.PushClient.StartFresh();
                }
                else
                {
                    var socket = details.SocketInformation.StreamSocket;
                    await instagram.PushClient.StartWithExistingSocket(socket);
                }
                await Task.Delay(TimeSpan.FromSeconds(5));  // Wait 5s to complete all outstanding IOs (hopefully)
                instagram.PushClient.ConnectionData.SaveToAppSettings();
                await instagram.PushClient.TransferPushSocket();
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
