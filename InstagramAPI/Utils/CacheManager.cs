using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;
using Newtonsoft.Json;

namespace InstagramAPI.Utils
{
    public static class CacheManager
    {
        private static readonly StorageFolder CacheFolder = ApplicationData.Current.LocalCacheFolder;

        public static async Task WriteCacheAsync(string id, object obj)
        {
            try
            {
                var filename = SessionManager.SanitizeFileName(id) + ".json";
                var json = JsonConvert.SerializeObject(obj, Formatting.None);
                var buffer = CryptographicBuffer.ConvertStringToBinary(json, BinaryStringEncoding.Utf8);
                await WriteToFileAsync(filename, buffer);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
            }
        }

        public static async Task<T> ReadCacheAsync<T>(string id)
        {
            try
            {
                var filename = SessionManager.SanitizeFileName(id) + ".json";
                var buffer = await TryReadFromFileAsync(filename);
                if (buffer == null)
                {
                    return default;
                }

                var json = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, buffer);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                return default;
            }
        }

        public static async Task RemoveCacheAsync(string id)
        {
            try
            {
                var filename = SessionManager.SanitizeFileName(id) + ".json";
                var file = await CacheFolder.TryGetItemAsync(filename);
                if (file != null)
                {
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
            catch (Exception)
            {
                // pass
            }
        }

        private static async Task WriteToFileAsync(string fileName, IBuffer data)
        {
            var file = await CacheFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            using (var writeStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                await writeStream.WriteAsync(data);
                await writeStream.FlushAsync();
            }
        }

        private static async Task<IBuffer> TryReadFromFileAsync(string fileName)
        {
            var file = await CacheFolder.TryGetItemAsync(fileName) as StorageFile;
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
