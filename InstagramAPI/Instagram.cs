using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Web.Http;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Android;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Classes.User;
using InstagramAPI.Push;
using InstagramAPI.Sync;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using HttpClient = Windows.Web.Http.HttpClient;

namespace InstagramAPI
{
    public partial class Instagram
    {
        public bool IsUserAuthenticated { get; private set; }
        public UserSessionData Session { get; } = new UserSessionData();
        public AndroidDevice Device { get; } = AndroidDevice.GetRandomAndroidDevice();
        public PushClient PushClient { get; }
        public SyncClient SyncClient { get; }

        private readonly HttpClient _httpClient = new HttpClient();
        private readonly ApiRequestMessage _apiRequestMessage;
        private readonly DebugLogger _logger;
        private TwoFactorLoginInfo _twoFactorInfo;  // Only used when login returns two factor
        private ChallengeLoginInfo _challengeInfo;  // Only used when login returns challenge

        public Instagram()
        {
#if DEBUG
            _logger = new DebugLogger(LogLevel.All);
#endif
            SetDefaultRequestHeaders();
            _apiRequestMessage = new ApiRequestMessage(this);

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            IsUserAuthenticated = (bool) localSettings.Values["_isUserAuthenticated"];
            PushClient = new PushClient(this, IsUserAuthenticated);
            SyncClient = new SyncClient(this);

            if (!IsUserAuthenticated) return;
            Session.LoadFromAppSettings();
            Device = AndroidDevice.CreateFromAppSettings() ?? Device;
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
            ValidateUser();
            ValidateRequestMessage();
            try
            {
                if (isNewLogin)
                {
                    var firstResponse = await GetAsync(UriCreator.BaseInstagramUri).ConfigureAwait(false);
                    _logger?.LogResponse(firstResponse);
                }

                var csrftoken = GetCsrfToken();
                Session.CsrfToken = csrftoken;
                var loginUri = UriCreator.GetLoginUri();
                var apiVersion = ApiVersion.CurrentApiVersion;
                var signature =
                    $"{_apiRequestMessage.GenerateChallengeSignature(apiVersion, apiVersion.SignatureKey, csrftoken, out var devid)}.{_apiRequestMessage.GetChallengeMessageString(csrftoken)}";
                Device.DeviceId = devid;
                var fields = new Dictionary<string, string>
                {
                    {"signed_body", signature},
                    {"ig_sig_key_version", "4"}
                };
                var httpContent = new HttpFormUrlEncodedContent(fields);
                httpContent.Headers.Add("Host", "i.instagram.com");
                var response = await _httpClient.PostAsync(loginUri, httpContent);
                var json = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.Ok)
                {
                    var loginFailReason = JsonConvert.DeserializeObject<LoginBaseResponse>(json);

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
                        _twoFactorInfo = loginFailReason.TwoFactorLoginInfo;
                        //2FA is required!
                        return Result<LoginResult>.Fail(LoginResult.TwoFactorRequired, "Two Factor Authentication is required", json);
                    }
                    if (loginFailReason.ErrorType == "checkpoint_challenge_required"
                       /* || !string.IsNullOrEmpty(loginFailReason.Message) && loginFailReason.Message == "challenge_required"*/)
                    {
                        _challengeInfo = loginFailReason.Challenge;

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
                IsUserAuthenticated = loginInfo.User != null;
                Session.Username = loginInfo.User.Username;
                Session.Password = password;
                Session.LoggedInUser = loginInfo.User;
                Session.RankToken = $"{loginInfo.User.Pk}_{_apiRequestMessage.PhoneId}";
                if (string.IsNullOrEmpty(Session.CsrfToken))
                {
                    Session.CsrfToken = GetCsrfToken();
                }
                return Result<LoginResult>.Success(LoginResult.Success, json: json);
            }
            catch (HttpRequestException httpException)
            {
                _logger?.LogException(httpException);
                return Result<LoginResult>.Except(httpException, LoginResult.Exception);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result<LoginResult>.Except(exception, LoginResult.Exception);
            }
        }


        /// <summary>
        /// No need to clear data. If IsUserAuthenticated is false, next time when constructor is called,
        /// data will not be loaded.
        /// </summary>
        public void Logout()
        {
            IsUserAuthenticated = false;
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
                var response = await PostAsync(instaUri, new HttpFormUrlEncodedContent(fields));
                var json = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.Ok)
                    return Result<CurrentUser>.Fail(json, response.ReasonPhrase);
                var user = JsonConvert.DeserializeObject<CurrentUser>(json);
                if (user.Pk < 1)
                    Result<CurrentUser>.Fail(json, "Pk is incorrect");
                return Result<CurrentUser>.Success(user, json);
            }
            catch (HttpRequestException httpException)
            {
                _logger?.LogException(httpException);
                return Result<CurrentUser>.Except(httpException);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result<CurrentUser>.Except(exception);
            }
        }

        public void SaveToAppSettings()
        {
            Device.SaveToAppSettings();
            Session.SaveToAppSettings();
            PushClient.ConnectionData.SaveToAppSettings();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["_isUserAuthenticated"] = IsUserAuthenticated;
        }

        // public StateData GetStateData()
        // {
        //     return new StateData()
        //     {
        //         Device = Device,
        //         IsAuthenticated = IsUserAuthenticated,
        //         Session = Session,
        //         FbnsConnectionData = PushClient.ConnectionData
        //     };
        // }
        //
        // public void LoadStateData(StateData stateData)
        // {
        //     Device = stateData.Device;
        //     IsUserAuthenticated = stateData.IsAuthenticated;
        //     Session = stateData.Session;
        //     PushClient.LoadState(stateData.FbnsConnectionData);
        // }
    }
}
