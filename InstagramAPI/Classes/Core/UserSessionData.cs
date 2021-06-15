using System;
using System.Collections.Generic;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.User;
using InstagramAPI.Push;
using Newtonsoft.Json;
using Windows.Web.Http;
using InstagramAPI.Classes.Android;

namespace InstagramAPI.Classes.Core
{
    public class UserSessionData
    {
        [JsonIgnore] public string CsrfToken => Instagram.GetCsrfToken();

        [JsonIgnore] public bool IsAuthenticated => LoggedInUser?.Pk > 0;

        [JsonIgnore] public string SessionName => IsAuthenticated ? LoggedInUser.Pk.ToString() : string.Empty;

        [JsonProperty]
        public string Username { get; internal set; }

        [JsonProperty]
        public string Password { get; internal set; }

        [JsonProperty]
        public BaseUser LoggedInUser { get; internal set; }

        /// <summary>
        ///     Only for facebook login
        /// </summary>
        [JsonProperty]
        public string FacebookUserId { get; internal set; }

        [JsonProperty]
        public string FacebookAccessToken { get; internal set; }

        [JsonProperty]
        public string AuthorizationToken { get; internal set; }

        [JsonProperty(ItemConverterType = typeof(HttpCookieConverter))]
        internal List<HttpCookie> Cookies { get; set; }

        [JsonProperty]
        internal FbnsConnectionData PushData { get; }

        [JsonProperty]
        internal AndroidDevice Device { get; }

        public UserSessionData(AndroidDevice device = null, FbnsConnectionData pushData = null)
        {
            if (device == null)
            {
                device = AndroidDevice.GetRandomAndroidDevice();
            }

            if (pushData == null)
            {
                pushData = new FbnsConnectionData();
            }

            Device = device;
            PushData = pushData;
        }

        public void LoadFromAppSettings()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var composite = (Windows.Storage.ApplicationDataCompositeValue)localSettings.Values["_userSessionData"];
            if (composite == null) return;
            Username = (string)composite["Username"];
            Password = (string)composite["Password"];
            FacebookUserId = (string)composite["FacebookUserId"];
            FacebookAccessToken = (string)composite["FacebookAccessToken"];
            AuthorizationToken = (string) composite["AuthorizationToken"];
            LoggedInUser = new BaseUser
            {
                IsVerified = (bool)composite["LoggedInUser.IsVerified"],
                IsPrivate = (bool)composite["LoggedInUser.IsPrivate"],
                Pk = (long)composite["LoggedInUser.Pk"],
                ProfilePictureUrl = new Uri((string)composite["LoggedInUser.ProfilePictureUrl"]),
                ProfilePictureId = (string)composite["LoggedInUser.ProfilePictureId"],
                Username = (string)composite["LoggedInUser.Username"],
                FullName = (string)composite["LoggedInUser.FullName"]
            };

            //var test = SessionManager.TryLoadLastSessionAsync();
        }

        public static void RemoveFromAppSettings()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values.Remove("_userSessionData");
        }
    }
}