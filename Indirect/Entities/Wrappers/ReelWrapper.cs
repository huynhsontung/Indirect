using Windows.UI.Xaml;
using InstagramAPI.Classes;

namespace Indirect.Entities.Wrappers
{
    public class ReelWrapper : DependencyObject
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(Reel),
            typeof(ReelWrapper),
            new PropertyMetadata(null));

        public Reel Source
        {
            get => (Reel)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public bool HasUnseenItems => Source.Seen != Source.LatestReelMedia;

        public ReelWrapper(Reel source)
        {
            Source = source;
        }
    }
}
