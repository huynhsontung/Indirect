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
using Windows.UI.Xaml.Media.Imaging;

namespace Indirect.Utilities
{
    internal class Helpers
    {
        public static Uri GetUri(string url) => new Uri(url);

        public static bool IsHttpUri(Uri uri)
        {
            return uri.IsAbsoluteUri && (uri.Scheme == "http" || uri.Scheme == "https");
        }

        public static async Task<IBuffer> CompressImage(StorageFile imagefile, int reqWidth, int reqHeight)
        {
            //open file as stream
            using (IRandomAccessStream fileStream = await imagefile.OpenAsync(FileAccessMode.ReadWrite))
            {
                return await CompressImage(fileStream, reqWidth, reqHeight).ConfigureAwait(false);
            }
        }

        public static async Task<IBuffer> CompressImage(IRandomAccessStream stream, int reqWidth, int reqHeight)
        {
            var decoder = await BitmapDecoder.CreateAsync(stream);

            var resizedStream = new InMemoryRandomAccessStream();

            var propertySet = new BitmapPropertySet();
            propertySet.Add("ImageQuality", new BitmapTypedValue(0.9, PropertyType.Single));
            BitmapEncoder encoder =
                await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, resizedStream, propertySet);
            encoder.SetSoftwareBitmap(await decoder.GetSoftwareBitmapAsync());
            double widthRatio = (double)reqWidth / decoder.PixelWidth;
            double heightRatio = (double)reqHeight / decoder.PixelHeight;

            double scaleRatio = Math.Min(widthRatio, heightRatio);

            if (reqWidth == 0)
                scaleRatio = heightRatio;

            if (reqHeight == 0)
                scaleRatio = widthRatio;

            uint aspectHeight = (uint)Math.Floor(decoder.PixelHeight * scaleRatio);
            uint aspectWidth = (uint)Math.Floor(decoder.PixelWidth * scaleRatio);

            encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;

            encoder.BitmapTransform.ScaledHeight = aspectHeight;
            encoder.BitmapTransform.ScaledWidth = aspectWidth;
            await encoder.FlushAsync();
            resizedStream.Seek(0);
            var outBuffer = new Windows.Storage.Streams.Buffer((uint)resizedStream.Size);
            await resizedStream.ReadAsync(outBuffer, (uint)resizedStream.Size, InputStreamOptions.None);
            
            return outBuffer;
        }
    }
}