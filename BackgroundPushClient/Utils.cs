using System.Threading.Tasks;
using System.Web;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.Notifications;
using InstagramAPI.Push;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BackgroundPushClient
{
    internal sealed class Utils
    {
        private static readonly ApplicationDataContainer LocalSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        private static CancellationTokenSource _rapidToast = new CancellationTokenSource();

        public static void OnMessageReceived(object sender, PushReceivedEventArgs args)
        {
            var notificationContent = args.NotificationContent;
            var igAction = notificationContent.IgAction;
            var querySeparatorIndex = igAction.IndexOf('?');
            var targetType = igAction.Substring(0, querySeparatorIndex);
            var queryParams = HttpUtility.ParseQueryString(igAction.Substring(querySeparatorIndex));
            var threadId = queryParams["id"];
            var itemId = queryParams["x"];
            var threadInfo = GetThreadInfoFromAppSettings(threadId);
            var threadTitle = string.Empty;
            long? threadUser = null;
            if (threadInfo != null)
            {
                threadTitle = threadInfo["title"]?.ToObject<string>();
                var users = threadInfo["users"]?.ToObject<long[]>();
                if (users?.Length == 1)
                {
                    threadUser = users[0];
                }
            }
            if (threadId == null || itemId == null || notificationContent.Message == null) return;
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
                Actions = new ToastActionsCustom
                {
                    Inputs =
                    {
                        new ToastTextBox("text")
                        {
                            PlaceholderContent = "Type a reply"
                        }
                    },
                    Buttons =
                    {
                        new ToastButton("Reply", $"action=reply&threadId={threadId}")
                        {
                            ActivationType = ToastActivationType.Background,
                            TextBoxId = "text",
                            ImageUri = "Assets/SendIcon.png"
                        }
                    }
                },
                Launch = $"action=open&threadId={threadId}",
                HintPeople = threadUser != null
                    ? new ToastPeople
                    {
                        RemoteId = $"{threadUser}@Indirect"
                    }
                    : null
            };

            // Create the toast notification
            var toast = new ToastNotification(toastContent.GetXml())
            {
                Group = threadId,
                Tag = itemId,
                ExpiresOnReboot = false
            };
            if (ApiInformation.IsPropertyPresent("Windows.UI.Notifications.ToastNotification", "RemoteId") &&
                !string.IsNullOrEmpty(notificationContent.PushId))
            {
                toast.RemoteId = notificationContent.PushId;
            }

            // And send the notification	
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        public static void PopMessageToast(string message)
        {
            var toastContent = new ToastContent
            {
                Header = new ToastHeader("message_toast", "Message toast", string.Empty),
                Visual = new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric
                    {
                        Children =
                        {
                            new AdaptiveText
                            {
                                Text = message
                            }
                        }
                    }
                }
            };

            var toast = new ToastNotification(toastContent.GetXml());
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        /// <summary>
        /// Hide previous toast early if another toast is ready to show up
        /// </summary>
        /// <param name="toast"></param>
        private static async void QueueRapidToast(ToastNotification toast)
        {
            _rapidToast?.Cancel();
            _rapidToast = new CancellationTokenSource();
            var notifier = ToastNotificationManager.CreateToastNotifier();
            notifier.Show(toast);
            try
            {
                await Task.Delay(5000, _rapidToast.Token);  // 5 seconds is default toast duration
            }
            catch (TaskCanceledException)
            {
                notifier.Hide(toast);   // Hide here will also remove toast from Action Center. We don't want that.
                // Create a replicate toast and send it straight to AC.
                // Cannot reuse the toast above because Show() doesn't allow a toast instance to be shown twice.
                var replicateToast = new ToastNotification(toast.Content)
                {
                    Group = toast.Group,
                    Tag = toast.Tag,
                    ExpiresOnReboot = toast.ExpiresOnReboot,
                    SuppressPopup = true
                };
                notifier.Show(replicateToast);
            }
        }

        private static JObject GetThreadInfoFromAppSettings(string threadId)
        {
            if (string.IsNullOrEmpty(threadId)) return null;
            var composite = (Windows.Storage.ApplicationDataCompositeValue)LocalSettings.Values["ThreadInfoPersistentDictionary"];
            var json = (string) composite?[threadId];
            return string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<JObject>(json);
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
