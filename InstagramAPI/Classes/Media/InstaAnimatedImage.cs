using Newtonsoft.Json;

namespace InstagramAPI.Classes.Media
{
    public class InstaAnimatedImage : InstaImage
    {
        [JsonProperty("size")] public string Size { get; set; }

        [JsonProperty("mp4")] public string Mp4 { get; set; }

        [JsonProperty("mp4_size")] public string Mp4Size { get; set; }

        [JsonProperty("webp")] public string Webp { get; set; }

        [JsonProperty("webp_size")] public string WebpSize { get; set; }
    }
}