using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    public class ClipCaption
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
        public long CreatedAt { get; set; }

        [JsonProperty("created_at_utc")]
        public long CreatedAtUtc { get; set; }

        [JsonProperty("content_type")]
        public string ContentType { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("bit_flags")]
        public long BitFlags { get; set; }

        [JsonProperty("did_report_as_spam")]
        public bool DidReportAsSpam { get; set; }

        [JsonProperty("share_enabled")]
        public bool ShareEnabled { get; set; }

        [JsonProperty("user")]
        public BaseUser User { get; set; }

        [JsonProperty("is_covered")]
        public bool IsCovered { get; set; }

        [JsonProperty("media_id")]
        public long MediaId { get; set; }

        [JsonProperty("private_reply_status")]
        public long PrivateReplyStatus { get; set; }
    }
}
