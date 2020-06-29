using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indirect.Utilities;
using InstagramAPI.Classes;

namespace Indirect.Wrapper
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
