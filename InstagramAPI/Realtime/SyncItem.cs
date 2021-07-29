using Newtonsoft.Json;

namespace InstagramAPI.Realtime
{
    public class SyncItem
    {
        [JsonProperty("op")]
        public string Op { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
