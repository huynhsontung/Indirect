using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Media
{
    public class GiphyMedia
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("bitly_gif_url")]
        public Uri BitlyGifUrl { get; set; }

        [JsonProperty("bitly_url")]
        public Uri BitlyUrl { get; set; }

        [JsonProperty("embed_url")]
        public Uri EmbedUrl { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("source", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue("")]
        public Uri Source { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("rating")]
        public string Rating { get; set; }

        [JsonProperty("content_url")]
        public string ContentUrl { get; set; }

        [JsonProperty("tags")]
        public string[] Tags { get; set; }

        [JsonProperty("featured_tags")]
        public string[] FeaturedTags { get; set; }

        [JsonProperty("source_tld")]
        public string SourceTld { get; set; }

        [JsonProperty("source_post_url", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue("")]
        public Uri SourcePostUrl { get; set; }

        [JsonProperty("is_sticker")]
        public bool IsSticker { get; set; }

        //[JsonProperty("import_datetime")]
        //public DateTimeOffset ImportDatetime { get; set; }

        //[JsonProperty("trending_datetime")]
        //public string TrendingDatetime { get; set; }

        //[JsonProperty("create_datetime")]
        //public DateTimeOffset CreateDatetime { get; set; }

        //[JsonProperty("update_datetime")]
        //public DateTimeOffset UpdateDatetime { get; set; }

        [JsonProperty("images")]
        public Dictionary<string, InstaAnimatedImage> Images { get; set; }

        [JsonProperty("analytics_response_payload")]
        public string AnalyticsResponsePayload { get; set; }

        [JsonProperty("user", NullValueHandling = NullValueHandling.Ignore)]
        public AuthorInfo User { get; set; }
    }

    public class AuthorInfo
    {
        [JsonProperty("avatar_url")]
        public Uri AvatarUrl { get; set; }

        [JsonProperty("banner_image")]
        public string BannerImage { get; set; }

        [JsonProperty("banner_url")]
        public string BannerUrl { get; set; }

        [JsonProperty("profile_url")]
        public Uri ProfileUrl { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("is_verified")]
        public bool IsVerified { get; set; }

        [JsonProperty("instagram_url")]
        public string InstagramUrl { get; set; }
    }
}