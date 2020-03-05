using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

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

    public abstract class StoryUploadBaseOption
    {
        public double X { get; set; } = 0.5;
        public double Y { get; set; } = 0.5;
        public double Z { get; set; } = 0;

        public double Width { get; set; } = 0.7416667;
        public double Height { get; set; } = 0.08751394;
        public double Rotation { get; set; } = 0.0;

        public abstract JObject ToJson();
    }

    public class InstaStoryLocationUpload : StoryUploadBaseOption
    {
        /// <summary>
        ///     Location id (get it from <seealso cref="ILocationProcessor.SearchLocationAsync"/> )
        /// </summary>
        public string LocationId { get; set; }

        public bool IsSticker { get; set; } = false;

        public override JObject ToJson()
        {
            return new JObject
            {
                {"x", X},
                {"y", Y},
                {"z", Z},
                {"width", Width},
                {"height", Height},
                {"rotation", Rotation},
                {"location_id", LocationId},
                {"is_sticker", IsSticker},
            };
        }
    }

    public class InstaStoryHashtagUpload : StoryUploadBaseOption
    {
        public string TagName { get; set; }

        public bool IsSticker { get; set; } = false;

        public override JObject ToJson()
        {
            return new JObject
            {
                {"x", X},
                {"y", Y},
                {"z", Z},
                {"width", Width},
                {"height", Height},
                {"rotation", Rotation},
                {"tag_name", TagName},
                {"is_sticker", IsSticker},
            };
        }
    }

    public class InstaStoryPollUpload : StoryUploadBaseOption
    {
        public string Question { get; set; }

        public string Answer1 { get; set; } = "YES";
        public string Answer2 { get; set; } = "NO";

        public double Answer1FontSize { get; set; } = 35.0;
        public double Answer2FontSize { get; set; } = 35.0;

        public bool IsSticker { get; set; } = false;

        public override JObject ToJson()
        {
            var jArray = new JArray
            {
                new JObject
                {
                    {"text", Answer1},
                    {"count", 0},
                    {"font_size", Answer1FontSize}
                },
                new JObject
                {
                    {"text", Answer2},
                    {"count", 0},
                    {"font_size", Answer2FontSize}
                },
            };

            return new JObject
            {
                {"x", X},
                {"y", Y},
                {"z", Z},
                {"width", Width},
                {"height", Height},
                {"rotation", Rotation},
                {"question", Question},
                {"viewer_vote", 0},
                {"viewer_can_vote", true},
                {"tallies", jArray},
                {"is_shared_result", false},
                {"finished", false},
                {"is_sticker", IsSticker},
            };
        }
    }

    public class InstaStorySliderUpload : StoryUploadBaseOption
    {
        public string Question { get; set; }

        public string BackgroundColor { get; set; } = "#ffffff";
        public string Emoji { get; set; } = "😍";

        public string TextColor { get; set; } = "#000000";

        public bool IsSticker { get; set; } = false;

        public override JObject ToJson()
        {
            return new JObject
            {
                {"x", X},
                {"y", Y},
                {"z", Z},
                {"width", Width},
                {"height", Height},
                {"rotation", Rotation},
                {"question", Question},
                {"viewer_can_vote", true},
                {"viewer_vote", -1.0},
                {"slider_vote_average", 0.0},
                {"background_color", BackgroundColor},
                {"emoji", $"{Emoji}"},
                {"text_color", TextColor},
                {"is_sticker", IsSticker},
            };
        }
    }

    public class InstaStoryCountdownUpload : StoryUploadBaseOption
    {
        public DateTimeOffset EndTime { get; set; } = DateTimeOffset.UtcNow.AddDays(1);
        public string Text { get; set; }
        public string StartBackgroundColor { get; set; } = "#ffffff";
        public string EndBackgroundColor { get; set; } = "#ffffff";
        public string TextColor { get; set; } = "#000000";

        public string DigitColor { get; set; } = "#4286f4";
        public string DigitCardColor { get; set; } = "#42dcf4";

        public bool FollowingEnabled { get; set; } = true;

        public bool IsSticker { get; set; } = false;

        public override JObject ToJson()
        {
            return new JObject
            {
                {"x", X},
                {"y", Y},
                {"z", Z},
                {"width", Width},
                {"height", Height},
                {"rotation", Rotation},
                {"text", Text},
                {"start_background_color", StartBackgroundColor},
                {"end_background_color", EndBackgroundColor},
                {"digit_color", DigitColor},
                {"digit_card_color", DigitCardColor},
                {"end_ts", EndTime.ToUnixTimeSeconds()},
                {"text_color", TextColor},
                {"following_enabled", FollowingEnabled},
                {"is_sticker", IsSticker}
            };
        }
    }

    public class InstaStoryQuestionUpload : StoryUploadBaseOption
    {
        public bool ViewerCanInteract { get; set; } = true;
        public string BackgroundColor { get; set; } = "#ffffff";
        public string TextColor { get; set; } = "#000000";

        public string Question { get; set; }

        internal bool IsSticker { get; set; } = true;
        internal string ProfilePicture { get; set; }
        internal string QuestionType { get; set; } = "text";

        public override JObject ToJson()
        {
            return new JObject
            {
                {"x", X},
                {"y", Y},
                {"z", Z},
                {"width", Width},
                {"height", Height},
                {"rotation", Rotation},
                {"question", Question},
                {"viewer_can_interact", ViewerCanInteract},
                {"profile_pic_url", ProfilePicture},
                {"question_type", QuestionType},
                {"background_color", BackgroundColor},
                {"text_color", TextColor},
                {"is_sticker", IsSticker},
            };
        }
    }
}