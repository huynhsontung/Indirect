using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking.Sockets;
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
                UnregisterLegacySocket();

                await Task.Delay(TimeSpan.FromSeconds(3));
                if (!await Utils.TryAcquireSyncLock())
                {
                    return;
                }

                var sessions = await SessionManager.GetAvailableSessionsAsync();
                var tasks = sessions.Select(async s =>
                {
                    var session = s.Session;
                    var instagram = new Instagram(session);

                    if (instagram.IsUserAuthenticated)
                    {
                        if (!instagram.PushClient.SocketRegistered())
                        {
                            instagram.PushClient.MessageReceived += Utils.OnMessageReceived;
                            instagram.PushClient.ExceptionsCaught += Utils.PushClientOnExceptionsCaught;
                            await instagram.PushClient.StartFresh();
                            await Task.Delay(TimeSpan.FromSeconds(PushClient.WaitTime));
                            await instagram.PushClient.TransferPushSocket();
                            await SessionManager.SaveSessionAsync(instagram);
                            Utils.PopMessageToast($"Push client for {session.LoggedInUser.Username} started.");
                        }
                    }
                });

                await Task.WhenAll(tasks);
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

        private void UnregisterLegacySocket()
        {
            if (SocketActivityInformation.AllSockets.ContainsKey(PushClient.SocketIdLegacy))
            {
                try
                {
                    SocketActivityInformation.AllSockets[PushClient.SocketIdLegacy].StreamSocket.Dispose();
                }
                catch (Exception e)
                {
                    Utils.PopMessageToast(e.ToString());
                }
            }
        }
    }
}
