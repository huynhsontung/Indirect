using System;
using System.Web;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;
using InstagramAPI;
using InstagramAPI.Utils;

namespace BackgroundPushClient
{
    public sealed class ReplyAction : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Instagram.StartAppCenter();
            var session = await SessionManager.TryLoadLastSessionAsync();
            if (session == null)
            {
                Utils.PopMessageToast("Reply failed. Account is not logged in. Tap this message to resolve this issue.");
                return;
            }

            var instagram = new Instagram(session);
            var deferral = taskInstance.GetDeferral();
            try
            {
                if (!instagram.IsUserAuthenticated) return;
                if (taskInstance.TriggerDetails is ToastNotificationActionTriggerDetail details)
                {
                    var arguments = HttpUtility.ParseQueryString(details.Argument);
                    var threadId = arguments["threadId"];
                    var action = arguments["action"];
                    var text = details.UserInput["text"] as string;
                    if (string.IsNullOrEmpty(threadId) || string.IsNullOrEmpty(text)) return;

                    await instagram.SendTextAsync(null, threadId, text);
                }
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