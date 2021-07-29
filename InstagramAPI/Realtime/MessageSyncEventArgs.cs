using System;
using System.Collections.Generic;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;

namespace InstagramAPI.Realtime
{
    public class MessageSyncEventArgs : EventArgs
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("data")]
        public List<SyncItem> Data { get; set; }

        [JsonProperty("message_type")]
        public long MessageType { get; set; }

        [JsonProperty("seq_id")]
        public long SeqId { get; set; }

        [JsonProperty("mutation_token")]
        public string MutationToken { get; set; }

        [JsonProperty("realtime")]
        public bool Realtime { get; set; }
    }
}
