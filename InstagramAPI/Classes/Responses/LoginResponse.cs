using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Responses
{
    public class LoginResponse
    {
        [JsonProperty("status")] public string Status { get; set; }

        [JsonProperty("logged_in_user")] public InstaUser User { get; set; }
    }
}
