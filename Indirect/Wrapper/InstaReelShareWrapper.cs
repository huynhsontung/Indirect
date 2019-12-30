using InstaSharper.API;
using InstaSharper.Classes.Models.Story;

namespace Indirect.Wrapper
{
    class InstaReelShareWrapper : InstaReelShare
    {
        public new InstaStoryItem Media { get; set; }

        public InstaReelShareWrapper(InstaReelShare source, IInstaApi api)
        {
            // todo: finish ReelShareWrapper
        }
    }
}
