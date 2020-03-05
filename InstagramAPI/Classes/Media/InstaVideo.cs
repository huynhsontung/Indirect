using Newtonsoft.Json;

namespace InstagramAPI.Classes.Media
{
    public class InstaVideo : InstaImage
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("type")] public int Type { get; set; }
    }
}