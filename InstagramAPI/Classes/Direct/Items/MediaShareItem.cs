using InstagramAPI.Classes.Direct.ItemContent;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.Items
{
    public class MediaShareItem : DirectItem
    {
        [JsonProperty("media_share")] public DirectMediaShare MediaShare { get; set; }
    }
}