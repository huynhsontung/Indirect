using InstagramAPI.Classes.Media;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    public partial class ReelShare
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("reel_owner_id")]
        public long ReelOwnerId { get; set; }

        [JsonProperty("is_reel_persisted")]
        public bool IsReelPersisted { get; set; }

        [JsonProperty("reel_type")]
        public string ReelType { get; set; }

        [JsonProperty("media")]
        public ReelMedia Media { get; set; }

        [JsonProperty("reel_name", NullValueHandling = NullValueHandling.Ignore)]
        public string ReelName { get; set; }

        [JsonProperty("reel_id", NullValueHandling = NullValueHandling.Ignore)]
        public string ReelId { get; set; }

        // [JsonProperty("reaction_info", NullValueHandling = NullValueHandling.Ignore)]
        // public ReactionInfo ReactionInfo { get; set; }
    }
}