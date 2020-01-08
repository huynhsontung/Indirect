using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Indirect.Wrapper;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Enums;

namespace Indirect
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
        public DataTemplate NotSupportedTemplate { get; set; }
        public DataTemplate UnexpectedTemplate { get; set; }

        private static readonly DataTemplate EmptyTemplate = new DataTemplate();

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            if (element != null && item != null && item is InstaDirectInboxItemWrapper inboxItem)
            {

                switch (inboxItem.ItemType)
                {
                    case InstaDirectThreadItemType.Like:
                        return LikeTemplate;

                    case InstaDirectThreadItemType.Hashtag:
                    case InstaDirectThreadItemType.Text when !string.IsNullOrEmpty(inboxItem.NavigateUri?.ToString()):
                        return HyperlinkTemplate;

                    case InstaDirectThreadItemType.Text:
                        return TextTemplate;

                    case InstaDirectThreadItemType.Link:
                        return HyperlinkWithPreviewTemplate;

                    case InstaDirectThreadItemType.ActionLog:
                        return EmptyTemplate;

                    case InstaDirectThreadItemType.MediaShare:
                        return MediaShareTemplate;

                    case InstaDirectThreadItemType.RavenMedia when inboxItem.VisualMedia.ViewMode != InstaViewMode.Permanent:
                        return HiddenMediaTemplate;

                    case InstaDirectThreadItemType.AnimatedMedia:
                    case InstaDirectThreadItemType.Media when inboxItem.Media.MediaType == InstaMediaType.Image:
                    case InstaDirectThreadItemType.RavenMedia when
                        inboxItem.RavenMedia?.MediaType == InstaMediaType.Image || inboxItem.VisualMedia?.Media.MediaType == InstaMediaType.Image:
                        return ImageTemplate;

                    case InstaDirectThreadItemType.Media when inboxItem.Media.MediaType == InstaMediaType.Video:
                    case InstaDirectThreadItemType.RavenMedia when
                        inboxItem.RavenMedia?.MediaType == InstaMediaType.Video || inboxItem.VisualMedia.Media.MediaType == InstaMediaType.Video:
                        return VideoTemplate;

                    case InstaDirectThreadItemType.ReelShare:
                        return ReelShareTemplate;

                    case InstaDirectThreadItemType.VoiceMedia:
                        return AudioTemplate;

                    case InstaDirectThreadItemType.Unknown:
                        return UnexpectedTemplate;

                    default:
                        return NotSupportedTemplate;
                }
            }
            return NotSupportedTemplate;
        }
    }
}