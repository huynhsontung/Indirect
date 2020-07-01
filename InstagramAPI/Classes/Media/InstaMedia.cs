using System;
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

        /// <summary>
        ///     Get last image url in Images. Mostly used for easy XAML binding.
        /// </summary>
        public Uri GetLastImageUrl()
        {
            if (Images == null || Images.Length == 0) return null;
            return Images[Images.Length - 1]?.Url;
        }
    }
}
