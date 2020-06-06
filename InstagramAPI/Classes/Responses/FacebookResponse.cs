using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Responses
{
    internal class FacebookLoginResponse
    {
        [JsonProperty("logged_in_user")] public BaseUser LoggedInUser { get; set; }

        [JsonProperty("code")] public int? Code { get; set; }

        [JsonProperty("fb_user_id")] public string FbUserId { get; set; }

        [JsonProperty("status")] public string Status { get; set; }

        [JsonProperty("created_user")] public BaseUser CreatedUser { get; set; }

        [JsonProperty("multiple_users_on_device")] public bool? MultipleUsersOnDevice { get; set; }
    }

    [JsonConverter(typeof(JsonPathConverter))]
    internal class FacebookRegistrationResponse
    {
        [JsonProperty("account_created")] public bool? AccountCreated { get; set; }

        [JsonProperty("dryrun_passed")] public bool? DryrunPassed { get; set; }

        [JsonProperty("tos_version")] public string TosVersion { get; set; }

        [JsonProperty("gdpr_required")] public bool? GdprRequired { get; set; }

        [JsonProperty("fb_user_id")] public string FbUserId { get; set; }

        [JsonProperty("status")] public string Status { get; set; }

        [JsonProperty("username_suggestions_with_metadata.suggestions", NullValueHandling = NullValueHandling.Ignore)] 
        public InstaRegistrationSuggestion[] Suggestions { get; set; }
    }

    public class InstaRegistrationSuggestion
    {
        [JsonProperty("username")]
        public string Username { get; set; }
        [JsonProperty("prototype")]
        public string Prototype { get; set; }
    }
}