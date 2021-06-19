using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking.Sockets;
using InstagramAPI;
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
                var sessions = await SessionManager.GetAvailableSessionsAsync();
                foreach (var container in sessions)
                {
                    var session = container.Session;
                    if (!await Utils.TryAcquireSyncLock(session.SessionName))
                    {
                        continue;
                    }

                    var instagram = new Instagram(session);

                    if (instagram.IsUserAuthenticated)
                    {
                        if (!instagram.PushClient.SocketRegistered())
                        {
                            instagram.PushClient.MessageReceived += Utils.OnMessageReceived;
                            instagram.PushClient.ExceptionsCaught += Utils.PushClientOnExceptionsCaught;
                            try
                            {
                                await instagram.PushClient.StartFresh();
                                await Task.Delay(TimeSpan.FromSeconds(PushClient.WaitTime));
                                await instagram.PushClient.TransferPushSocket();
                                await SessionManager.SaveSessionAsync(instagram);
                                Utils.PopMessageToast($"Push client for {session.LoggedInUser.Username} started.");
                            }
                            catch (Exception e)
                            {
                                Utils.PopMessageToast(e.ToString());
                                DebugLogger.LogException(e);
                            }
                        }
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
