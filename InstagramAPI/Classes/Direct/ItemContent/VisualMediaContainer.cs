using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    public class VisualMediaContainer
    {
        [JsonProperty("url_expire_at_secs", NullValueHandling = NullValueHandling.Ignore)] 
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset? UrlExpireAtSecs { get; set; }

        [JsonProperty("media")] public VisualMedia Media { get; set; }

        [JsonProperty("seen_count")] public int SeenCount { get; set; }

        [JsonProperty("replay_expiring_at_us", NullValueHandling = NullValueHandling.Ignore)] 
        [JsonConverter(typeof(MicroTimestampConverter))]
        public DateTimeOffset? ReplayExpiringAtUs { get; set; }

        [JsonProperty("view_mode")] public VisualMediaViewMode ViewMode { get; set; }

        // [JsonProperty("expiring_media_action_summary")] public Dictionary<string,string> RavenExpiringMediaActionSummary { get; set; }

        [JsonProperty("seen_user_ids")] public long[] SeenUserIds { get; set; }

        [JsonIgnore] public bool IsExpired => Media == null || Media.IsExpired;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum VisualMediaViewMode
    {
        /// <summary>
        ///     Only see one time without replay option, and it will be remove
        /// </summary>
        [EnumMember(Value = "once")]
        Once,
        /// <summary>
        ///     Only see once with replay option, and it will be remove
        /// </summary>
        [EnumMember(Value = "replayable")]
        Replayable,
        /// <summary>
        ///     Permanent direct, it's like sending photo/video to direct
        /// </summary>
        [EnumMember(Value = "permanent")]
        Permanent
    }
}