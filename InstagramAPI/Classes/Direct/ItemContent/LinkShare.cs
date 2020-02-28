using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    public class LinkShare
    {
        [JsonProperty("text")] public string Text { get; set; }

        [JsonProperty("link_context")] public LinkShareContext LinkContext { get; set; }
    }

    public class LinkShareContext
    {
        [JsonProperty("link_url")] public string LinkUrl { get; set; }

        [JsonProperty("link_title")] public string LinkTitle { get; set; }

        [JsonProperty("link_summary")] public string LinkSummary { get; set; }

        [JsonProperty("link_image_url")] public string LinkImageUrl { get; set; }
    }
}