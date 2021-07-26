using InstagramAPI.Classes.Responses;
using Newtonsoft.Json;

namespace InstagramAPI.Realtime
{
    public class UserPresenceEventArgs : UserPresenceValue
    {
        [JsonProperty("user_id")]
        public long UserId { get; set; }
    }
}
