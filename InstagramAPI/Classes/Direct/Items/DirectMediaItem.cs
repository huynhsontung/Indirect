using InstagramAPI.Classes.Direct.ItemContent;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.Items
{
    public class DirectMediaItem : DirectItem
    {
        [JsonProperty("media")] public DirectMedia Media { get; set; }
    }
}