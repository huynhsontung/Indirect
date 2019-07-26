using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
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

        public static MainPage PageReference;

        public BitmapImage ProfilePicture { get; set; } = new BitmapImage();

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
            var response = await _instaApi.SendGetRequestAsync(new Uri(pictureUrl));
            var rawStream = await response.Content.ReadAsStreamAsync();
            
            // If called when layout is being populated (for base class call)
            await PageReference.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    await ProfilePicture.SetSourceAsync(rawStream.AsRandomAccessStream());
                });
        }

        

    }
}
