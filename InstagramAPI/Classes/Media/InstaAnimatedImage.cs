using System;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Media
{
    public class InstaAnimatedImage : InstaImage
    {
        [JsonProperty("size")] public int Size { get; set; }

        [JsonProperty("mp4")] public Uri Mp4 { get; set; }

        [JsonProperty("mp4_size")] public int? Mp4Size { get; set; }

        [JsonProperty("webp")] public Uri Webp { get; set; }

        [JsonProperty("webp_size")] public int? WebpSize { get; set; }
    }
}