using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.Items
{
    public class LikeItem : DirectItem
    {
        [JsonProperty("like")] public string Like { get; set; }
    }
}