using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.Responses;
using Newtonsoft.Json;

namespace InstagramAPI.Classes
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

        [JsonProperty("last_activity_at_ms")]
        [JsonConverter(typeof(MilliTimestampConverter))]
        public DateTimeOffset? LastActivityAtMs { get; set; }
    }
}
