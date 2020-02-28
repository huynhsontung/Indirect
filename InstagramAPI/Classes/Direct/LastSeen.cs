using System;
using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct
{
    public class LastSeen
    {
        [JsonProperty("timestamp")] 
        [JsonConverter(typeof(MicroTimestampConverter))]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("item_id")] public string ItemId { get; set; }
    }
}
