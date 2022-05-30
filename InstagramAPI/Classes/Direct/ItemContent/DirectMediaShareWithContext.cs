using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    public class DirectMediaShareWithContext
    {
        [JsonProperty("text")] public string Text { get; set; }
        [JsonProperty("media_share_type")] public string MediaShareType { get; set; }
        [JsonProperty("tagged_user_id")] public long TaggedUserId { get; set; }
        [JsonProperty("media")] public DirectMediaShare Media { get; set; }
    }
}
