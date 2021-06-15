using System.Threading.Tasks;
using System.Web;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.Notifications;
using InstagramAPI.Push;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Utils;

namespace BackgroundPushClient
{
    internal sealed class Utils
    {
        private static Dictionary<string, DirectThreadInfo> ThreadInfoDictionary { get; set; }

        public static async void OnMessageReceived(object sender, PushReceivedEventArgs args)
        {
            try
            {
                var notificationContent = args.NotificationContent;
                var igAction = notificationContent.IgAction;
                var querySeparatorIndex = igAction.IndexOf('?');
                var targetType = igAction.Substring(0, querySeparatorIndex);
                var queryParams = HttpUtility.ParseQueryString(igAction.Substring(querySeparatorIndex));
                var threadId = queryParams["id"];
                var itemId = queryParams["x"];
                if (threadId == null || itemId == null || notificationContent.Message == null || !await TryAcquireSyncLock())
                {
                    return;
                }

                var threadInfo = await GetThreadInfoAsync(threadId);
                var threadTitle = "Unknown Thread";
                if (!string.IsNullOrEmpty(threadInfo?.Title))
                {
                    threadTitle = threadInfo.Title;
                }

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
                    Launch = $"action=open&threadId={threadId}"
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
            catch (Exception e)
            {
                PopMessageToast(e.ToString());
                DebugLogger.LogException(e);
            }
        }

        public static void PopMessageToast(string message)
        {
#if DEBUG
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
#endif
        }

        public static void PushClientOnExceptionsCaught(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;
            PopMessageToast(exception.ToString());
        }

        private static async Task<DirectThreadInfo> GetThreadInfoAsync(string threadId)
        {
            if (ThreadInfoDictionary != null)
            {
                ThreadInfoDictionary.TryGetValue(threadId, out var info);
                return info;
            }

            var dict = await CacheManager.ReadCacheAsync<Dictionary<string, DirectThreadInfo>>(nameof(ThreadInfoDictionary));
            if (dict == null || !dict.ContainsKey(threadId))
            {
                return null;
            }

            ThreadInfoDictionary = dict;
            return dict[threadId];
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
