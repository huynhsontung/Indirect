using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    public class ItemClip
    {
        [JsonProperty("clip")]
        public Clip Clip { get; set; }
    }
}
