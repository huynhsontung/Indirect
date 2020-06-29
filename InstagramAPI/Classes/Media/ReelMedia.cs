using System;
using System.Collections.Generic;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Media
{
    [JsonConverter(typeof(JsonPathConverter))]
    public class ReelMedia
    {
        [JsonProperty("user")]
        public BaseUser User { get; set; }  // Only contains Pk and IsPrivate

        [JsonProperty("expiring_at")]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset ExpiringAt { get; set; }

        [JsonProperty("taken_at", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset? TakenAt { get; set; }

        [JsonProperty("pk", NullValueHandling = NullValueHandling.Ignore)]
        public long? Pk { get; set; }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("device_timestamp", NullValueHandling = NullValueHandling.Ignore)]
        public long? DeviceTimestamp { get; set; }

        [JsonProperty("media_type", NullValueHandling = NullValueHandling.Ignore)]
        public ReelMediaType MediaType { get; set; }

        [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
        public string Code { get; set; }

        [JsonProperty("client_cache_key", NullValueHandling = NullValueHandling.Ignore)]
        public string ClientCacheKey { get; set; }

        [JsonProperty("filter_type", NullValueHandling = NullValueHandling.Ignore)]
        public long? FilterType { get; set; }

        [JsonProperty("image_versions2.candidates", NullValueHandling = NullValueHandling.Ignore)]
        public InstaImage[] Images { get; set; }

        [JsonProperty("original_width", NullValueHandling = NullValueHandling.Ignore)]
        public int? OriginalWidth { get; set; }

        [JsonProperty("original_height", NullValueHandling = NullValueHandling.Ignore)]
        public int? OriginalHeight { get; set; }

        [JsonProperty("caption_is_edited", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CaptionIsEdited { get; set; }

        [JsonProperty("comment_likes_enabled", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CommentLikesEnabled { get; set; }

        [JsonProperty("comment_threading_enabled", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CommentThreadingEnabled { get; set; }

        [JsonProperty("has_more_comments", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasMoreComments { get; set; }

        [JsonProperty("max_num_visible_preview_comments", NullValueHandling = NullValueHandling.Ignore)]
        public long? MaxNumVisiblePreviewComments { get; set; }

        [JsonProperty("preview_comments", NullValueHandling = NullValueHandling.Ignore)]
        public object[] PreviewComments { get; set; }

        [JsonProperty("can_view_more_preview_comments", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanViewMorePreviewComments { get; set; }

        [JsonProperty("comment_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? CommentCount { get; set; }

        [JsonProperty("caption_position", NullValueHandling = NullValueHandling.Ignore)]
        public double? CaptionPosition { get; set; }

        [JsonProperty("is_reel_media", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsReelMedia { get; set; }

        [JsonProperty("timezone_offset", NullValueHandling = NullValueHandling.Ignore)]
        public long? TimezoneOffset { get; set; }

        [JsonProperty("like_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? LikeCount { get; set; }

        [JsonProperty("has_liked", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasLiked { get; set; }

        [JsonProperty("likers", NullValueHandling = NullValueHandling.Ignore)]
        public object[] Likers { get; set; }

        [JsonProperty("photo_of_you", NullValueHandling = NullValueHandling.Ignore)]
        public bool? PhotoOfYou { get; set; }

        [JsonProperty("caption")]
        public MediaCaption Caption { get; set; }

        // [JsonProperty("fb_user_tags", NullValueHandling = NullValueHandling.Ignore)]
        // public FbUserTags FbUserTags { get; set; }

        [JsonProperty("can_viewer_save", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanViewerSave { get; set; }

        [JsonProperty("organic_tracking_token", NullValueHandling = NullValueHandling.Ignore)]
        public string OrganicTrackingToken { get; set; }

        [JsonProperty("story_is_saved_to_archive", NullValueHandling = NullValueHandling.Ignore)]
        public bool? StoryIsSavedToArchive { get; set; }

        [JsonProperty("imported_taken_at", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset? ImportedTakenAt { get; set; }

        [JsonProperty("video_versions", NullValueHandling = NullValueHandling.Ignore)]
        public InstaVideo[] VideoVersions { get; set; }

        [JsonProperty("has_audio", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasAudio { get; set; }

        [JsonProperty("video_duration", NullValueHandling = NullValueHandling.Ignore)]
        public double? VideoDuration { get; set; }
    }

    public enum ReelMediaType
    {
        Unknown = 0,
        Image = 1,
        Video = 2,
    }
}