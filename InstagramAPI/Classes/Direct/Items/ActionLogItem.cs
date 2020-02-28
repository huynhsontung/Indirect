using InstagramAPI.Classes.Direct.ItemContent;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.Items
{
    public class ActionLogItem : DirectItem
    {
        [JsonProperty("action_log")] public DirectActionLog ActionLog { get; set; }
    }
}