using System;
using System.Collections.Generic;
using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Responses
{
    public class UserPresenceResponse : BaseStatusResponse
    {
        [JsonProperty("user_presence")]
        public Dictionary<long, UserPresenceValue> UserPresence { get; set; }
    }

    public class UserPresenceValue : EventArgs
    {
        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("last_activity_at_ms", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(MilliTimestampConverter))]
        public DateTimeOffset? LastActivityAtMs { get; set; }
    }
}
