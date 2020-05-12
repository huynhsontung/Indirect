using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indirect.Utilities;
using InstagramAPI.Classes.Story;

namespace Indirect.Wrapper
{
    public class StoryItemWrapper : StoryItem, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Reel Parent { get; }

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

        public StoryItemWrapper(StoryItem source, Reel parent)
        {
            PropertyCopier<StoryItem, StoryItemWrapper>.Copy(source, this);
            Parent = parent;
        }

        public Uri GetBestVideoResourceUri(VideoResource[] resources)
        {
            var main = resources.FirstOrDefault(x => x.Profile == VideoProfile.Main);
            return main != null ? main.Src : resources.FirstOrDefault(x => x.Profile == VideoProfile.Baseline)?.Src;
        }
    }
}
