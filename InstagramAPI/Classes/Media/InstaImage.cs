using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Media
{
    public class InstaImage
    {
        [JsonProperty("url")] public Uri Url { get; set; }

        [JsonProperty("width")] public int Width { get; set; }

        [JsonProperty("height")] public int Height { get; set; }
    }
}
