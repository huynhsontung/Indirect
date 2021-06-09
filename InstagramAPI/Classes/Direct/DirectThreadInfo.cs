using System.Collections.Generic;
using InstagramAPI.Classes.User;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.Direct
{
    [JsonObject]
    public class DirectThreadInfo
    {
        public string ThreadId { get; set; }

        public string Title { get; set; }

        //public List<UserWithFriendship> Users { get; set; }

        public DirectThreadInfo(DirectThread thread)
        {
            if (thread == null)
            {
                return;
            }

            ThreadId = thread.ThreadId;
            Title = thread.Title;
            //Users = thread.Users;
        }
    }
}
