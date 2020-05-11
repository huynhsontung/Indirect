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

        public StoryItemWrapper(StoryItem source)
        {
            PropertyCopier<StoryItem, StoryItemWrapper>.Copy(source, this);
        }
    }
}
