using System;
using InstagramAPI.Classes.JsonConverters;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.User
{
    public class BaseUser : IEquatable<BaseUser>
    {
        [JsonProperty("is_verified")] public bool? IsVerified { get; set; }

        [JsonProperty("is_private")] public bool? IsPrivate { get; set; }

        [JsonProperty("pk")] public long Pk { get; set; }

        [JsonProperty("profile_pic_url")] public Uri ProfilePictureUrl { get; set; }

        [JsonProperty("profile_pic_id")] public string ProfilePictureId { get; set; }

        [JsonProperty("username")] public string Username { get; set; }

        [JsonProperty("full_name")] public string FullName { get; set; }

        [JsonProperty("has_anonymous_profile_picture")] public bool? HasAnonymousProfilePicture { get; set; }

        [JsonProperty("latest_reel_media")] public long? LatestReelMedia { get; set; }

        [JsonProperty("interop_user_type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(BoolConverter))]
        public bool InteropUserType { get; set; }

        [JsonProperty("interop_messaging_user_fbid", NullValueHandling = NullValueHandling.Ignore)]
        public long? InteropMessagingUserFbid { get; set; }

        [JsonIgnore]
        public Uri ProfileUrl => string.IsNullOrEmpty(Username) ? null : new Uri($"https://www.instagram.com/{Username}/");

        public bool Equals(BaseUser user)
        {
            return Pk == user?.Pk;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BaseUser);
        }

        public override int GetHashCode()
        {
            return Pk.GetHashCode();
        }
    }
}