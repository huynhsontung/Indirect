using System;
using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Responses
{
    public class ItemAckResponse : DefaultResponse
    {
        [JsonProperty("action")] public string Action { get; set; }
        [JsonProperty("payload")] public ItemAckPayloadResponse Payload { get; set; }
    }

    public class ItemAckPayloadResponse
    {
        [JsonProperty("client_context")] public string ClientContext { get; set; }

        [JsonProperty("item_id")] public string ItemId { get; set; }

        [JsonProperty("thread_id")] public string ThreadId { get; set; }

        [JsonProperty("timestamp")]
        [JsonConverter(typeof(MicroTimestampConverter))]
        public DateTimeOffset Timestamp { get; set; }

    }
}