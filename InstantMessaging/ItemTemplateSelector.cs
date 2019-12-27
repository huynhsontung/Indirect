using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using InstantMessaging.Wrapper;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Enums;

namespace InstantMessaging
{
    class ItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate NoBorderTemplate { get; set; }
        public DataTemplate InlineImageTemplate { get; set; }
        public DataTemplate NotSupportedTemplate { get; set; }

        private static readonly DataTemplate EmptyTemplate = new DataTemplate();

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            if (element != null && item != null && item is InstaDirectInboxItemWrapper inboxItem)
            {
                switch (inboxItem.ItemType)
                {
                    case InstaDirectThreadItemType.Like:
                        return NoBorderTemplate;
                        
                    case InstaDirectThreadItemType.Text:
                        return TextTemplate;

                    case InstaDirectThreadItemType.ActionLog:
                        return EmptyTemplate;

                    case InstaDirectThreadItemType.Media when inboxItem.Media.MediaType == InstaMediaType.Image:
                    case InstaDirectThreadItemType.RavenMedia when 
                        inboxItem.RavenMedia?.MediaType == InstaMediaType.Image || inboxItem.VisualMedia?.Media.MediaType == InstaMediaType.Image:
                        return InlineImageTemplate;

                    default:
                        return NotSupportedTemplate;
                }
            }
            return NotSupportedTemplate;
        }
    }
}
