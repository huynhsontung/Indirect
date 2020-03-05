using System;
using System.Collections.Generic;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;

namespace InstagramAPI.Sync
{

    public class MessageSyncEventArgs : EventArgs
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("data")]
        public List<ItemSyncData> Data { get; set; }

        [JsonProperty("message_type")]
        public long MessageType { get; set; }

        [JsonProperty("seq_id")]
        public long SeqId { get; set; }

        [JsonProperty("mutation_token")]
        public string MutationToken { get; set; }

        [JsonProperty("realtime")]
        public bool Realtime { get; set; }

        public static MessageSyncEventArgs FromJson(string json) =>
            JsonConvert.DeserializeObject<MessageSyncEventArgs>(json);
    }

    public class ItemSyncData
    {
        [JsonProperty("op")]
        public string Op { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("value")]
        [JsonConverter(typeof(DirectItemConverter))]
        public DirectItem Item { get; set; }
    }
}
