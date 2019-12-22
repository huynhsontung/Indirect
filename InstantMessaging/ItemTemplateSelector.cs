using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using InstaSharper.Classes.Models.Direct;

namespace InstantMessaging
{
    class ItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate LikeTemplate { get; set; }
        public DataTemplate NotSupportedTemplate { get; set; }

        private static readonly DataTemplate EmptyTemplate = new DataTemplate();

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            if (element != null && item != null && item is InstaDirectInboxItem inboxItem)
            {
                switch (inboxItem.ItemType)
                {
                    case InstaDirectThreadItemType.Like:
                        return LikeTemplate;
                        
                    case InstaDirectThreadItemType.Text:
                        return TextTemplate;

                    case InstaDirectThreadItemType.ActionLog:
                        return EmptyTemplate;
                    
                    default:
                        return NotSupportedTemplate;
                }
            }
            return NotSupportedTemplate;
        }
    }
}
