using System.Collections.Generic;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.Media;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    [JsonConverter(typeof(JsonPathConverter))]
    public class DirectMedia
    {
        [JsonProperty("image_versions2.candidates")] public List<InstaImage> Images { get; set; }

        [JsonProperty("original_width")] public int OriginalWidth { get; set; }

        [JsonProperty("original_height")] public int OriginalHeight { get; set; }

        [JsonProperty("media_type")] public InstaMediaType MediaType { get; set; }

        [JsonProperty("video_versions")] public List<InstaVideo> Videos { get; set; }
    }
}
