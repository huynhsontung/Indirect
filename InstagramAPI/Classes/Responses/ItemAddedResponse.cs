using System.Collections.Generic;
using InstagramAPI.Classes.Direct;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Responses
{
    public class ItemAddedResponse : BaseStatusResponse
    {
        [JsonProperty("threads")]
        public DirectThread[] Threads { get; set; }
    }
}