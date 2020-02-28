using InstagramAPI.Classes.Direct.ItemContent;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.Items
{
    public class AnimatedMediaItem : DirectItem
    {
        [JsonProperty("animated_media")] public AnimatedMedia AnimatedMedia { get; set; }
    }
}