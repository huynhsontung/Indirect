using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    public class Hashtag
    {
        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("media_count")] public long MediaCount { get; set; }

        [JsonProperty("media")] public DirectMediaShare Media { get; set; }
    }
}