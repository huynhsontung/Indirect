using System;
using System.Threading.Tasks;
using InstagramAPI.Classes.Android;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Classes.User;
using InstagramAPI.Push;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using InstagramAPI.Classes.Core;
using InstagramAPI.Realtime;

namespace InstagramAPI
{
    public partial class Instagram
    {
        public bool IsUserAuthenticated => Session?.IsAuthenticated ?? false;
        public UserSessionData Session { get; }
        public AndroidDevice Device => Session.Device;
        public PushClient PushClient { get; }
        public RealtimeClient RealtimeClient { get; }
        public HttpClientManager HttpClient { get; }

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
            HttpClient = new HttpClientManager(session);
            PushClient = new PushClient(this);
            RealtimeClient = new RealtimeClient(this);

            PushClient.ExceptionsCaught += (sender, args) =>
            {
                var e = (Exception) args.ExceptionObject;
                DebugLogger.LogException(e, properties: e.Data);
            };
        }

        public async Task<CurrentUser> UpdateLoggedInUser()
        {
            var result = await GetCurrentUserAsync();
            if (!result.IsSucceeded) return null;
            Session.LoggedInUser = result.Value;
            return result.Value;
        }

        public async Task<Result<CurrentUser>> GetCurrentUserAsync()
        {
            ValidateLoggedIn();
            try
            {
                var instaUri = UriCreator.GetCurrentUserUri();
                var response = await HttpClient.GetAsync(instaUri);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
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
                var response = await HttpClient.GetAsync(uri);
                var json = await response.Content.ReadAsStringAsync();

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
