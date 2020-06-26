using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using InstagramAPI;
using InstagramAPI.Utils;

namespace BackgroundPushClient
{
    public sealed class InternetAvailable : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            try
            {
                this.Log("Internet available background task triggered");
                if (!await Utils.TryAcquireSyncLock())
                {
                    this.Log("Failed to open SyncLock file. Main application might be running. Exit background task.");
                    return;
                }
                var instagram = Instagram.Instance;
                instagram.PushClient.MessageReceived += Utils.OnMessageReceived;
                await instagram.PushClient.StartFresh();

                await Task.Delay(TimeSpan.FromSeconds(5));  // Wait 5s to complete all outstanding IOs (hopefully)
                instagram.PushClient.ConnectionData.SaveToAppSettings();
                await instagram.PushClient.TransferPushSocket();
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                this.Log($"{typeof(InternetAvailable).FullName}: Can't finish push cycle. Abort.");
            }
            finally
            {
                deferral.Complete();
            }
        }
    }
}
