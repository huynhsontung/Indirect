using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct
{
    public class RankedRecipientThread : DirectThread
    {
        [JsonProperty("thread_title")] public string ThreadTitle { get; set; }
    }
}