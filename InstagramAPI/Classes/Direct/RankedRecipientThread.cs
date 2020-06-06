using System.Collections.Generic;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct
{
    public class RankedRecipientThread
    {
        [JsonProperty("canonical")] public bool Canonical { get; set; }

        [JsonProperty("named")] public bool Named { get; set; }

        [JsonProperty("pending")] public bool Pending { get; set; }

        [JsonProperty("thread_id")] public string ThreadId { get; set; }

        [JsonProperty("thread_title")] public string ThreadTitle { get; set; }

        [JsonProperty("thread_type")] public string ThreadType { get; set; }

        [JsonProperty("users")] public List<BaseUser> Users { get; set; }

        [JsonProperty("viewer_id")] public long ViewerId { get; set; }
    }
}