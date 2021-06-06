using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Storage;
using Windows.Web.Http;
using InstagramAPI.Classes;
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
using HttpMethod = Windows.Web.Http.HttpMethod;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
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
        public TwoFactorLoginInfo TwoFactorInfo { get; private set; }  // Only used when login returns two factor
        public ChallengeLoginInfo ChallengeInfo { get; private set; }  // Only used when login returns challenge

        private readonly HttpClient _httpClient;
        private readonly ApiRequestMessage _apiRequestMessage;

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
            _apiRequestMessage = new ApiRequestMessage(this);
            SetDefaultRequestHeaders();

            PushClient.ExceptionsCaught += (sender, args) =>
            {
                var e = (Exception) args.ExceptionObject;
                DebugLogger.LogException(e, properties: e.Data);
            };
        }

        /// <summary>
        ///     Login using given credentials asynchronously
        /// </summary>
        /// <param name="isNewLogin"></param>
        /// <returns>
        ///     Success --> is succeed
        ///     TwoFactorRequired --> requires 2FA login.
        ///     BadPassword --> Password is wrong
        ///     InvalidUser --> User/phone number is wrong
        ///     Exception --> Something wrong happened
        ///     ChallengeRequired --> You need to pass Instagram challenge
        /// </returns>
        public async Task<Result<LoginResult>> LoginAsync(string username, string password, bool isNewLogin = true)
        {
            if (Session.IsAuthenticated)
            {
                return Result<LoginResult>.Success(LoginResult.Success);
            }

            Session.Username = username;
            Session.Password = password;
            ValidateRequestMessage();
            try
            {
                if (isNewLogin)
                {
                    var firstResponse = await _httpClient.GetAsync(UriCreator.BaseInstagramUri);
                    DebugLogger.LogResponse(firstResponse);
                }

                var csrftoken = GetCsrfToken();
                var loginUri = UriCreator.GetLoginUri();
                var signature =
                    $"SIGNATURE.{_apiRequestMessage.GetChallengeMessageString(csrftoken)}";
                var fields = new Dictionary<string, string>
                {
                    {"signed_body", signature}
                };
                var request = new HttpRequestMessage(HttpMethod.Post, loginUri);
                request.Headers.Host = new HostName("i.instagram.com");
                request.Content = new HttpFormUrlEncodedContent(fields);
                var response = await _httpClient.SendRequestAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                DebugLogger.LogResponse(response);

                if (response.StatusCode != HttpStatusCode.Ok)
                {
                    var loginFailReason = JsonConvert.DeserializeObject<LoginFailedResponse>(json);

                    if (loginFailReason.InvalidCredentials)
                        return Result<LoginResult>.Fail(loginFailReason.ErrorType == "bad_password"
                                ? LoginResult.BadPassword
                                : LoginResult.InvalidUser,
                            "Invalid Credentials", json
                        );
                    if (loginFailReason.TwoFactorRequired)
                    {
                        if (loginFailReason.TwoFactorLoginInfo != null)
                            Session.Username = loginFailReason.TwoFactorLoginInfo.Username;
                        TwoFactorInfo = loginFailReason.TwoFactorLoginInfo;
                        //2FA is required!
                        return Result<LoginResult>.Fail(LoginResult.TwoFactorRequired, "Two Factor Authentication is required", json);
                    }
                    if (loginFailReason.ErrorType == "checkpoint_challenge_required"
                       /* || !string.IsNullOrEmpty(loginFailReason.Message) && loginFailReason.Message == "challenge_required"*/)
                    {
                        ChallengeInfo = loginFailReason.Challenge;
                        
                        return Result<LoginResult>.Fail(LoginResult.ChallengeRequired, "Challenge is required", json);
                    }
                    if (loginFailReason.ErrorType == "rate_limit_error")
                    {
                        return Result<LoginResult>.Fail(LoginResult.LimitError, "Please wait a few minutes before you try again.", json);
                    }
                    if (loginFailReason.ErrorType == "inactive user" || loginFailReason.ErrorType == "inactive_user")
                    {
                        return Result<LoginResult>.Fail(LoginResult.InactiveUser, $"{loginFailReason.Message}\r\nHelp url: {loginFailReason.HelpUrl}");
                    }
                    if (loginFailReason.ErrorType == "checkpoint_logged_out")
                    {
                        return Result<LoginResult>.Fail(LoginResult.CheckpointLoggedOut, $"{loginFailReason.ErrorType} {loginFailReason.CheckpointUrl}");
                    }
                    return Result<LoginResult>.Fail(LoginResult.Exception, json: json);
                }
                var loginInfo = JsonConvert.DeserializeObject<LoginResponse>(json);
                if (loginInfo.User == null)
                {
                    return Result<LoginResult>.Fail(LoginResult.Exception, "User is null!", json);
                }

                Session.AuthorizationToken = GetAuthToken(response.Headers);
                Session.Username = loginInfo.User.Username;
                Session.LoggedInUser = loginInfo.User;
                SetDefaultRequestHeaders();
                return Result<LoginResult>.Success(LoginResult.Success, json: json);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception);
                return Result<LoginResult>.Except(exception, LoginResult.Exception);
            }
        }

        /// <summary>
        ///     Login with Facebook access token
        /// </summary>
        /// <param name="fbAccessToken">Facebook access token</param>
        /// <returns>
        ///     Success --> is succeed
        ///     TwoFactorRequired --> requires 2FA login.
        ///     BadPassword --> Password is wrong
        ///     InvalidUser --> User/phone number is wrong
        ///     Exception --> Something wrong happened
        ///     ChallengeRequired --> You need to pass Instagram challenge
        /// </returns>
        public async Task<Result<LoginResult>> LoginWithFacebookAsync(string fbAccessToken)
        {
            if (Session.IsAuthenticated)
            {
                return Result<LoginResult>.Success(LoginResult.Success);
            }

            try
            {
                if (string.IsNullOrEmpty(fbAccessToken)) throw new ArgumentNullException(nameof(fbAccessToken));
                if (GetCsrfToken() == string.Empty)
                {
                    var firstResponse = await _httpClient.GetAsync(UriCreator.BaseInstagramUri);
                    DebugLogger.LogResponse(firstResponse);
                }

                var instaUri = UriCreator.GetFacebookSignUpUri();

                var data = new JObject
                {
                    {"dryrun", "true"},
                    {"phone_id", Device.PhoneId.ToString()},
                    {"_csrftoken", GetCsrfToken()},
                    {"adid", Guid.NewGuid().ToString()},
                    {"guid",  Device.Uuid.ToString()},
                    {"_uuid",  Device.Uuid.ToString()},
                    {"device_id", Device.DeviceId},
                    {"waterfall_id", Guid.NewGuid().ToString()},
                    {"fb_access_token", fbAccessToken},
                };

                Session.FacebookAccessToken = fbAccessToken;
                var request = GetSignedRequest(instaUri, data);
                var response = await _httpClient.SendRequestAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                DebugLogger.LogResponse(response);

                if (response.StatusCode != HttpStatusCode.Ok)
                {
                    var loginFailReason = JsonConvert.DeserializeObject<LoginFailedResponse>(json);

                    if (loginFailReason.InvalidCredentials)
                        return Result<LoginResult>.Fail(loginFailReason.ErrorType == "bad_password"
                            ? LoginResult.BadPassword
                            : LoginResult.InvalidUser, "Invalid Credentials", json);
                    if (loginFailReason.TwoFactorRequired)
                    {
                        if (loginFailReason.TwoFactorLoginInfo != null)
                            Session.Username = loginFailReason.TwoFactorLoginInfo.Username;
                        TwoFactorInfo = loginFailReason.TwoFactorLoginInfo;
                        return Result<LoginResult>.Fail(LoginResult.TwoFactorRequired, "Two Factor Authentication is required", json);
                    }
                    if (loginFailReason.ErrorType == "checkpoint_challenge_required")
                    {
                        ChallengeInfo = loginFailReason.Challenge;

                        return Result<LoginResult>.Fail(LoginResult.ChallengeRequired, "Challenge is required", json);
                    }
                    if (loginFailReason.ErrorType == "rate_limit_error")
                    {
                        return Result<LoginResult>.Fail(LoginResult.LimitError, "Please wait a few minutes before you try again.", json);
                    }
                    if (loginFailReason.ErrorType == "inactive user" || loginFailReason.ErrorType == "inactive_user")
                    {
                        return Result<LoginResult>.Fail(LoginResult.InactiveUser, $"{loginFailReason.Message}\r\nHelp url: {loginFailReason.HelpUrl}");
                    }
                    if (loginFailReason.ErrorType == "checkpoint_logged_out")
                    {
                        return Result<LoginResult>.Fail(LoginResult.CheckpointLoggedOut, $"{loginFailReason.ErrorType} {loginFailReason.CheckpointUrl}");
                    }
                    return Result<LoginResult>.Fail(LoginResult.Exception, json: json);
                }

                var fbUserId = string.Empty;
                BaseUser loginInfoUser = null;
                if (json.Contains("\"account_created\""))
                {
                    var rmt = JsonConvert.DeserializeObject<FacebookRegistrationResponse>(json);
                    if (rmt?.AccountCreated != null)
                    {
                        fbUserId = rmt?.FbUserId;
                        if (rmt.AccountCreated.Value)
                        {
                            loginInfoUser = JsonConvert.DeserializeObject<FacebookLoginResponse>(json)?.CreatedUser;
                        }
                        else
                        {
                            return Result<LoginResult>.Fail(LoginResult.Exception, "Facebook account is not linked", json);
                        }
                    }
                }

                if (loginInfoUser == null)
                {
                    var obj = JsonConvert.DeserializeObject<FacebookLoginResponse>(json);
                    fbUserId = obj?.FbUserId;
                    loginInfoUser = obj?.LoggedInUser;
                }

                if (loginInfoUser == null) return Result<LoginResult>.Fail(LoginResult.Exception, json: json);

                Session.AuthorizationToken = GetAuthToken(response.Headers);
                Session.LoggedInUser = loginInfoUser;
                Session.FacebookUserId = fbUserId;
                Session.Username = loginInfoUser.Username;
                Session.Password = "LOGGED_IN_THROUGH_FB";
                SetDefaultRequestHeaders();
                return Result<LoginResult>.Success(LoginResult.Success, json);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception);
                return Result<LoginResult>.Except(exception, LoginResult.Exception);
            }
        }

        public async Task<Result<LoginResult>> LoginWithTwoFactorAsync(string verificationCode)
        {
            if (TwoFactorInfo == null) 
                return Result<LoginResult>.Except(new ArgumentNullException(nameof(TwoFactorInfo), "Cannot login with two factor before logging in normally"));
            try
            {
                var twoFactorData = new JObject
                {
                    {"verification_code", verificationCode},
                    {"username", Session.Username},
                    {"device_id", Device.DeviceId},
                    {"two_factor_identifier", TwoFactorInfo.TwoFactorIdentifier}
                };
                var loginUri = UriCreator.GetTwoFactorLoginUri();
                var signature =
                    $"SIGNATURE.{twoFactorData.ToString(Formatting.None)}";
                var fields = new Dictionary<string, string>
                {
                    {"signed_body", signature}
                };
                var request = new HttpRequestMessage(HttpMethod.Post, loginUri);
                request.Headers.Host = new HostName("i.instagram.com");
                request.Content = new HttpFormUrlEncodedContent(fields);
                var response = await _httpClient.SendRequestAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                DebugLogger.LogResponse(response);

                if (!response.IsSuccessStatusCode)
                {
                    var loginFailReason = JsonConvert.DeserializeObject<LoginFailedResponse>(json);

                    if (loginFailReason.ErrorType == "sms_code_validation_code_invalid")
                        return Result<LoginResult>.Fail(LoginResult.InvalidCode, "Please check the security code.", json);
                    if (loginFailReason.Challenge != null)
                    {
                        ChallengeInfo = loginFailReason.Challenge;
                        return Result<LoginResult>.Fail(LoginResult.ChallengeRequired, "Challenge is required", json);
                    }
                    return Result<LoginResult>.Fail(LoginResult.CodeExpired, "This code is no longer valid, please, call LoginAsync again to request a new one", json);
                }

                var loginInfo =
                    JsonConvert.DeserializeObject<LoginResponse>(json);
                if (loginInfo.User == null)
                {
                    return Result<LoginResult>.Fail(LoginResult.Exception, "User is null!", json);
                }

                Session.AuthorizationToken = GetAuthToken(response.Headers);
                Session.Username = loginInfo.User.Username;
                Session.LoggedInUser = loginInfo.User;
                TwoFactorInfo = null;
                SetDefaultRequestHeaders();
                return Result<LoginResult>.Success(LoginResult.Success);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception);
                return Result<LoginResult>.Except(exception, LoginResult.Exception);
            }
        }

        /// <summary>
        /// No need to clear data. If IsUserAuthenticated is false, next time when constructor is called,
        /// data will not be loaded.
        /// </summary>
        public async Task Logout()
        {
            await SessionManager.TryRemoveSessionAsync(Session);
            await SessionManager.RemoveAllSessions();   // TODO: remove when multiple profile support is in
            SyncClient.Shutdown();
            PushClient.Shutdown();
            PushClient.UnregisterTasks();
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
