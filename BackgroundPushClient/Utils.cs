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
        public const string STATE_FILE_NAME = "state.bin";

        public static async Task<StateData> LoadStateAsync()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var stateFile = (StorageFile)await localFolder.GetItemAsync(STATE_FILE_NAME);
            using (var stateStream = await stateFile.OpenStreamForReadAsync())
            {
                var formatter = new BinaryFormatter();
                stateStream.Seek(0, SeekOrigin.Begin);
                var stateData = (StateData)formatter.Deserialize(stateStream);
                if (stateData.Cookies == null || stateData.FbnsConnectionData == null)
                {
                    throw new ArgumentException("State data doesn't have Cookies or FbnsConnectionData. Abort!");
                }

                return stateData;
            }
        }

        public static async Task<IBuffer> SaveStateAsync(StateData stateData)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var stateFile = (StorageFile)await localFolder.GetItemAsync(STATE_FILE_NAME);
            var formatter = new BinaryFormatter();
            using (var stateFileStream = await stateFile.OpenStreamForWriteAsync())
            {
                formatter.Serialize(stateFileStream, stateData);
            }

            var memoryStream = new MemoryStream();
            formatter.Serialize(memoryStream, stateData);
            var buffer = CryptographicBuffer.CreateFromByteArray(memoryStream.ToArray());
            return buffer;
        }

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
