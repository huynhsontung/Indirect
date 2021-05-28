using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using InstagramAPI;
using InstagramAPI.Push;
using InstagramAPI.Utils;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace BackgroundPushClient
{
    public sealed class UpdateTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
#if !DEBUG
            AppCenter.Start(Secrets.APPCENTER_SECRET, typeof(Analytics), typeof(Crashes));
#endif
            var deferral = taskInstance.GetDeferral();
            try
            {
                PushClient.UnregisterTasks();
                await PushClient.RequestBackgroundAccess();
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
            }
            finally
            {
                deferral.Complete();
            }
        }
    }
}
