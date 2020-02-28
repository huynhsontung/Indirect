using InstagramAPI.Classes.Direct.ItemContent;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.Items
{
    public class ReelShareItem : DirectItem
    {
        [JsonProperty("reel_share")] public ReelShare ReelShareMedia { get; set; }
    }
}