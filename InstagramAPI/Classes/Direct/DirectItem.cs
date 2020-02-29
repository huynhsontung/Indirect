using System;
using System.Collections.Generic;
using InstagramAPI.Classes.Direct.ItemContent;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.Responses;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct
{
    public class DirectItem : BaseStatusResponse, IEquatable<DirectItem>
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

        // Below are content belong to specific item type. The reason not to split up these into
        // separate classes is that this class need to be wrapped in another class in the front-end
        // for UI persistent data. Separate these will introduce tricky inheritance problems.

        #region ActionLog

        [JsonProperty("action_log")] public DirectActionLog ActionLog { get; set; }

        #endregion

        #region AnimatedMedia

        [JsonProperty("animated_media")] public AnimatedMedia AnimatedMedia { get; set; }

        #endregion

        #region DirectMedia

        [JsonProperty("media")] public DirectMedia Media { get; set; }

        #endregion

        #region Hashtag

        [JsonProperty("hashtag")] public Hashtag HashtagMedia { get; set; }

        #endregion

        #region Like

        [JsonProperty("like")] public string Like { get; set; }

        #endregion

        #region Link

        [JsonProperty("link")] public LinkShare Link { get; set; }

        #endregion

        #region MediaShare

        [JsonProperty("media_share")] public DirectMediaShare MediaShare { get; set; }

        #endregion

        #region RavenMedia

        // Only appear in message sync version
        [JsonProperty("raven_media")] public VisualMedia RavenMedia { get; set; }

        [JsonProperty("visual_media")] public VisualMediaContainer VisualMedia { get; set; }

        #endregion

        #region ReelShare

        [JsonProperty("reel_share")] public ReelShare ReelShareMedia { get; set; }

        #endregion

        #region Text

        [JsonProperty("text")] public string Text { get; set; }

        #endregion

        #region VoiceMedia

        [JsonProperty("voice_media")] public VoiceShare VoiceMedia { get; set; }

        #endregion

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
