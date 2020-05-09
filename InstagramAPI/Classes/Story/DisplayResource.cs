using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Story
{
    /// <summary>
    /// Store story image or story video poster image
    /// </summary>
    public class DisplayResource
    {
        [JsonProperty("src")]
        public Uri Src { get; set; }

        [JsonProperty("config_width")]
        public long ConfigWidth { get; set; }

        [JsonProperty("config_height")]
        public long ConfigHeight { get; set; }
    }
}
