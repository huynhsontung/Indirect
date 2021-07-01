using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Indirect.Entities;
using Indirect.Entities.Wrappers;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Media;

namespace Indirect.Controls.Selectors
{
    internal class MediaTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ImageView { get; set; }

        public DataTemplate VideoView { get; set; }

        public DataTemplate ReelView { get; set; }

        protected override DataTemplate SelectTemplateCore(object obj, DependencyObject container)
        {
            if (obj is FlatReelsContainer)
            {
                return ReelView;
            }

            if (obj is DirectItemWrapper item)
            {
                switch (item.Source.ItemType)
                {
                    case DirectItemType.Media when item.Source.Media.MediaType == InstaMediaType.Image:
                    case DirectItemType.RavenMedia when
                        item.Source.RavenMedia?.MediaType == InstaMediaType.Image || item.Source.VisualMedia?.Media.MediaType == InstaMediaType.Image:
                        return ImageView;

                    case DirectItemType.Media when item.Source.Media.MediaType == InstaMediaType.Video:
                    case DirectItemType.RavenMedia when
                        item.Source.RavenMedia?.MediaType == InstaMediaType.Video || item.Source.VisualMedia?.Media.MediaType == InstaMediaType.Video:
                        return VideoView;

                    case DirectItemType.ReelShare:
                        return item.Source.ReelShareMedia.Media.MediaType == InstaMediaType.Image ? ImageView : VideoView;

                    case DirectItemType.StoryShare when item.Source.StoryShareMedia.Media != null:
                        return item.Source.StoryShareMedia.Media.MediaType == InstaMediaType.Image ? ImageView : VideoView;

                    default:
                        return null;
                }
            }

            return null;
        }
    }
}
