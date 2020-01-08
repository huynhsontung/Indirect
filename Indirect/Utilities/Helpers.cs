using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Indirect.Wrapper;
using InstaSharper.API;

namespace Indirect.Utilities
{
    internal class Helpers
    {
        public static TimeSpan DefaultCacheDuration = TimeSpan.FromDays(30);
        private static readonly StorageFolder TempFolder = ApplicationData.Current.TemporaryFolder;

        public static Uri GetUri(string url) => new Uri(url);

        public static bool IsHttpUri(Uri uri)
        {
            return uri.IsAbsoluteUri && (uri.Scheme == "http" || uri.Scheme == "https");
        }

        public static async Task<IRandomAccessStream> GetAndCacheObject(Uri target, InstaApi api)
        {
            return await GetAndCacheObject(target, api, DefaultCacheDuration);
        }

        public static async Task<IRandomAccessStream> GetAndCacheObject(Uri target, InstaApi api,
            TimeSpan cacheDuration)
        {
            var localPath = target.LocalPath.Replace('/', '\\');
            var localFile = (StorageFile) await TempFolder.TryGetItemAsync(localPath);
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

        public static async Task<byte[]> CompressImage(StorageFile imagefile, int reqWidth, int reqHeight)
        {
            //open file as stream
            using (IRandomAccessStream fileStream = await imagefile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var decoder = await BitmapDecoder.CreateAsync(fileStream);

                var resizedStream = new InMemoryRandomAccessStream();

                var propertySet = new BitmapPropertySet();
                propertySet.Add("ImageQuality", new BitmapTypedValue(0.7, PropertyType.Single));
                BitmapEncoder encoder =
                    await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, resizedStream, propertySet);
                encoder.SetSoftwareBitmap(await decoder.GetSoftwareBitmapAsync());
                double widthRatio = (double) reqWidth / decoder.PixelWidth;
                double heightRatio = (double) reqHeight / decoder.PixelHeight;

                double scaleRatio = Math.Min(widthRatio, heightRatio);

                if (reqWidth == 0)
                    scaleRatio = heightRatio;

                if (reqHeight == 0)
                    scaleRatio = widthRatio;

                uint aspectHeight = (uint) Math.Floor(decoder.PixelHeight * scaleRatio);
                uint aspectWidth = (uint) Math.Floor(decoder.PixelWidth * scaleRatio);

                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;

                encoder.BitmapTransform.ScaledHeight = aspectHeight;
                encoder.BitmapTransform.ScaledWidth = aspectWidth;
                await encoder.FlushAsync();
                resizedStream.Seek(0);
                var outBuffer = new byte[resizedStream.Size];
                await resizedStream.ReadAsync(outBuffer.AsBuffer(), (uint) resizedStream.Size, InputStreamOptions.None);

                return outBuffer;
            }
        }
    }
}