using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
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
            Task.Run(async () => { await GetProfilePicture(ProfilePictureUrl); });
        }

        private async Task GetProfilePicture(string pictureUrl)
        {
            var response = await _instaApi.SendGetRequestAsync(new Uri(pictureUrl));
            var rawStream = await response.Content.ReadAsStreamAsync();

            await PageReference.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    ProfilePicture.DecodePixelHeight = 48;  // Match with PersonPicture ProfilePicture="{x:Bind Users[0].ProfilePicture, Mode=OneWay}" Height="48" Width="48" ...
                    await ProfilePicture.SetSourceAsync(rawStream.AsRandomAccessStream());
                });
        }
    }
}
