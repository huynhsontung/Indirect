using System;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Story
{
    public class Owner  // GraphUser type
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("profile_pic_url")]
        public Uri ProfilePicUrl { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("followed_by_viewer")]
        public bool FollowedByViewer { get; set; }

        [JsonProperty("requested_by_viewer")]
        public bool RequestedByViewer { get; set; }
    }
}
