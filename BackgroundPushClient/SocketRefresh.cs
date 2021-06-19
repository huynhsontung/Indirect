using System;
using Windows.ApplicationModel.Background;
using InstagramAPI.Utils;

namespace BackgroundPushClient
{
    public sealed class SocketRefresh : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            try
            {
                await Utils.RefreshAllPushSockets();
                Utils.PopMessageToast("Successfully refreshed push sockets.");
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                Utils.PopMessageToast(e.ToString());
            }
            finally
            {
                deferral.Complete();
            }
        }
    }
}
