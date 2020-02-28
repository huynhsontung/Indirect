using System;
using System.Collections.Generic;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.Media;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    /// <summary>
    /// Share version of Instagram post through Direct. Not complete.
    /// </summary>
    [JsonConverter(typeof(JsonPathConverter))]
    public class DirectMediaShare
    {
        [JsonProperty("taken_at")]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset TakenAt { get; set; }

        [JsonProperty("pk")]
        public string Pk { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("device_timestamp")]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset DeviceTimestamp { get; set; }

        [JsonProperty("media_type")]
        public InstaMediaType MediaType { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("client_cache_key")]
        public string ClientCacheKey { get; set; }

        [JsonProperty("filter_type")]
        public int FilterType { get; set; }

        [JsonProperty("image_versions2.candidates")]
        public List<InstaImage> Images { get; set; }

        [JsonProperty("video_versions")] 
        public List<InstaVideo> Videos { get; set; }

        [JsonProperty("original_width")]
        public int OriginalWidth { get; set; }

        [JsonProperty("original_height")]
        public int OriginalHeight { get; set; }

        [JsonProperty("user")]
        public InstaUser User { get; set; }

        [JsonProperty("can_viewer_reshare")]
        public bool CanViewerReshare { get; set; }

        [JsonProperty("caption_is_edited")]
        public bool CaptionIsEdited { get; set; }

        [JsonProperty("liker_config")]
        public LikerConfig LikerConfig { get; set; }

        [JsonProperty("comment_likes_enabled")]
        public bool CommentLikesEnabled { get; set; }

        [JsonProperty("comment_threading_enabled")]
        public bool CommentThreadingEnabled { get; set; }

        [JsonProperty("has_more_comments")]
        public bool HasMoreComments { get; set; }

        [JsonProperty("max_num_visible_preview_comments")]
        public int MaxNumVisiblePreviewComments { get; set; }

        [JsonProperty("can_view_more_preview_comments")]
        public bool CanViewMorePreviewComments { get; set; }

        [JsonProperty("comment_count")]
        public int CommentCount { get; set; }

        [JsonProperty("like_count")]
        public int LikeCount { get; set; }

        [JsonProperty("bucketed_like_count")]
        public object BucketedLikeCount { get; set; }

        [JsonProperty("has_liked")]
        public bool HasLiked { get; set; }

        [JsonProperty("top_likers")]
        public string[] TopLikers { get; set; }

        [JsonProperty("facepile_top_likers")]
        public List<InstaUser> FacepileTopLikers { get; set; }

        [JsonProperty("photo_of_you")]
        public bool PhotoOfYou { get; set; }

        [JsonProperty("caption")]
        public MediaCaption Caption { get; set; }

        [JsonProperty("can_viewer_save")]
        public bool CanViewerSave { get; set; }

        [JsonProperty("organic_tracking_token")]
        public string OrganicTrackingToken { get; set; }

        [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
        public LocationContainer Location { get; set; }
    }

    public partial class LikerConfig
    {
        [JsonProperty("is_daisy")]
        public bool IsDaisy { get; set; }

        [JsonProperty("hide_view_count")]
        public bool HideViewCount { get; set; }

        [JsonProperty("show_count_in_likers_list")]
        public bool ShowCountInLikersList { get; set; }

        [JsonProperty("show_view_count_in_likers_list")]
        public bool ShowViewCountInLikersList { get; set; }

        [JsonProperty("show_daisy_liker_list_header")]
        public bool ShowDaisyLikerListHeader { get; set; }

        [JsonProperty("show_learn_more")]
        public bool ShowLearnMore { get; set; }

        [JsonProperty("ads_display_mode")]
        public int AdsDisplayMode { get; set; }

        [JsonProperty("display_mode")]
        public int DisplayMode { get; set; }

        [JsonProperty("disable_liker_list_navigation")]
        public bool DisableLikerListNavigation { get; set; }

        [JsonProperty("show_author_view_likes_button")]
        public bool ShowAuthorViewLikesButton { get; set; }
    }
}