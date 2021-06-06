using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.Responses;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct
{
    public class DirectThreadItemsOnly : BaseStatusResponse
    {
        [JsonProperty("items")]
        public List<DirectItem> Items { get; set; }
    }
}
