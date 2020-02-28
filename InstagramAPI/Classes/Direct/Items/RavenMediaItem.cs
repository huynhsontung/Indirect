using InstagramAPI.Classes.Direct.ItemContent;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.Items
{
    public class RavenMediaItem : DirectItem
    {
        // Only appear in message sync version
        [JsonProperty("raven_media")] public VisualMedia RavenMedia { get; set; }

        [JsonProperty("visual_media")] public VisualMediaContainer VisualMedia { get; set; }
    }
}