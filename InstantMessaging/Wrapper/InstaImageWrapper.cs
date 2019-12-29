using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;
using InstaSharper.API;
using InstaSharper.Classes.Models.Media;

namespace InstantMessaging.Wrapper
{
    class InstaImageWrapper : InstaImage
    {
        private readonly IInstaApi _instaApi;
        // private BitmapImage _image = new BitmapImage();
        // private bool _loaded = false;

        // public BitmapImage Image
        // {
        //     get
        //     {
        //         // Some images have multiple quality versions.
        //         // Only download an image when explicitly asked to do so.
        //         _ = GetImageAsync();
        //         return _image;
        //     }
        // }

        public BitmapImage Image;

        public InstaImageWrapper(InstaImage source, IInstaApi api)
        {
            _instaApi = api;
            Url = source.Url;
            Width = source.Width;
            Height = source.Height;
            ImageBytes = source.ImageBytes;
            // _image.DecodePixelHeight = Height;
            // _image.DecodePixelWidth = Width;
            Image = new BitmapImage(new Uri(Url))
            {
                DecodePixelHeight = Height,
                DecodePixelWidth = Width
            };
        }

        // private async Task GetImageAsync()
        // {
        //     if (_loaded) return;
        //     _loaded = true;
        //     if (string.IsNullOrEmpty(Url)) return;
        //     var pictureUri = new Uri(Url);
        //     var dataStream = await Helpers.GetAndCacheObject(pictureUri, _instaApi);
        //     await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
        //         CoreDispatcherPriority.Normal,
        //         async () =>
        //         {
        //             try
        //             {
        //                 await _image.SetSourceAsync(dataStream);
        //             }
        //             catch (Exception)
        //             {
        //                 // ignored
        //             }
        //         });
        // }

    }
}