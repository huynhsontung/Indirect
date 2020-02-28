using InstagramAPI.Classes.Direct.ItemContent;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.Items
{
    public class VoiceMediaItem : DirectItem
    {
        [JsonProperty("voice_media")] public VoiceShare VoiceMedia { get; set; }
    }
}