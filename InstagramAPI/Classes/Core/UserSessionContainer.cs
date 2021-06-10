using System;

namespace InstagramAPI.Classes.Core
{
    public struct UserSessionContainer
    {
        public UserSessionData Session { get; set; }

        public Uri ProfilePicture { get; set; }
    }
}
