using System.Collections.Generic;
using InstagramAPI.Classes.Direct;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Responses
{
    public class TextSentResponse : BaseStatusResponse
    {
        [JsonProperty("threads")]
        public List<DirectThread> Threads { get; set; } = new List<DirectThread>();
    }
}