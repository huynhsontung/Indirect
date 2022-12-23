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
            var deferral = taskInstance.GetDeferral();
            try
            {
                if (taskInstance.TriggerDetails is ToastNotificationActionTriggerDetail details)
                {
                    var arguments = HttpUtility.ParseQueryString(details.Argument);
                    var threadId = details.Argument.Contains("threadId") ? arguments["threadId"] : null;
                    var action = details.Argument.Contains("action") ? arguments["action"] : null;
                    var viewerId = details.Argument.Contains("viewerId") ? arguments["viewerId"] : null;
                    var text = details.UserInput["text"] as string;
                    var session = await SessionManager.TryLoadSessionAsync(viewerId);
                    if (session == null)
                    {
                        Utils.PopMessageToast("Reply failed. Account is not logged in. Tap this message to resolve this issue.");
                        return;
                    }

                    var instagram = new Instagram(session);
                    if (!instagram.IsUserAuthenticated) return;
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