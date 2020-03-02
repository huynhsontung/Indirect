using System.Collections.Generic;

namespace InstagramAPI.Classes
{
    public class StoryUploadOptions
    {
        public List<InstaStoryLocationUpload> Locations { get; set; } = new List<InstaStoryLocationUpload>();

        public List<InstaStoryHashtagUpload> Hashtags { get; set; } = new List<InstaStoryHashtagUpload>();

        public List<InstaStoryPollUpload> Polls { get; set; } = new List<InstaStoryPollUpload>();

        public InstaStorySliderUpload Slider { get; set; }

        public InstaStoryCountdownUpload Countdown { get; set; }

        // internal InstaMediaStoryUpload MediaStory { get; set; }
        //
        // public List<InstaStoryMentionUpload> Mentions { get; set; } = new List<InstaStoryMentionUpload>();

        public List<InstaStoryQuestionUpload> Questions { get; set; } = new List<InstaStoryQuestionUpload>();
    }
}