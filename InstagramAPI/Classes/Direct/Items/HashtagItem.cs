using InstagramAPI.Classes.Direct.ItemContent;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.Items
{
    public class HashtagItem : DirectItem
    {
        [JsonProperty("hashtag")] public Hashtag HashtagMedia { get; set; }
    }
}