using System.ComponentModel;
using InstagramAPI.Classes;

namespace Indirect.Entities.Wrappers
{
    public class ReelWrapper : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Reel Source
        {
            get => _source;
            set
            {
                _source = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Source)));
            }
        }

        public bool HasUnseenItems => Source.Seen != Source.LatestReelMedia;

        private Reel _source;

        public ReelWrapper(Reel source)
        {
            _source = source;
        }
    }
}
