using System;
using System.Collections.Generic;
using System.Net.Mime;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.Media;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    [JsonConverter(typeof(JsonPathConverter))]
    public class VisualMedia
    {
        [JsonProperty("media_id")] public long MediaId { get; set; }

        [JsonProperty("id")] public string Id { get; set; }

        [JsonProperty("media_type")] public InstaMediaType MediaType { get; set; }

        [JsonProperty("image_versions2.candidates")] public List<InstaImage> Images { get; set; }

        [JsonProperty("video_versions")] public List<InstaVideo> Videos { get; set; }

        [JsonProperty("organic_tracking_token")] public string TrackingToken { get; set; }

        [JsonProperty("original_width")] public int Width { get; set; }

        [JsonProperty("original_height")] public int Height { get; set; }

        [JsonProperty("url_expire_at_secs")] 
        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset? UrlExpireAtSecs { get; set; }

        public bool IsExpired => string.IsNullOrEmpty(Id);
    }
}