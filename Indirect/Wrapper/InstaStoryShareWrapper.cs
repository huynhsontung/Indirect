using InstaSharper.API;
using InstaSharper.Classes.Models.Story;

namespace Indirect.Wrapper
{
    class InstaStoryShareWrapper : InstaStoryShare
    {
        public new InstaMediaWrapper Media { get; set; }

        public InstaStoryShareWrapper(InstaStoryShare source, InstaApi api)
        {
            Media = new InstaMediaWrapper(source.Media, api);
            ReelType = source.ReelType;
            IsReelPersisted = source.IsReelPersisted;
            Text = source.Text;
            IsLinked = source.IsLinked;
            Title = source.Title;
            Message = source.Message;
        }
    }
}
