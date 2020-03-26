using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct.ItemContent
{
    public class VideoCallEvent
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("vc_id")]
        public long VcId { get; set; }

        [JsonProperty("encoded_server_data_info")]
        public string EncodedServerDataInfo { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("text_attributes")]
        public object[] TextAttributes { get; set; }

        [JsonProperty("did_join")]
        public object DidJoin { get; set; }

        // todo: expand video call event
    }
}
