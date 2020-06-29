using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indirect.Utilities;
using InstagramAPI;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Media;

namespace Indirect.Wrapper
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

        public ReelItemWrapper(ReelMedia source, ReelWrapper parent)
        {
            PropertyCopier<ReelMedia, ReelItemWrapper>.Copy(source, this);
            Parent = parent;
        }

        public async Task Reply(string message)
        {
            var userId = User.Pk;
            var resultThread = await Instagram.Instance.CreateGroupThreadAsync(new[] { userId });
            if (!resultThread.IsSucceeded) return;
            var thread = resultThread.Value;
            await Instagram.Instance.SendReelShareAsync(Parent.Id, Id, MediaType, thread.ThreadId, message);
        }
    }
}
