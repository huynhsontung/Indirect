using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using InstagramAPI;
using InstagramAPI.Classes.Android;
using InstagramAPI.Classes.Core;
using InstagramAPI.Push;
using InstagramAPI.Utils;

namespace BackgroundPushClient
{
    public sealed class UpdateTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Instagram.StartAppCenter();
            var deferral = taskInstance.GetDeferral();
            try
            {
                PushClient.UnregisterTasks();
                BackgroundExecutionManager.RemoveAccess();
                //await Task.Delay(TimeSpan.FromSeconds(15));  // Quota exception if there is no wait
                if (!await Utils.TryAcquireSyncLock())
                {
                    return;
                }

                var session = await SessionManager.TryLoadLastSessionAsync();
                if (session == null && Instagram.IsUserAuthenticatedPersistent)
                {
                    var device = AndroidDevice.CreateFromAppSettings();
                    var pushData = new FbnsConnectionData();
                    pushData.LoadFromAppSettings();
                    session = new UserSessionData(device, pushData);
                    session.LoadFromAppSettings();
                }

                if (session == null)
                {
                    return;
                }

                var instagram = new Instagram(session);

                if (instagram.IsUserAuthenticated)
                {
                    // Switch to file based session
                    await SessionManager.SaveSessionAsync(instagram);

                    UserSessionData.RemoveFromAppSettings();
                    AndroidDevice.RemoveFromAppSettings();
                    FbnsConnectionData.RemoveFromAppSettings();

                    await Task.Delay(TimeSpan.FromSeconds(3));
                    if (!PushClient.SocketRegistered() && await Utils.TryAcquireSyncLock())
                    {
                        instagram.PushClient.MessageReceived += Utils.OnMessageReceived;
                        instagram.PushClient.ExceptionsCaught += Utils.PushClientOnExceptionsCaught;
                        await instagram.PushClient.StartFresh();
                        await Task.Delay(TimeSpan.FromSeconds(PushClient.WaitTime));  // Wait 5s to complete all outstanding IOs (hopefully)
                        await instagram.PushClient.TransferPushSocket();
                        Utils.PopMessageToast("Push client started.");
                    }
                }

            }
            catch (Exception e)
            {
                Utils.PopMessageToast(e.ToString());
                DebugLogger.LogException(e);
            }
            finally
            {
                deferral.Complete();
            }
        }
    }
}
