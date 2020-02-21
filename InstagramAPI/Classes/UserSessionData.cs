using System;
using InstagramAPI.Classes.User;

namespace InstagramAPI.Classes
{
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

        public void SaveToAppSettings()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var composite = new Windows.Storage.ApplicationDataCompositeValue
            {
                ["Username"] = Username,
                ["Password"] = Password,
                ["RankToken"] = RankToken,
                ["CsrfToken"] = CsrfToken,
                ["FacebookUserId"] = FacebookUserId,
                ["FacebookAccessToken"] = FacebookAccessToken,
                ["LoggedInUser.IsVerified"] = LoggedInUser.IsVerified,
                ["LoggedInUser.IsPrivate"] = LoggedInUser.IsPrivate,
                ["LoggedInUser.Pk"] = LoggedInUser.Pk,
                ["LoggedInUser.ProfilePictureUrl"] = LoggedInUser.ProfilePictureUrl,
                ["LoggedInUser.ProfilePictureId"] = LoggedInUser.ProfilePictureId,
                ["LoggedInUser.Username"] = LoggedInUser.Username,
                ["LoggedInUser.FullName"] = LoggedInUser.FullName
            };
            localSettings.Values["_userSessionData"] = composite;
        }

        public void LoadFromAppSettings()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var composite = (Windows.Storage.ApplicationDataCompositeValue)localSettings.Values["_userSessionData"];
            if (composite == null) return;
            Username = (string) composite["Username"];
            Password = (string) composite["Password"];
            RankToken = (string) composite["RankToken"];
            CsrfToken = (string) composite["CsrfToken"];
            FacebookUserId = (string) composite["FacebookUserId"];
            FacebookAccessToken = (string) composite["FacebookAccessToken"];
            LoggedInUser = new UserShort
            {
                IsVerified = (bool) composite["LoggedInUser.IsVerified"],
                IsPrivate = (bool) composite["LoggedInUser.IsPrivate"],
                Pk = (long) composite["LoggedInUser.Pk"],
                ProfilePictureUrl = (string) composite["LoggedInUser.ProfilePictureUrl"],
                ProfilePictureId = (string) composite["LoggedInUser.ProfilePictureId"],
                Username = (string) composite["LoggedInUser.Username"],
                FullName = (string) composite["LoggedInUser.FullName"]
            };
        }

        public static UserSessionData CreateFromAppSettings()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var composite = (Windows.Storage.ApplicationDataCompositeValue) localSettings.Values["_userSessionData"];
            if (composite == null) return null;
            var session = new UserSessionData();
            session.LoadFromAppSettings();
            return session;
        }
    }
}