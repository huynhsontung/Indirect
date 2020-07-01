using System;
using System.Collections.Generic;
using System.Net.Mime;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.Media;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    [JsonConverter(typeof(JsonPathConverter))]
    public class VisualMedia : InstaMedia
    {
        [JsonProperty("media_id")] public long MediaId { get; set; }

        [JsonProperty("organic_tracking_token")] public string TrackingToken { get; set; }

        [JsonProperty("url_expire_at_secs")] 
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset? UrlExpireAtSecs { get; set; }

        public bool IsExpired => string.IsNullOrEmpty(Id);
    }
}