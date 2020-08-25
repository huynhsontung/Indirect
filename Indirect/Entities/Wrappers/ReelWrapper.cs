using System.ComponentModel;
using Indirect.Utilities;
using InstagramAPI.Classes;

namespace Indirect.Entities.Wrappers
{
    public class ReelWrapper : Reel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnSeenChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasUnseenItems)));

        public ReelWrapper(Reel source)
        {
            PropertyCopier<Reel, ReelWrapper>.Copy(source, this);
        }
    }
}
