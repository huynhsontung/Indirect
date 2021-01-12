using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.Media;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    [JsonConverter(typeof(JsonPathConverter))]
    public class Clip
    {
        [JsonProperty("taken_at")]
        [JsonConverter(typeof(TimestampConverter))]
        public long TakenAt { get; set; }

        [JsonProperty("pk")]
        public long Pk { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("device_timestamp")]
        [JsonConverter(typeof(MicroTimestampConverter))]
        public long DeviceTimestamp { get; set; }

        [JsonProperty("media_type")]
        public long MediaType { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("client_cache_key")]
        public string ClientCacheKey { get; set; }

        [JsonProperty("filter_type")]
        public long FilterType { get; set; }

        [JsonProperty("user")]
        public BaseUser User { get; set; }

        [JsonProperty("can_viewer_reshare")]
        public bool CanViewerReshare { get; set; }

        [JsonProperty("caption_is_edited")]
        public bool CaptionIsEdited { get; set; }

        [JsonProperty("comment_likes_enabled")]
        public bool CommentLikesEnabled { get; set; }

        [JsonProperty("comment_threading_enabled")]
        public bool CommentThreadingEnabled { get; set; }

        [JsonProperty("has_more_comments")]
        public bool HasMoreComments { get; set; }

        [JsonProperty("next_max_id")]
        public long NextMaxId { get; set; }

        [JsonProperty("max_num_visible_preview_comments")]
        public long MaxNumVisiblePreviewComments { get; set; }

        //[JsonProperty("preview_comments")]
        //public PreviewComment[] PreviewComments { get; set; }

        [JsonProperty("can_view_more_preview_comments")]
        public bool CanViewMorePreviewComments { get; set; }

        [JsonProperty("comment_count")]
        public long CommentCount { get; set; }

        [JsonProperty("image_versions2.candidates")]
        public InstaImage[] Images { get; set; }

        [JsonProperty("original_width")]
        public long OriginalWidth { get; set; }

        [JsonProperty("original_height")]
        public long OriginalHeight { get; set; }

        [JsonProperty("like_count")]
        public long LikeCount { get; set; }

        [JsonProperty("has_liked")]
        public bool HasLiked { get; set; }

        [JsonProperty("like_and_view_counts_disabled")]
        public bool LikeAndViewCountsDisabled { get; set; }

        [JsonProperty("photo_of_you")]
        public bool PhotoOfYou { get; set; }

        [JsonProperty("can_see_insights_as_brand")]
        public bool CanSeeInsightsAsBrand { get; set; }

        [JsonProperty("is_dash_eligible")]
        public long IsDashEligible { get; set; }

        [JsonProperty("video_dash_manifest")]
        public string VideoDashManifest { get; set; }

        [JsonProperty("video_codec")]
        public string VideoCodec { get; set; }

        [JsonProperty("number_of_qualities")]
        public long NumberOfQualities { get; set; }

        [JsonProperty("video_versions")]
        public InstaVideo[] VideoVersions { get; set; }

        [JsonProperty("has_audio")]
        public bool HasAudio { get; set; }

        [JsonProperty("video_duration")]
        public double VideoDuration { get; set; }

        [JsonProperty("view_count")]
        public double ViewCount { get; set; }

        [JsonProperty("play_count")]
        public long PlayCount { get; set; }

        [JsonProperty("caption")]
        public ClipCaption Caption { get; set; }

        [JsonProperty("can_viewer_save")]
        public bool CanViewerSave { get; set; }

        [JsonProperty("organic_tracking_token")]
        public string OrganicTrackingToken { get; set; }

        //[JsonProperty("sharing_friction_info")]
        //public SharingFrictionInfo SharingFrictionInfo { get; set; }

        [JsonProperty("product_type")]
        public string ProductType { get; set; }

        [JsonProperty("is_in_profile_grid")]
        public bool IsInProfileGrid { get; set; }

        [JsonProperty("profile_grid_control_enabled")]
        public bool ProfileGridControlEnabled { get; set; }

        [JsonProperty("is_shop_the_look_eligible")]
        public bool IsShopTheLookEligible { get; set; }

        [JsonProperty("deleted_reason")]
        public long DeletedReason { get; set; }

        [JsonProperty("integrity_review_decision")]
        public string IntegrityReviewDecision { get; set; }

        //[JsonProperty("clips_metadata")]
        //public ClipsMetadata ClipsMetadata { get; set; }

        [JsonProperty("logging_info_token")]
        public object LoggingInfoToken { get; set; }
    }
}
