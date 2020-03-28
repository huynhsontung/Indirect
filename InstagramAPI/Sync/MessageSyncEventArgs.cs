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

    public class ItemSyncData : SyncBaseData
    {
        private static readonly DirectItemConverter Converter = new DirectItemConverter();
        private DirectItem _item;

        [JsonIgnore]
        public DirectItem Item
        {
            get
            {
                if (string.IsNullOrEmpty(Value)) return null;
                if (_item == null) _item = JsonConvert.DeserializeObject<DirectItem>(Value, Converter);
                return _item;
            }
        }
    }
}
