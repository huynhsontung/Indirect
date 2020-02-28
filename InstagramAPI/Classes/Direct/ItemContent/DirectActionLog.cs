using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    public class DirectActionLog
    {
        [JsonProperty("description")] public string Description { get; set; }
    }
}