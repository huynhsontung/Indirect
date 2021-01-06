using System;
using System.Collections.Generic;
using System.Linq;
using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    public class ReactionsContainer
    {
        [JsonProperty("likes")] public LikeReaction[] Likes { get; set; }
        
        [JsonProperty("emojis")] public EmojiReaction[] Emojis { get; set; }
        
        [JsonProperty("likes_count")] public uint LikesCount { get; set; }
    }

    public class LikeReaction
    {
        [JsonProperty("sender_id")] public long SenderId { get; set; }
        
        [JsonProperty("client_context")] public string ClientContext { get; set; }
        
        [JsonProperty("super_react_type")] public string SuperReactType { get; set; }

        [JsonProperty("timestamp")] 
        [JsonConverter(typeof(MicroTimestampConverter))]
        public DateTimeOffset Timestamp { get; set; }
    }

    public class EmojiReaction : LikeReaction
    {
        [JsonProperty("emoji")] public string Emoji { get; set; }
    }
}
