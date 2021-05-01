using InstagramAPI.Classes.Core;
using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage;
using Windows.Storage.Streams;

namespace InstagramAPI.Utils
{
    internal static class StorageHelper
    {
        private const string SESSION_EXT = ".session";

        private static readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;
        private static readonly StorageFolder LocalFolder = ApplicationData.Current.LocalFolder;

        public static bool IsUserAuthenticated
        {
            get => (bool?)LocalSettings.Values["IsUserAuthenticated"] ?? false;
            set => LocalSettings.Values["IsUserAuthenticated"] = value;
        }

        public static string SessionUsername
        {
            get => (string)LocalSettings.Values["SessionUsername"];
            set => LocalSettings.Values["SessionUsername"] = value;
        }

        public static async Task SaveSessionAsync(UserSessionData session)
        {
            if (string.IsNullOrEmpty(session.Username))
            {
                return;
            }

            var sessionName = session.Username;
            var json = JsonConvert.SerializeObject(session, Formatting.None, new TimestampConverter());
            var encoded = CryptographicBuffer.ConvertStringToBinary(json, BinaryStringEncoding.Utf8);
            var secured = await ProtectAsync(encoded);
            await WriteToFileAsync(sessionName + SESSION_EXT, secured);
        }

        public static async Task<UserSessionData> TryLoadSessionAsync(string sessionName)
        {
            var data = await TryReadFromFileAsync(sessionName + SESSION_EXT);
            if (data == null)
            {
                return null;
            }

            var encoded = await UnprotectAsync(data);
            var json = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, encoded);
            var session = JsonConvert.DeserializeObject<UserSessionData>(json, new TimestampConverter());
            return session;
        }

        public static IAsyncOperation<IBuffer> ProtectAsync(IBuffer data)
        {
            var provider = new DataProtectionProvider("LOCAL=user");
            return provider.ProtectAsync(data);
        }

        public static IAsyncOperation<IBuffer> UnprotectAsync(IBuffer data)
        {
            var provider = new DataProtectionProvider();
            return provider.UnprotectAsync(data);
        }

        public static async Task<string[]> GetAvailableSessionsAsync()
        {
            var files = await LocalFolder.GetFilesAsync();
            return files.Where(x => x.FileType == SESSION_EXT).Select(x => x.DisplayName).ToArray();
        }

        public static async Task<bool> RemoveSession(string sessionName)
        {
            if (string.IsNullOrEmpty(sessionName))
            {
                return false;
            }

            var item = await LocalFolder.TryGetItemAsync(sessionName + SESSION_EXT);
            if (item == null)
            {
                return false;
            }

            await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
            return true;
        }

        private static async Task WriteToFileAsync(string fileName, IBuffer data)
        {
            var file = await LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            using (var writeStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                await writeStream.WriteAsync(data);
                await writeStream.FlushAsync();
            }
        }

        private static async Task<IBuffer> TryReadFromFileAsync(string fileName)
        {
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
    }
}
