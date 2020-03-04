using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Web;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
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
            var toastContent = new ToastContent()
            {
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
    }
}
