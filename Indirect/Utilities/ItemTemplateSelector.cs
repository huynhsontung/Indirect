using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Indirect.Wrapper;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Direct.ItemContent;
using InstagramAPI.Classes.Media;

namespace Indirect.Utilities
{
    class ItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate LikeTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate VideoTemplate { get; set; }
        public DataTemplate AudioTemplate { get; set; }
        public DataTemplate HiddenMediaTemplate { get; set; }
        public DataTemplate MediaShareTemplate { get; set; }
        public DataTemplate HyperlinkTemplate { get; set; }
        public DataTemplate HyperlinkWithPreviewTemplate { get; set; }
        public DataTemplate ReelShareTemplate { get; set; }
        public DataTemplate VideoCallTemplate { get; set; }
        public DataTemplate NotSupportedTemplate { get; set; }
        public DataTemplate UnexpectedTemplate { get; set; }
        public DataTemplate PlaceholderTemplate { get; set; }

        private static readonly DataTemplate EmptyTemplate = new DataTemplate();

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            if (element != null && item != null && item is InstaDirectInboxItemWrapper inboxItem)
            {

                switch (inboxItem.ItemType)
                {
                    case DirectItemType.Like:
                        return LikeTemplate;

                    case DirectItemType.Hashtag:
                    case DirectItemType.Text when !string.IsNullOrEmpty(inboxItem.NavigateUri?.ToString()):
                        return HyperlinkTemplate;

                    case DirectItemType.Text:
                        return TextTemplate;

                    case DirectItemType.Link:
                        return HyperlinkWithPreviewTemplate;

                    case DirectItemType.ActionLog:
                        return EmptyTemplate;

                    case DirectItemType.MediaShare:
                        return MediaShareTemplate;

                    case DirectItemType.RavenMedia when inboxItem.VisualMedia.ViewMode != VisualMediaViewMode.Permanent:
                        return HiddenMediaTemplate;

                    case DirectItemType.AnimatedMedia:
                    case DirectItemType.Media when inboxItem.Media.MediaType == InstaMediaType.Image:
                    case DirectItemType.RavenMedia when
                        inboxItem.RavenMedia?.MediaType == InstaMediaType.Image || inboxItem.VisualMedia?.Media.MediaType == InstaMediaType.Image:
                        return ImageTemplate;

                    case DirectItemType.Media when inboxItem.Media.MediaType == InstaMediaType.Video:
                    case DirectItemType.RavenMedia when
                        inboxItem.RavenMedia?.MediaType == InstaMediaType.Video || inboxItem.VisualMedia.Media.MediaType == InstaMediaType.Video:
                        return VideoTemplate;

                    case DirectItemType.ReelShare:
                        return ReelShareTemplate;

                    case DirectItemType.VoiceMedia:
                        return AudioTemplate;

                    case DirectItemType.Unknown:
                        return UnexpectedTemplate;

                    case DirectItemType.VideoCallEvent:
                        return VideoCallTemplate;

                    case DirectItemType.Placeholder:
                        return PlaceholderTemplate;

                    default:
                        return NotSupportedTemplate;
                }
            }
            return NotSupportedTemplate;
        }
    }
}