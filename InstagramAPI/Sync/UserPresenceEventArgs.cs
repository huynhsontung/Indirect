using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstagramAPI.Classes.Responses;
using Newtonsoft.Json;

namespace InstagramAPI.Sync
{
    public class UserPresenceEventArgs : UserPresenceValue
    {
        [JsonProperty("user_id")]
        public long UserId { get; set; }
    }
}
