using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstagramAPI.Classes.Story;

namespace Indirect
{
    internal partial class ApiContainer
    {
        public readonly ObservableCollection<Reel> ReelsFeed = new ObservableCollection<Reel>();
    }
}
