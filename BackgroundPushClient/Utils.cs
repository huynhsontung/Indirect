using System.Web;
using Windows.UI.Notifications;
using InstagramAPI;
using InstagramAPI.Push;
using Microsoft.Toolkit.Uwp.Notifications;

namespace BackgroundPushClient
{
    internal sealed class Utils
    {
        public static void OnMessageReceived(object sender, PushReceivedEventArgs args)
        {
            var notificationContent = args.NotificationContent;
            var igAction = notificationContent.IgAction;
            var querySeparatorIndex = igAction.IndexOf('?');
            var targetType = igAction.Substring(0, querySeparatorIndex);
            var queryParams = HttpUtility.ParseQueryString(igAction.Substring(querySeparatorIndex));
            var threadId = queryParams["id"];
            var itemId = queryParams["x"];
            var threadTitle = GetThreadTitleFromAppSettings(threadId);
            if (string.IsNullOrEmpty(threadTitle))
                threadTitle = notificationContent.Message.Substring(notificationContent.Message.IndexOf(' '));
            var toastContent = new ToastContent()
            {
                Header = new ToastHeader(threadId, threadTitle, string.Empty),
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = notificationContent.Message
                            }
                        },
                        AppLogoOverride = string.IsNullOrEmpty(args.NotificationContent.OptionalAvatarUrl)
                            ? null
                            : new ToastGenericAppLogo()
                            {
                                Source = args.NotificationContent.OptionalAvatarUrl,
                                HintCrop = ToastGenericAppLogoCrop.Circle,
                                AlternateText = "Profile picture"
                            }
                    }
                }
            };

            // Create the toast notification
            var toast = new ToastNotification(toastContent.GetXml())
            {
                Group = threadId,
                Tag = itemId,
                ExpiresOnReboot = false
            };
            // And send the notification
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private static string GetThreadTitleFromAppSettings(string threadId)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var composite = (Windows.Storage.ApplicationDataCompositeValue)localSettings.Values[Instagram.THREAD_TITLE_PERSISTENT_DICTIONARY_KEY];
            return (string) composite?[threadId];
        }
    }
}
