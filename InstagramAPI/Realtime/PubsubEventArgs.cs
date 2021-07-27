using System;
using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;

namespace InstagramAPI.Realtime
{
    public class PubsubEventArgs : EventArgs
    {
        [JsonProperty("publish_metadata")]
        public PublishMetadata PublishMetadata { get; set; }

        [JsonProperty("lazy")]
        public bool Lazy { get; set; }

        [JsonProperty("data", ItemConverterType = typeof(ActivityIndicatorDataConverter))]
        public ActivityIndicatorData[] Data { get; set; }

        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("num_endpoints")]
        public long NumEndpoints { get; set; }
    }

    public class PublishMetadata
    {
        [JsonProperty("publish_time_ms")]
        public DateTimeOffset PublishTimeMs { get; set; }

        [JsonProperty("topic_publish_id")]
        public long TopicPublishId { get; set; }
    }

    public class ActivityIndicatorData : SyncBaseData
    {
        [JsonProperty("doublePublish")]
        public bool DoublePublish { get; set; }

        [JsonIgnore]
        public ActivityIndicator Indicator { get; set; }
    }

    public class ActivityIndicator
    {
        [JsonProperty("timestamp")]
        [JsonConverter(typeof(MicroTimestampConverter))]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("sender_id")]
        public long SenderId { get; set; }

        [JsonProperty("ttl")]
        public int TimeToLive { get; set; }    // in ms

        [JsonProperty("activity_status")]
        public int ActivityStatus { get; set; }
    }
}
