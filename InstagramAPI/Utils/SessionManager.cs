using InstagramAPI.Classes.Core;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage;
using Windows.Storage.Streams;

namespace InstagramAPI.Utils
{
    public static class SessionManager
    {
        private const string SESSION_EXT = ".session";
        private const string SESSION_PFP_EXT = ".jpg";

        private static readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;
        private static readonly StorageFolder LocalFolder = ApplicationData.Current.LocalFolder;

        public static string LastSessionName
        {
            get => (string)LocalSettings.Values["LastSessionName"];
            private set => LocalSettings.Values["LastSessionName"] = value;
        }

        public static async Task SaveSessionAsync(Instagram instagram)
        {
            var session = instagram.Session;
            if (!session.IsAuthenticated)
            {
                return;
            }

            if (session.LoggedInUser?.Pk == default)
            {
                throw new Exception("Session is authenticated but has no user data.");
            }

            session.Cookies = CookieHelper.GetCookies();
            var sessionName = session.LoggedInUser.Pk.ToString();
            var json = JsonConvert.SerializeObject(session, Formatting.None);
            var encoded = CryptographicBuffer.ConvertStringToBinary(json, BinaryStringEncoding.Utf8);
            var secured = await ProtectAsync(encoded);
            try
            {
                await WriteToFileAsync(sessionName + SESSION_EXT, secured);
            }
            catch (FileLoadException)
            {
                DebugLogger.Log(nameof(SessionManager),
                    $"Session file {sessionName + SESSION_EXT} is being used. Cannot write session to file.");
                return;
            }

            LastSessionName = sessionName;

            // Also save profile picture for preview later
            try
            {
                var pfpUrl = session.LoggedInUser.ProfilePictureUrl;
                if (pfpUrl == null)
                {
                    return;
                }

                var response = await instagram.GetAsync(pfpUrl);
                if (response.IsSuccessStatusCode)
                {
                    var pfpData = await response.Content.ReadAsBufferAsync();
                    await WriteToFileAsync(sessionName + SESSION_PFP_EXT, pfpData);
                }
            }
            catch (Exception)
            {
                // Not important if fail
                // pass
            }
        }

        public static async Task<UserSessionData> TryLoadLastSessionAsync()
        {
            return await TryLoadSessionAsync(LastSessionName) ?? await TryLoadFirstSessionAsync();
        }

        public static async Task<UserSessionData> TryLoadSessionAsync(string sessionName)
        {
            if (string.IsNullOrEmpty(sessionName))
            {
                return null;
            }

            var data = await TryReadFromFileAsync(sessionName + SESSION_EXT);
            if (data == null)
            {
                return null;
            }

            try
            {
                var encoded = await UnprotectAsync(data);
                var json = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, encoded);
                var session = JsonConvert.DeserializeObject<UserSessionData>(json);
                return session;
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                return null;
            }
        }

        public static async Task<UserSessionData> TryLoadFirstSessionAsync()
        {
            var files = await LocalFolder.GetFilesAsync();
            return await TryLoadSessionAsync(files.FirstOrDefault(x => x.FileType == SESSION_EXT)?.DisplayName);
        }

        public static async Task<UserSessionMetadata[]> GetAvailableSessionsAsync()
        {
            throw new NotImplementedException();
            var files = await LocalFolder.GetFilesAsync();
            return files.Where(x => x.FileType == SESSION_EXT).Select(x => new UserSessionMetadata
            {
                Username = x.DisplayName,
                ProfilePicture = new Uri($"{LocalFolder.Path}\\{x.DisplayName}{SESSION_PFP_EXT}")
            }).ToArray();
        }

        public static async Task<bool> TryRemoveSessionAsync(UserSessionData session)
        {
            if (session.LoggedInUser?.Pk == default)
            {
                return false;
            }

            var sessionName = session.LoggedInUser.Pk.ToString();
            var item = await LocalFolder.TryGetItemAsync(sessionName + SESSION_EXT);
            if (item == null)
            {
                return false;
            }

            try
            {
                await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch (Exception)
            {
                return false;
            }

            var pfp = await LocalFolder.TryGetItemAsync(sessionName + SESSION_PFP_EXT);
            if (pfp != null)
            {
                try
                {
                    await pfp.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
                catch (Exception )
                {
                    // pass
                }
            }

            return true;
        }

        private static IAsyncOperation<IBuffer> ProtectAsync(IBuffer data)
        {
            var provider = new DataProtectionProvider("LOCAL=user");
            return provider.ProtectAsync(data);
        }

        private static IAsyncOperation<IBuffer> UnprotectAsync(IBuffer data)
        {
            var provider = new DataProtectionProvider();
            return provider.UnprotectAsync(data);
        }

        private static async Task WriteToFileAsync(string fileName, IBuffer data)
        {
            fileName = SanitizeFileName(fileName);
            var file = await LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            using (var writeStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                await writeStream.WriteAsync(data);
                await writeStream.FlushAsync();
            }
        }

        private static async Task<IBuffer> TryReadFromFileAsync(string fileName)
        {
            fileName = SanitizeFileName(fileName);
            var file = await LocalFolder.TryGetItemAsync(fileName) as StorageFile;
            if (file == null)
            {
                return null;
            }

            using (var readStream = await file.OpenReadAsync())
            {
                var bytes = new byte[readStream.Size];
                var buffer = bytes.AsBuffer();
                await readStream.ReadAsync(buffer, (uint)readStream.Size, InputStreamOptions.None);
                return buffer;
            }
        }

        private static string SanitizeFileName(string name)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            var newName = string.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            return newName;
        }
    }
}
