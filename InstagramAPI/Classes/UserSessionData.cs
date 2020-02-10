using System;
using InstagramAPI.Classes.User;

namespace InstagramAPI.Classes
{
    [Serializable]
    public class UserSessionData
    {
        public string Username { get; internal set; }
        public string Password { get; internal set; }

        public UserShort LoggedInUser { get; internal set; }
        public string RankToken { get; internal set; }

        public string CsrfToken { get; internal set; }

        /// <summary>
        ///     Only for facebook login
        /// </summary>
        public string FacebookUserId { get; internal set; } = string.Empty;

        public string FacebookAccessToken { get; internal set; } = string.Empty;
    }
}