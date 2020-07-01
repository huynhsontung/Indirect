using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Indirect.Wrapper;
using InstagramAPI.Classes.Media;

namespace Indirect.Utilities
{
    class StoryTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ImageTemplate { get; set; }

        public DataTemplate VideoTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ReelItemWrapper story && story.MediaType == InstaMediaType.Video)
                return VideoTemplate;
            return ImageTemplate;
        }
    }
}
