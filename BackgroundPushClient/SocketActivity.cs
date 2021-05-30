using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking.Sockets;
using InstagramAPI;
using InstagramAPI.Push;
using InstagramAPI.Utils;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace BackgroundPushClient
{
    public sealed class SocketActivity : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
#if !DEBUG
            AppCenter.Start(Secrets.APPCENTER_SECRET, typeof(Analytics), typeof(Crashes));
#endif
            var deferral = taskInstance.GetDeferral();
            this.Log("-------------- Start of background task --------------");
            var details = (SocketActivityTriggerDetails) taskInstance.TriggerDetails;
            this.Log($"{details.Reason}");
            try
            {
                if (!await Utils.TryAcquireSyncLock())
                {
                    this.Log("Failed to open SyncLock file. Main application might be running. Exit background task.");
                    return;
                }

                var instagram = new Instagram();
                instagram.PushClient.MessageReceived += Utils.OnMessageReceived;
                switch (details.Reason)
                {
                    case SocketActivityTriggerReason.KeepAliveTimerExpired:
                    case SocketActivityTriggerReason.SocketActivity:
                        StreamSocket socket = null;
                        try
                        {
                            socket = details.SocketInformation.StreamSocket;
                        }
                        catch (Exception)
                        {
                            // pass
                        }

                        if (socket != null)
                        {
                            instagram.PushClient.StartWithExistingSocket(socket);
                        }
                        else
                        {
                            await instagram.PushClient.StartFresh();
                        }

                        break;

                    case SocketActivityTriggerReason.SocketClosed:
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        if (!await Utils.TryAcquireSyncLock())
                        {
                            this.Log("Failed to open SyncLock file after extended wait. Main application might be running. Exit background task.");
                            return;
                        }

                        await instagram.PushClient.StartFresh();
                        break;

                    default:
                        return;
                }

                await Task.Delay(TimeSpan.FromSeconds(5));  // Wait 5s to complete all outstanding IOs (hopefully)
                instagram.PushClient.ConnectionData.SaveToAppSettings();
                await instagram.PushClient.TransferPushSocket(false);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e, properties: new Dictionary<string, string>
                {
                    {"SocketActivityTriggerReason", details.Reason.ToString()}
                });
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
