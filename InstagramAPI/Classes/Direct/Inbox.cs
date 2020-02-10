using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct
{
    public class Inbox
    {
        [JsonProperty("has_older")] public bool HasOlder { get; set; }

        [JsonProperty("unseen_count_ts")] public long UnseenCountTs { get; set; }

        [JsonProperty("unseen_count")] public long UnseenCount { get; set; }

        [JsonProperty("threads")] public List<InboxThread> Threads { get; set; }

        [JsonProperty("oldest_cursor")] public string OldestCursor { get; set; }

        [JsonProperty("blended_inbox_enabled")] public bool BlendedInboxEnabled { get; set; }
    }
}
