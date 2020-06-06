using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Responses
{
    class UserInfoResponse : BaseStatusResponse
    {
        [JsonProperty("user")]
        public UserInfo User { get; set; }
    }
}
