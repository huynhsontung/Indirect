using System;
using InstagramAPI.Classes.Direct.ItemContent;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.Media;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;
using Hashtag = InstagramAPI.Classes.Direct.ItemContent.Hashtag;

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

        [JsonProperty("item_type")]
        [JsonConverter(typeof(TolerantEnumConverter))]
        public DirectItemType ItemType { get; set; }

        [JsonProperty("reactions")] public ReactionsContainer Reactions { get; set; }

        [JsonProperty("client_context")] public string ClientContext { get; set; }
        
        [JsonProperty("show_forward_attribution")] public bool ShowForwardAttribution { get; set; }
        
        [JsonProperty("is_shh_mode")] public bool IsShhMode { get; set; }

        [JsonProperty("replied_to_message", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DirectItemConverter))] 
        public DirectItem RepliedToMessage { get; set; }

        // Below are content belong to specific item type. The reason not to split up these into
        // separate classes is that this class need to be wrapped in another class in the front-end
        // for UI persistent data. Separate these will introduce tricky inheritance problems.

        #region ActionLog

        [JsonProperty("action_log")] public DirectActionLog ActionLog { get; set; }
        
        [JsonProperty("hide_in_thread")]
        [JsonConverter(typeof(BoolConverter))]
        public bool HideInThread { get; set; }

        #endregion

        #region AnimatedMedia

        [JsonProperty("animated_media")] public AnimatedMedia AnimatedMedia { get; set; }

        #endregion

        #region DirectMedia

        [JsonProperty("media")] public InstaMedia Media { get; set; }

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

        // ReelShare is for replying/reacting to stories
        [JsonProperty("reel_share")] public ReelShare ReelShareMedia { get; set; }

        #endregion

        #region StoryShare

        // StoryShare is for sharing a third person's story to a second person (recipient)
        // Like sharing friends' stories to another friend
        [JsonProperty("story_share")] public StoryShare StoryShareMedia { get; set; }

        #endregion

        #region Text

        [JsonProperty("text")] public string Text { get; set; }

        #endregion

        #region VoiceMedia

        [JsonProperty("voice_media")] public VoiceShare VoiceMedia { get; set; }

        #endregion

        #region VideoCallEvent

        [JsonProperty("video_call_event")] public VideoCallEvent VideoCallEvent { get; set; }

        #endregion

        #region Profile

        [JsonProperty("profile", NullValueHandling = NullValueHandling.Ignore)]
        public BaseUser Profile { get; set; }

        [JsonProperty("preview_medias", NullValueHandling = NullValueHandling.Ignore)]
        public InstaMedia[] PreviewMedias { get; set; }

        #endregion

        #region Placeholder

        [JsonProperty("placeholder")] public Placeholder Placeholder { get; set; }

        #endregion

        public bool Equals(DirectItem other)
        {
            return !string.IsNullOrEmpty(ItemId) && !string.IsNullOrEmpty(other?.ItemId) && ItemId == other.ItemId;
        }
    }
}
