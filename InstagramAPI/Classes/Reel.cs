using System;
using System.ComponentModel;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.Media;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes
{
    public class Reel : IEquatable<Reel>
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("latest_reel_media")]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset LatestReelMedia { get; set; }

        [JsonProperty("expiring_at")]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset ExpiringAt { get; set; }

        [JsonProperty("seen", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(TimestampConverter))]
        [DefaultValue(0)]
        public DateTimeOffset? Seen { get; set; }

        [JsonProperty("can_reply")]
        public bool CanReply { get; set; }

        [JsonProperty("can_gif_quick_reply")]
        public bool CanGifQuickReply { get; set; }

        [JsonProperty("can_reshare")]
        public bool CanReshare { get; set; }

        [JsonProperty("reel_type")]
        public string ReelType { get; set; }

        [JsonProperty("is_sensitive_vertical_ad")]
        public bool IsSensitiveVerticalAd { get; set; }

        [JsonProperty("user")]
        public UserWithFriendship User { get; set; }

        [JsonProperty("ranked_position")]
        public long RankedPosition { get; set; }

        [JsonProperty("seen_ranked_position")]
        public long SeenRankedPosition { get; set; }

        [JsonProperty("muted")]
        public bool Muted { get; set; }

        [JsonProperty("prefetch_count")]
        public long PrefetchCount { get; set; }

        [JsonProperty("has_besties_media")]
        public bool HasBestiesMedia { get; set; }

        [JsonProperty("latest_besties_reel_media")]
        public double LatestBestiesReelMedia { get; set; }

        [JsonProperty("media_count")]
        public long MediaCount { get; set; }

        [JsonProperty("media_ids")]
        public long[] MediaIds { get; set; }

        [JsonProperty("has_pride_media")]
        public bool HasPrideMedia { get; set; }

        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public ReelMedia[] Items { get; set; }

        [JsonProperty("is_cacheable", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsCacheable { get; set; }

        public bool Equals(Reel other)
        {
            return Id == other?.Id;
        }
    }
}
