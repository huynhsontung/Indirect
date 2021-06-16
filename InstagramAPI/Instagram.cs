using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Web.Http;
using InstagramAPI.Classes.Android;
using InstagramAPI.Classes.Challenge;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Classes.User;
using InstagramAPI.Push;
using InstagramAPI.Sync;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HttpClient = Windows.Web.Http.HttpClient;
using InstagramAPI.Classes.Core;

namespace InstagramAPI
{
    public partial class Instagram
    {
        private static readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

        // TODO: Remove IsUserAuthenticatedPersistent after migration
        public static bool IsUserAuthenticatedPersistent => (bool?)LocalSettings.Values["_isUserAuthenticated"] ?? false;

        public bool IsUserAuthenticated => Session?.IsAuthenticated ?? false;
        public UserSessionData Session { get; private set; }
        public AndroidDevice Device => Session.Device;
        public PushClient PushClient { get; }
        public SyncClient SyncClient { get; }

        private readonly HttpClient _httpClient;

        public Instagram(UserSessionData session)
        {
#if DEBUG
            DebugLogger.LogLevel = LogLevel.All;
#endif
            if (session == null)
            {
                session = new UserSessionData();
            }

            Session = session;
            PushClient = new PushClient(this);
            SyncClient = new SyncClient(this);

            _httpClient = new HttpClient(CookieHelper.SetCookies(session.Cookies));
            SetDefaultRequestHeaders(_httpClient, session);

            PushClient.ExceptionsCaught += (sender, args) =>
            {
                var e = (Exception) args.ExceptionObject;
                DebugLogger.LogException(e, properties: e.Data);
            };
        }

        /// <summary>
        /// No need to clear data. If IsUserAuthenticated is false, next time when constructor is called,
        /// data will not be loaded.
        /// </summary>
        public async Task Logout()
        {
            await SessionManager.TryRemoveSessionAsync(Session);
            SyncClient.Shutdown();
            PushClient.Shutdown();
            CookieHelper.ClearCookies();
            Session = new UserSessionData(Device);
        }

        public async Task<bool> UpdateLoggedInUser()
        {
            var result = await GetCurrentUserAsync();
            if (!result.IsSucceeded) return false;
            Session.LoggedInUser = result.Value;
            return true;
        }

        public async Task<Result<CurrentUser>> GetCurrentUserAsync()
        {
            ValidateLoggedIn();
            try
            {
                var instaUri = UriCreator.GetCurrentUserUri();
                var fields = new Dictionary<string, string>
                {
                    {"_uuid", Device.Uuid.ToString()},
                    {"_uid", Session.LoggedInUser.Pk.ToString()},
                    {"_csrftoken", Session.CsrfToken}
                };
                var response = await _httpClient.PostAsync(instaUri, new HttpFormUrlEncodedContent(fields));
                var json = await response.Content.ReadAsStringAsync();
                DebugLogger.LogResponse(response);

                if (response.StatusCode != HttpStatusCode.Ok)
                    return Result<CurrentUser>.Fail(json, response.ReasonPhrase);
                var statusResponse = JObject.Parse(json);
                if (statusResponse["status"].ToObject<string>() != "ok")
                    Result<CurrentUser>.Fail(json);

                var user = statusResponse["user"].ToObject<CurrentUser>();
                if (user.Pk < 1)
                    Result<CurrentUser>.Fail(json, "Pk is incorrect");
                return Result<CurrentUser>.Success(user, json);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception);
                return Result<CurrentUser>.Except(exception);
            }
        }

        public async Task<Result<UserInfo>> GetUserInfoAsync(long userId)
        {
            ValidateLoggedIn();
            try
            {
                var uri = UriCreator.GetUserInfoUri(userId);
                var response = await _httpClient.GetAsync(uri);
                var json = await response.Content.ReadAsStringAsync();
                DebugLogger.LogResponse(response);

                if (!response.IsSuccessStatusCode)
                    return Result<UserInfo>.Fail(json, response.ReasonPhrase);
                var userInfoResponse = JsonConvert.DeserializeObject<UserInfoResponse>(json);
                return userInfoResponse.IsOk()
                    ? Result<UserInfo>.Success(userInfoResponse.User)
                    : Result<UserInfo>.Fail(json);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception);
                return Result<UserInfo>.Except(exception);
            }
        }
    }
}
