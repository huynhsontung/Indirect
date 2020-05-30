using System;
using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Story
{
    public class Reel : IEquatable<Reel>    // GraphReel type
    {
        [JsonProperty("has_besties_media", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasBestiesMedia { get; set; }

        [JsonProperty("has_pride_media", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasPrideMedia { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("can_reply")]
        public bool CanReply { get; set; }

        [JsonProperty("can_reshare")]
        public bool CanReshare { get; set; }

        [JsonProperty("expiring_at")]
        public long ExpiringAt { get; set; }

        [JsonProperty("latest_reel_media")]
        public long LatestReelMedia { get; set; }

        [JsonProperty("muted")]
        public bool Muted { get; set; }

        [JsonProperty("supports_reel_reactions")]
        public object SupportsReelReactions { get; set; }

        [JsonProperty("items")]
        public StoryItem[] Items { get; set; }

        [JsonProperty("prefetch_count")]
        public long PrefetchCount { get; set; }

        [JsonProperty("ranked_position")]
        public long RankedPosition { get; set; }

        [JsonProperty("seen", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset? Seen { get; set; }

        [JsonProperty("seen_ranked_position")]
        public long SeenRankedPosition { get; set; }

        [JsonProperty("user")]
        public Owner User { get; set; }

        [JsonProperty("owner")]
        public Owner Owner { get; set; }

        public bool Equals(Reel other)
        {
            return Id == other?.Id && !string.IsNullOrEmpty(Id);
        }
    }
}
