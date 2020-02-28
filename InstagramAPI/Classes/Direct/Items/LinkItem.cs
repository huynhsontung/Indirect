using InstagramAPI.Classes.Direct.ItemContent;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.Items
{
    public class LinkItem : DirectItem
    {
        [JsonProperty("link")] public LinkShare Link { get; set; }
    }
}