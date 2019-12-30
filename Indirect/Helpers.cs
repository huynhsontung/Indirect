using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using InstaSharper.API;

namespace Indirect
{
    internal class Helpers
    {
        public static TimeSpan DefaultCacheDuration = TimeSpan.FromDays(30);
        private static readonly StorageFolder TempFolder = ApplicationData.Current.TemporaryFolder;

        public static async Task<IRandomAccessStream> GetAndCacheObject(Uri target, IInstaApi api)
        {
            return await GetAndCacheObject(target, api, DefaultCacheDuration);
        }

        public static async Task<IRandomAccessStream> GetAndCacheObject(Uri target, IInstaApi api, TimeSpan cacheDuration)
        {
            var localPath = target.LocalPath.Replace('/', '\\');
            var localFile = (StorageFile)await TempFolder.TryGetItemAsync(localPath);
            if (localFile == null || DateTime.Now - localFile.DateCreated > cacheDuration)
            {
                var response = await api.SendGetRequestAsync(target);
                localFile =
                    await TempFolder.CreateFileAsync(localPath, CreationCollisionOption.ReplaceExisting);
                using (var fileStream = await localFile.OpenStreamForWriteAsync())
                {
                    await response.Content.CopyToAsync(fileStream);
                }
                var rawStream = await response.Content.ReadAsStreamAsync();
                return rawStream.AsRandomAccessStream();
            }
            else
            {
                var fileStream = await localFile.OpenAsync(FileAccessMode.Read);
                return fileStream;
            }
        }
    }
}