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
                await SessionManager.RemoveLegacySessions();
                PushClient.UnregisterTasks();
                BackgroundExecutionManager.RemoveAccess();
                UnregisterLegacySocket();

                await Task.Delay(TimeSpan.FromSeconds(3));
                await Utils.RefreshAllPushSockets();
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
