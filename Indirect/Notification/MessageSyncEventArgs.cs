using System;
using System.Collections.Generic;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Classes.ResponseWrappers.Direct;
using InstaSharper.Converters.Directs;
using Newtonsoft.Json;

namespace Indirect.Notification
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
        public string RawValue { get; set; }

        public InstaDirectInboxItemResponse Value => JsonConvert.DeserializeObject<InstaDirectInboxItemResponse>(RawValue);

        public InstaDirectInboxItem Item
        {
            get
            {
                var converter = new InstaDirectThreadItemConverter() {SourceObject = Value};
                return converter.Convert();
            }
        }
    }
}
