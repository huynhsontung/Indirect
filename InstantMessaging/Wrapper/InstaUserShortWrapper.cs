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

        private readonly StorageFolder _tempFolder;

        public InstaUserShortWrapper(InstaUserShort source, IInstaApi api)
        {
            _tempFolder = ApplicationData.Current.TemporaryFolder;
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
            var localPath = pictureUri.LocalPath.Replace('/', '\\');
            var profilePictureFile = (StorageFile) await _tempFolder.TryGetItemAsync(localPath);
            if (profilePictureFile == null || DateTime.Now - profilePictureFile.DateCreated > TimeSpan.FromDays(7))
            {
                var response = await _instaApi.SendGetRequestAsync(pictureUri);
                profilePictureFile =
                    await _tempFolder.CreateFileAsync(localPath, CreationCollisionOption.ReplaceExisting);
                using (var fileStream = await profilePictureFile.OpenStreamForWriteAsync())
                {
                    await response.Content.CopyToAsync(fileStream);
                }
                var rawStream = await response.Content.ReadAsStreamAsync();
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        try
                        {
                            await ProfilePicture.SetSourceAsync(rawStream.AsRandomAccessStream());
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    });
            }
            else
            {
                using (var fileStream = await profilePictureFile.OpenAsync(FileAccessMode.Read))
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        async () =>
                        {
                            try
                            {
                                await ProfilePicture.SetSourceAsync(fileStream);
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                        });
                }
            }
        }

        

    }
}
