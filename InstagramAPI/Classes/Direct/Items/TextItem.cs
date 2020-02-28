using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.Items
{
    public class TextItem : DirectItem
    {
        [JsonProperty("text")] public string Text { get; set; }
    }
}