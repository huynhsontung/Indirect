using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Media
{
    [JsonConverter(typeof(JsonPathConverter))]
    public class InstaMedia
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("image_versions2.candidates", NullValueHandling = NullValueHandling.Ignore)]
        public InstaImage[] Images { get; set; }

        [JsonProperty("original_width", NullValueHandling = NullValueHandling.Ignore)]
        public int? OriginalWidth { get; set; }

        [JsonProperty("original_height", NullValueHandling = NullValueHandling.Ignore)]
        public int? OriginalHeight { get; set; }

        [JsonProperty("media_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(TolerantEnumConverter))]
        public InstaMediaType MediaType { get; set; }

        [JsonProperty("video_versions", NullValueHandling = NullValueHandling.Ignore)] 
        public InstaVideo[] Videos { get; set; }

        [JsonProperty("has_audio", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasAudio { get; set; }

        [JsonProperty("video_duration", NullValueHandling = NullValueHandling.Ignore)]
        public double? VideoDuration { get; set; }
    }
}
