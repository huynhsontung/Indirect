using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace InstagramAPI.Classes.Direct
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DirectItemType
    {
        Unknown = 0,
        [EnumMember(Value = "text")]
        Text,
        [EnumMember(Value = "media_share")]
        MediaShare,
        [EnumMember(Value = "like")]
        Like,
        [EnumMember(Value = "link")]
        Link,
        [EnumMember(Value = "media")]
        Media,
        [EnumMember(Value = "reel_share")]
        ReelShare,
        [EnumMember(Value = "placeholder")]
        Placeholder,
        [EnumMember(Value = "raven_media")]
        RavenMedia,
        [EnumMember(Value = "story_share")]
        StoryShare,
        [EnumMember(Value = "action_log")]
        ActionLog,
        [EnumMember(Value = "profile")]
        Profile,
        [EnumMember(Value = "location")]
        Location,
        /// <summary>
        ///     Instagram TV video share type
        /// </summary>
        [EnumMember(Value = "felix_share")]
        FelixShare,
        [EnumMember(Value = "voice_media")]
        VoiceMedia,
        [EnumMember(Value = "animated_media")]
        AnimatedMedia,
        [EnumMember(Value = "hashtag")]
        Hashtag,
        [EnumMember(Value = "live_viewer_invite")]
        LiveViewerInvite,
        [EnumMember(Value = "video_call_event")]
        VideoCallEvent,
    }
}
