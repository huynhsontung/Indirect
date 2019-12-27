using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using InstaSharper.API;
using InstaSharper.Classes.Models.User;

namespace InstantMessaging.Wrapper
{
    public class InstaUserShortWrapper : InstaUserShort
    {
        private readonly IInstaApi _instaApi;
        public BitmapImage ProfilePicture { get; set; } = new BitmapImage();

        private readonly StorageFolder _tempFolder = ApplicationData.Current.TemporaryFolder;

        public InstaUserShortWrapper(InstaUserShort source, IInstaApi api)
        {
            _instaApi = api;
            IsVerified = source.IsVerified;
            IsPrivate = source.IsPrivate;
            Pk = source.Pk;
            ProfilePictureUrl = source.ProfilePictureUrl;
            ProfilePictureId = source.ProfilePictureId;
            UserName = source.UserName;
            FullName = source.FullName;
            Task.Run(async () => {if(!string.IsNullOrEmpty(ProfilePictureUrl)) await GetProfilePicture(ProfilePictureUrl); });
        }

        protected async Task GetProfilePicture(string pictureUrl)
        {
            var pictureUri = new Uri(pictureUrl);
            var dataStream = await Helpers.GetAndCacheObject(pictureUri, _instaApi);
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                async () =>
                {
                    try
                    {
                        await ProfilePicture.SetSourceAsync(dataStream);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                });
        }

        

    }
}
