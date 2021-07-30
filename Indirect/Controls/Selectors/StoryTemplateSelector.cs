using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Indirect.Entities.Wrappers;
using InstagramAPI.Classes.Media;

namespace Indirect.Controls.Selectors
{
    class StoryTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ImageTemplate { get; set; }

        public DataTemplate VideoTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ReelItemWrapper story && story.Source.MediaType == InstaMediaType.Video)
                return VideoTemplate;
            return ImageTemplate;
        }
    }
}
