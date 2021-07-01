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
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using InstagramAPI;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Utils;

namespace BackgroundPushClient
{
    internal sealed class Utils
    {
        private static Dictionary<string, DirectThreadInfo> ThreadInfoDictionary { get; set; }
        private static readonly StorageFolder CacheFolder = ApplicationData.Current.LocalCacheFolder;

        private Instagram Instagram { get; }

        public Utils(Instagram instagram)
        {
            Instagram = instagram;
        }

        public async void OnMessageReceived(object sender, PushReceivedEventArgs args)
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
                var viewerId = notificationContent.IntendedRecipientUserId;
                if (threadId == null || itemId == null || notificationContent.Message == null ||
                    !await TryAcquireSyncLock(viewerId.ToString()))
                {
                    return;
                }

                var threadInfo = await GetThreadInfoAsync(threadId, viewerId);
                var threadTitle = "Unknown Thread";
                if (!string.IsNullOrEmpty(threadInfo?.Title))
                {
                    threadTitle = threadInfo.Title;
                }

                var loggedInUsers = GetLoggedInUsers();
                if (loggedInUsers.Count > 1 && loggedInUsers.ContainsKey(viewerId.ToString()))
                {

                    threadTitle = $"({loggedInUsers[viewerId.ToString()]}) {threadTitle}";
                }

                var avatarPath = args.NotificationContent.OptionalAvatarUrl;
                if (!string.IsNullOrEmpty(avatarPath))
                {
                    var path = await TryCacheImageAsync(new Uri(avatarPath));
                    if (!string.IsNullOrEmpty(path))
                    {
                        avatarPath = path;
                    }
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
                            AppLogoOverride = string.IsNullOrEmpty(avatarPath)
                                ? null
                                : new ToastGenericAppLogo
                                {
                                    Source = avatarPath,
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
                            new ToastButton("Reply", $"action=reply&threadId={threadId}&viewerId={viewerId}")
                            {
                                ActivationType = ToastActivationType.Background,
                                TextBoxId = "text",
                                ImageUri = "Assets/SendIcon.png"
                            }
                        }
                    },
                    Launch = $"action=open&threadId={threadId}&viewerId={viewerId}"
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

        private static async Task<DirectThreadInfo> GetThreadInfoAsync(string threadId, long viewerId)
        {
            if (ThreadInfoDictionary != null)
            {
                ThreadInfoDictionary.TryGetValue(threadId, out var info);
                return info;
            }

            var dict = await CacheManager.ReadCacheAsync<Dictionary<string, DirectThreadInfo>>(
                $"{nameof(ThreadInfoDictionary)}_{viewerId}");
            if (dict == null || !dict.ContainsKey(threadId))
            {
                return null;
            }

            ThreadInfoDictionary = dict;
            return dict[threadId];
        }

        private async Task<string> TryCacheImageAsync(Uri uri)
        {
            try
            {
                var filename = uri.Segments.LastOrDefault();
                if (string.IsNullOrEmpty(filename))
                {
                    return string.Empty;
                }

                var image = await CacheFolder.TryGetItemAsync(filename);
                if (image != null)
                {
                    return image.Path;
                }

                var response = await Instagram.HttpClient.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    var imageData = await response.Content.ReadAsByteArrayAsync();
                    if (imageData.Length == 0)
                    {
                        return string.Empty;
                    }

                    var imageFile = await CacheFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
                    using (var stream = await imageFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        await stream.WriteAsync(imageData.AsBuffer());
                        await stream.FlushAsync();
                    }

                    return imageFile.Path;
                }

                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static async Task<bool> TryAcquireSyncLock(string sessionName)
        {
            if (string.IsNullOrEmpty(sessionName))
            {
                return true;
            }

            var storageFolder = ApplicationData.Current.LocalFolder;
            var storageItem = await storageFolder.TryGetItemAsync($"SyncLock_{sessionName}.mutex");
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

        public static Dictionary<string, string> GetLoggedInUsers()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values["LoggedInUsers"] is ApplicationDataCompositeValue dict)
            {
                var dictionary = new Dictionary<string, string>(dict.Select(x =>
                    new KeyValuePair<string, string>(x.Key, (string) x.Value)));

                return dictionary;
            }

            return new Dictionary<string, string>(0);
        }

        public static async Task RefreshAllPushSockets()
        {
            var sessions = await SessionManager.GetAvailableSessionsAsync();
            foreach (var container in sessions)
            {
                var session = container.Session;
                if (!await TryAcquireSyncLock(session.SessionName) || !session.IsAuthenticated)
                {
                    continue;
                }

                var instagram = new Instagram(session);
                var lockFile = await TryAcquireSocketActivityLock(instagram.PushClient.SocketId);
                if (lockFile == null)
                {
                    continue;
                }

                try
                {
                    await RefreshPushSocket(instagram);
                }
                finally
                {
                    lockFile.Dispose();
                }
            }
        }

        public static async Task RefreshPushSocket(Instagram instagram)
        {
            if (!instagram.PushClient.SocketRegistered())
            {
                var utils = new Utils(instagram);
                instagram.PushClient.MessageReceived += utils.OnMessageReceived;
                instagram.PushClient.ExceptionsCaught += PushClientOnExceptionsCaught;
                try
                {
                    await instagram.PushClient.StartFresh();
                    await Task.Delay(TimeSpan.FromSeconds(PushClient.WaitTime));
                    await instagram.PushClient.TransferPushSocket();
                    await SessionManager.SaveSessionAsync(instagram, true);
                    PopMessageToast($"Push client for {instagram.Session.LoggedInUser.Username} started.");
                }
                catch (Exception e)
                {
                    PopMessageToast(e.ToString());
                    DebugLogger.LogException(e);
                }

                instagram.PushClient.MessageReceived -= utils.OnMessageReceived;
                instagram.PushClient.ExceptionsCaught -= PushClientOnExceptionsCaught;
            }
        }

        public static async Task<FileStream> TryAcquireSocketActivityLock(string socketId)
        {
            try
            {
                var storageFolder = ApplicationData.Current.LocalFolder;
                var storageItem = await storageFolder.CreateFileAsync(socketId + ".mutex", CreationCollisionOption.OpenIfExists);
                return new FileStream(storageItem.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
