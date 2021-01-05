using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Challenge
{
    public class ChallengeSelectMethodStep
    {
        [JsonProperty("choice")]
        public string Choice { get; set; }

        [JsonProperty("fb_access_token")]
        public string FbAccessToken { get; set; }

        [JsonProperty("big_blue_token")]
        public string BigBlueToken { get; set; }

        [JsonProperty("google_oauth_token")]
        public string GoogleOauthToken { get; set; }

        [JsonProperty("vetted_device")]
        public string VettedDevice { get; set; }

        [JsonProperty("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
