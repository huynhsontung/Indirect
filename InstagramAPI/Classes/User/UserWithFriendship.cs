using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.User
{
    public class UserWithFriendship : InstaUser
    {
        [JsonProperty("friendship_status")] public FriendshipStatus FriendshipStatus { get; set; }
    }

    public class FriendshipStatus
    {
        [JsonProperty("following")] public bool Following { get; set; }

        [JsonProperty("is_private")] public bool IsPrivate { get; set; }

        [JsonProperty("incoming_request")] public bool IncomingRequest { get; set; }

        [JsonProperty("outgoing_request")] public bool OutgoingRequest { get; set; }

        [JsonProperty("is_bestie")] public bool IsBestie { get; set; }

        [JsonProperty("is_restricted")] public bool IsRestricted { get; set; }
    }
}
