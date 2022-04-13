using System.Threading.Tasks;
using Windows.UI.Xaml;
using Indirect.Utilities;
using InstagramAPI.Classes.Core;
using InstagramAPI.Classes.Media;
using NeoSmart.Unicode;

namespace Indirect.Entities.Wrappers
{
    public class ReelItemWrapper : DependencyObject
    {
        public static readonly DependencyProperty DraftMessageProperty = DependencyProperty.Register(
            nameof(DraftMessage),
            typeof(string),
            typeof(ReelItemWrapper),
            new PropertyMetadata(""));

        public ReelMedia Source { get; }

        public ReelWrapper Parent { get; }

        public string DraftMessage
        {
            get => (string) GetValue(DraftMessageProperty);
            set => SetValue(DraftMessageProperty, value);
        }

        private MainViewModel ViewModel { get; }

        public ReelItemWrapper(ReelMedia source, ReelWrapper parent)
        {
            Source = source;
            Parent = parent;
            ViewModel = ((App) Application.Current).ViewModel;
        }

        public async Task<bool> Reply(string message)
        {
            var userId = Source.User.Pk;
            var resultThread = await ViewModel.InstaApi.CreateGroupThreadAsync(new[] { userId });
            if (!resultThread.IsSucceeded) return false;
            var thread = resultThread.Value;
            Result result;
            if (Emoji.IsEmoji(message, 1))
            {
                result = await ViewModel.InstaApi.SendReelReactAsync(Parent.Source.Id, Source.Id, thread.ThreadId, message);
            }
            else
            {
                result = await ViewModel.InstaApi.SendReelShareAsync(Parent.Source.Id, Source.Id, Source.MediaType, thread.ThreadId, message);
            }

            return result.IsSucceeded;
        }

        public async Task Download()
        {
            var url = Source.Videos?.Length > 0 ? Source.Videos[0].Url : Source.Images.GetFullImageUri();
            if (url == null)
            {
                return;
            }

            await MediaHelpers.DownloadMedia(url).ConfigureAwait(false);
        }
    }
}
