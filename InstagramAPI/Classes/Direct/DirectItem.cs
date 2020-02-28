using System;
using System.Collections.Generic;
using InstagramAPI.Classes.Direct.ItemContent;
using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct
{
    public class DirectItem : IEquatable<DirectItem>
    {
        public string Description { get; set; }

        public string RawJson { get; set; }

        public bool FromMe { get; set; } = false;

        [JsonProperty("user_id")] public long UserId { get; set; }

        [JsonProperty("timestamp")]
        [JsonConverter(typeof(MicroTimestampConverter))]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("item_id")] public string ItemId { get; set; }

        [JsonProperty("item_type")] public DirectItemType ItemType { get; set; }

        [JsonProperty("reactions")] public ReactionsContainer Reactions { get; set; }

        [JsonProperty("client_context")] public string ClientContext { get; set; }

        // [JsonProperty("story_share")] public InstaStoryShareResponse StoryShare { get; set; }

        // raven media properties
        // [JsonProperty("view_mode")] public string RavenViewMode { get; set; }

        // [JsonProperty("seen_user_ids")] public List<long> RavenSeenUserIds { get; set; }

        // [JsonProperty("reply_chain_count")] public int? RavenReplayChainCount { get; set; }

        // [JsonProperty("seen_count")] public int RavenSeenCount { get; set; }

        
        // end

        // [JsonProperty("profile")] public InstaUserShortResponse ProfileMedia { get; set; }

        // [JsonProperty("preview_medias")] public List<InstaMediaItemResponse> ProfileMediasPreview { get; set; }

        // [JsonProperty("placeholder")] public InstaPlaceholderResponse Placeholder { get; set; }

        // [JsonProperty("location")] public InstaLocationResponse LocationMedia { get; set; }

        // [JsonProperty("felix_share")] public InstaFelixShareResponse FelixShareMedia { get; set; }

        // [JsonProperty("live_viewer_invite")] public InstaDirectBroadcastResponse LiveViewerInvite { get; set; }

        public bool Equals(DirectItem other)
        {
            return !string.IsNullOrEmpty(ItemId) && !string.IsNullOrEmpty(other?.ItemId) && ItemId == other.ItemId;
        }
    }
}
