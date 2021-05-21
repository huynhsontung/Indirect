using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace Indirect.Utilities
{
    public static class Helpers
    {
        public static Uri GetUri(string url) => new Uri(url);

        public static bool IsHttpUri(Uri uri)
        {
            return uri.IsAbsoluteUri && (uri.Scheme == "http" || uri.Scheme == "https");
        }

        public static List<string> ExtractLinks(string text)
        {
            text = text.Replace('\r', '\n');
            var tokens = text.Split("\t\n ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var links = tokens.Where(x =>
                !string.IsNullOrEmpty(x) &&
                (x.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
                 x.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase) ||
                 x.StartsWith("www.", StringComparison.InvariantCultureIgnoreCase))).ToList();
            return links;
        }

        public static async Task<IBuffer> CompressImage(StorageFile imagefile, int reqWidth, int reqHeight)
        {
            if (imagefile == null) throw new ArgumentNullException(nameof(imagefile));
            //open file as stream
            using (IRandomAccessStream fileStream = await imagefile.OpenAsync(FileAccessMode.Read))
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
            resizedStream.Dispose();
            return outBuffer;
        }

        public static async Task QuickRunAsync(this CoreDispatcher dispatcher, DispatchedHandler agileCallback,
            CoreDispatcherPriority priority = CoreDispatcherPriority.Normal)
        {
            if (dispatcher.HasThreadAccess)
            {
                agileCallback.Invoke();
            }
            else
            {
                await dispatcher.RunAsync(priority, agileCallback);
            }
        }
    }
}