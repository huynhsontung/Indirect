using System;
using System.Net;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.User;
using InstagramAPI.Push;
using Newtonsoft.Json;
using InstagramAPI.Classes.Android;
using InstagramAPI.Classes.Challenge;
using Newtonsoft.Json.Linq;

namespace InstagramAPI.Classes.Core
{
    public class UserSessionData
    {
        [JsonIgnore] public bool IsAuthenticated => LoggedInUser?.Pk > 0;

        [JsonIgnore] public string SessionName => IsAuthenticated ? LoggedInUser.Pk.ToString() : string.Empty;

        [JsonIgnore] public TwoFactorLoginInfo TwoFactorInfo { get; internal set; }

        [JsonIgnore] public ChallengeLoginInfo ChallengeInfo { get; internal set; }

        [JsonProperty]
        public string Username { get; set; }

        [JsonIgnore]
        public string Password { get; set; }

        [JsonIgnore]
        public byte PasswordEncryptionKeyId { get; internal set; }

        [JsonIgnore]
        public IBuffer PasswordEncryptionPubKey { get; internal set; }

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

        [JsonProperty]
        public string WwwClaim { get; internal set; }

        [JsonProperty]
        public string Mid { get; internal set; }

        [JsonProperty]
        [JsonConverter(typeof(CookieCollectionConverter))]
        public CookieCollection Cookies { get; internal set; }

        [JsonProperty]
        internal PushConnectionData PushData { get; }

        [JsonProperty]
        public AndroidDevice Device { get; }

        [JsonIgnore]
        public string SessionId
        {
            get
            {
                if (!string.IsNullOrEmpty(AuthorizationToken))
                {
                    var base64 = AuthorizationToken.Substring("Bearer IGT:2:".Length);
                    if (!string.IsNullOrEmpty(base64))
                    {
                        var buffer = CryptographicBuffer.DecodeFromBase64String(base64);
                        var json = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, buffer);
                        var jObject = JObject.Parse(json);
                        return jObject["sessionid"]?.ToObject<string>();
                    }
                }

                return string.Empty;
            }
        }

        public UserSessionData()
        {
            Device = AndroidDevice.GetRandomAndroidDevice();
            PushData = new PushConnectionData(Device);
            WwwClaim = "0";
        }
    }
}