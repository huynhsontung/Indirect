using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Indirect.Utilities;
using InstagramAPI.Classes.Core;
using InstagramAPI.Classes.Media;

namespace Indirect.Entities.Wrappers
{
    public class ReelItemWrapper : ReelMedia, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ReelWrapper Parent { get; }

        private string _draftMessage;
        public string DraftMessage
        {
            get => _draftMessage;
            set
            {
                _draftMessage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DraftMessage)));
            }
        }

        private MainViewModel ViewModel { get; }

        private static readonly Regex EmojiRegex = new Regex(@"^(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])$");

        public ReelItemWrapper(ReelMedia source, ReelWrapper parent)
        {
            PropertyCopier<ReelMedia, ReelItemWrapper>.Copy(source, this);
            Parent = parent;
            ViewModel = ((App) Application.Current).ViewModel;
        }

        public async Task<bool> Reply(string message)
        {
            var userId = User.Pk;
            var resultThread = await ViewModel.InstaApi.CreateGroupThreadAsync(new[] { userId });
            if (!resultThread.IsSucceeded) return false;
            var thread = resultThread.Value;
            Result result;
            if (EmojiRegex.IsMatch(message))
            {
                result = await ViewModel.InstaApi.SendReelReactAsync(Parent.Id, Id, thread.ThreadId, message);
            }
            else
            {
                result = await ViewModel.InstaApi.SendReelShareAsync(Parent.Id, Id, MediaType, thread.ThreadId, message);
            }

            return result.IsSucceeded;
        }

        public async Task Download()
        {
            var url = Videos?.Length > 0 ? Videos[0].Url : Images.GetFullImageUri();
            if (url == null)
            {
                return;
            }

            await MediaHelpers.DownloadMedia(url).ConfigureAwait(false);
        }
    }
}
