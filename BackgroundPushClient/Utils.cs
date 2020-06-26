using System.Threading.Tasks;
using System.Web;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.Notifications;
using InstagramAPI;
using InstagramAPI.Push;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.IO;

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
                threadTitle = notificationContent.Message.Substring(0, notificationContent.Message.IndexOf(' '));
            var toastContent = new ToastContent
            {
                Header = new ToastHeader(threadId, threadTitle, string.Empty),
                Visual = new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric
                    {
                        Children =
                        {
                            new AdaptiveText
                            {
                                Text = notificationContent.Message
                            }
                        },
                        HeroImage = string.IsNullOrEmpty(notificationContent.OptionalImage)
                            ? null
                            : new ToastGenericHeroImage
                            {
                                Source = notificationContent.OptionalImage
                            },
                        AppLogoOverride = string.IsNullOrEmpty(args.NotificationContent.OptionalAvatarUrl)
                            ? null
                            : new ToastGenericAppLogo
                            {
                                Source = args.NotificationContent.OptionalAvatarUrl,
                                HintCrop = ToastGenericAppLogoCrop.Circle,
                                AlternateText = "Profile picture"
                            }
                    }
                },
                Launch = $"action=openThread&threadId={threadId}"
            };

            // Create the toast notification
            var toast = new ToastNotification(toastContent.GetXml())
            {
                Group = threadId,
                Tag = itemId,
                ExpiresOnReboot = false
            };
            if (ApiInformation.IsPropertyPresent(typeof(ToastNotification).FullName,
                nameof(ToastNotification.RemoteId)))
            {
                toast.RemoteId = notificationContent.PushId;
            }
            // And send the notification
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private static string GetThreadTitleFromAppSettings(string threadId)
        {
            if (string.IsNullOrEmpty(threadId)) return null;
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var composite = (Windows.Storage.ApplicationDataCompositeValue)localSettings.Values[Instagram.THREAD_TITLE_PERSISTENT_DICTIONARY_KEY];
            return (string) composite?[threadId];
        }

        public static async Task<bool> TryAcquireSyncLock()
        {
            var storageFolder = ApplicationData.Current.LocalFolder;
            var storageItem = await storageFolder.TryGetItemAsync("SyncLock.mutex");
            if (storageItem is StorageFile)
            {
                try
                {
                    var lockFile = new FileStream(storageItem.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    lockFile.Dispose();
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
