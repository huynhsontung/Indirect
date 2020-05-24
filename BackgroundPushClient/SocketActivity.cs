using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking.Sockets;
using InstagramAPI;

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
                var instagram = Instagram.Instance;
                var details = (SocketActivityTriggerDetails) taskInstance.TriggerDetails;
                if (details.Reason == SocketActivityTriggerReason.SocketClosed) return;
                Debug.WriteLine($"{typeof(SocketActivity).FullName}: {details.Reason}");
                
                var socket = details.SocketInformation.StreamSocket;
                instagram.PushClient.MessageReceived += Utils.OnMessageReceived;
                await instagram.PushClient.StartWithExistingSocket(socket);

                // We don't need to handle SocketActivity event. Push client will take care of that.
                if (details.Reason == SocketActivityTriggerReason.KeepAliveTimerExpired)
                {
                    await instagram.PushClient.SendPing();
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
