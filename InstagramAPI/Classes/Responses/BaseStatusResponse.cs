using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Responses
{
    public class BaseStatusResponse
    {
        [JsonProperty("status")] public string Status { get; set; }

        public bool IsOk()
        {
            return !string.IsNullOrEmpty(Status) && Status.ToLower() == "ok";
        }
    }
}
