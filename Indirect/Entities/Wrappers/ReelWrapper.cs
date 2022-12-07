using InstagramAPI.Classes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Indirect.Entities.Wrappers
{
    public partial class ReelWrapper : ObservableObject
    {
        [ObservableProperty] private Reel _source;

        public bool HasUnseenItems => Source.Seen != Source.LatestReelMedia;

        public ReelWrapper(Reel source)
        {
            Source = source;
        }
    }
}
