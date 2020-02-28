using System;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Media
{
    public class MediaCaption
    {
        [JsonProperty("pk")]
        public long Pk { get; set; }

        [JsonProperty("user_id")]
        public long UserId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("type")]
        public long Type { get; set; }

        [JsonProperty("created_at")]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("created_at_utc")]
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset CreatedAtUtc { get; set; }

        [JsonProperty("content_type")]
        public string ContentType { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("bit_flags")]
        public long BitFlags { get; set; }

        [JsonProperty("user")]
        public InstaUser User { get; set; }

        [JsonProperty("did_report_as_spam")]
        public bool DidReportAsSpam { get; set; }

        [JsonProperty("share_enabled")]
        public bool ShareEnabled { get; set; }

        [JsonProperty("media_id")]
        public long MediaId { get; set; }
    }
}