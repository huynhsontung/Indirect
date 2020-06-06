using System;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.Media;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    public class VoiceShare
    {
        [JsonProperty("media")]
        public VoiceMedia Media { get; set; }

        [JsonProperty("seen_user_ids")]
        public long[] SeenUserIds { get; set; }

        [JsonProperty("view_mode")]
        public string ViewMode { get; set; }

        [JsonProperty("seen_count")]
        public int SeenCount { get; set; }

        [JsonProperty("replay_expiring_at_us")]
        [JsonConverter(typeof(MicroTimestampConverter))]
        public DateTimeOffset? ReplayExpiringAtUs { get; set; }
    }

    public class VoiceMedia
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("media_type")]
        public int MediaType { get; set; }

        [JsonProperty("product_type")]
        public string ProductType { get; set; }

        [JsonProperty("audio")]
        public InstaAudio Audio { get; set; }

        [JsonProperty("organic_tracking_token")]
        public string OrganicTrackingToken { get; set; }

        [JsonProperty("user")]
        public BaseUser User { get; set; }
    }
}