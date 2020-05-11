using System;
using System.Runtime.Serialization;
using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace InstagramAPI.Classes.Story
{
    public class StoryItem : IEquatable<StoryItem>
    {
        [JsonProperty("__typename")]
        [JsonConverter(typeof(StringEnumConverter))]
        public StoryItemType Typename { get; set; }

        [JsonProperty("audience")]
        public string Audience { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("dimensions")]
        public Dimensions Dimensions { get; set; }

        [JsonProperty("story_view_count")]
        public object StoryViewCount { get; set; }

        //[JsonProperty("edge_story_media_viewers")]
        //public EdgeStoryMediaViewers EdgeStoryMediaViewers { get; set; }

        [JsonProperty("display_resources")]
        public DisplayResource[] DisplayResources { get; set; }

        [JsonProperty("display_url")]
        public Uri DisplayUrl { get; set; }

        [JsonProperty("media_preview")]
        public string MediaPreview { get; set; }

        [JsonProperty("gating_info")]
        public object GatingInfo { get; set; }

        [JsonProperty("fact_check_overall_rating")]
        public object FactCheckOverallRating { get; set; }

        [JsonProperty("fact_check_information")]
        public object FactCheckInformation { get; set; }

        [JsonProperty("taken_at_timestamp")]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset TakenAtTimestamp { get; set; }

        [JsonProperty("expiring_at_timestamp")]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset ExpiringAtTimestamp { get; set; }

        [JsonProperty("story_cta_url")]
        public Uri StoryCtaUrl { get; set; }

        [JsonProperty("is_video")]
        public bool IsVideo { get; set; }

        [JsonProperty("owner")]
        public Owner Owner { get; set; }

        [JsonProperty("tracking_token")]
        public string TrackingToken { get; set; }

        [JsonProperty("tappable_objects")]
        public TappableObject[] TappableObjects { get; set; }

        [JsonProperty("story_app_attribution")]
        public object StoryAppAttribution { get; set; }

        //[JsonProperty("edge_media_to_sponsor_user")]
        //public Edge EdgeMediaToSponsorUser { get; set; }

        [JsonProperty("has_audio", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasAudio { get; set; }

        [JsonProperty("overlay_image_resources")]
        public object OverlayImageResources { get; set; }

        [JsonProperty("video_duration", NullValueHandling = NullValueHandling.Ignore)]
        public double? VideoDuration { get; set; }

        [JsonProperty("video_resources", NullValueHandling = NullValueHandling.Ignore)]
        public VideoResource[] VideoResources { get; set; }

        public bool Equals(StoryItem other)
        {
            return Id == other?.Id && !string.IsNullOrEmpty(Id);
        }
    }

    public enum StoryItemType
    {
        [EnumMember(Value = "GraphStoryImage")]
        GraphStoryImage,
        [EnumMember(Value = "GraphStoryVideo")]
        GraphStoryVideo,
    }

    public class Dimensions
    {
        [JsonProperty("height")]
        public long Height { get; set; }

        [JsonProperty("width")]
        public long Width { get; set; }
    }
}
