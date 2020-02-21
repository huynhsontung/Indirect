using System;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.User
{
    public class UserShort : IEquatable<UserShort>
    {
        [JsonProperty("is_verified")] public bool IsVerified { get; set; }

        [JsonProperty("is_private")] public bool IsPrivate { get; set; }

        [JsonProperty("pk")] public long Pk { get; set; }

        [JsonProperty("profile_pic_url")] public string ProfilePictureUrl { get; set; }

        [JsonProperty("profile_pic_id")] public string ProfilePictureId { get; set; } = "unknown";

        [JsonProperty("username")] public string Username { get; set; }

        [JsonProperty("full_name")] public string FullName { get; set; }

        public static UserShort Empty => new UserShort {FullName = string.Empty, Username = string.Empty};

        public bool Equals(UserShort user)
        {
            return Pk == user?.Pk;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as UserShort);
        }

        public override int GetHashCode()
        {
            return Pk.GetHashCode();
        }
    }
}