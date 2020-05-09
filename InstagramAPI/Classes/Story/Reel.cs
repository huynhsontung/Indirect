using Newtonsoft.Json;

namespace InstagramAPI.Classes.Story
{
    public class Reel  // GraphReel type
    {
        [JsonProperty("has_besties_media")]
        public bool HasBestiesMedia { get; set; }

        [JsonProperty("has_pride_media")]
        public bool HasPrideMedia { get; set; }

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

        [JsonProperty("seen")]
        public object Seen { get; set; }

        [JsonProperty("seen_ranked_position")]
        public long SeenRankedPosition { get; set; }

        [JsonProperty("user")]
        public Owner User { get; set; }

        [JsonProperty("owner")]
        public Owner Owner { get; set; }
    }
}
