using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace InstagramAPI.Classes.Story
{
    public class VideoResource
    {
        [JsonProperty("src")]
        public Uri Src { get; set; }

        [JsonProperty("config_width")]
        public long ConfigWidth { get; set; }

        [JsonProperty("config_height")]
        public long ConfigHeight { get; set; }

        [JsonProperty("mime_type")]
        public string MimeType { get; set; }

        [JsonProperty("profile")]
        [JsonConverter(typeof(StringEnumConverter))]
        public VideoProfile Profile { get; set; }
    }

    public enum VideoProfile
    {
        [EnumMember(Value = "MAIN")]
        Main,
        [EnumMember(Value = "BASELINE")]
        Baseline,
    }
}
