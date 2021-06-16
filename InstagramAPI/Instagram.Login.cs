using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Web.Http;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Core;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Classes.User;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI
{
    public partial class Instagram
    {
        /// <summary>
        ///     Login using given credentials asynchronously
        /// </summary>
        /// <param name="session"></param>
        /// <returns>
        ///     Success --> is succeed
        ///     TwoFactorRequired --> requires 2FA login.
        ///     BadPassword --> Password is wrong
        ///     InvalidUser --> User/phone number is wrong
        ///     Exception --> Something wrong happened
        ///     ChallengeRequired --> You need to pass Instagram challenge
        /// </returns>
        public static async Task<Result<LoginResult>> LoginAsync(UserSessionData session)
        {
            if (session.IsAuthenticated)
            {
                return Result<LoginResult>.Success(LoginResult.Success);
            }

            try
            {
                var httpClient = new HttpClient();
                SetDefaultRequestHeaders(httpClient, session);
                var firstResponse = await httpClient.GetAsync(UriCreator.BaseInstagramUri);
                DebugLogger.LogResponse(firstResponse);

                var loginUri = UriCreator.GetLoginUri();
                var signature =
                    $"SIGNATURE.{ApiRequestMessage.GetChallengeMessageString(session)}";
                var fields = new Dictionary<string, string>
                {
                    {"signed_body", signature}
                };
                var request = new HttpRequestMessage(HttpMethod.Post, loginUri);
                request.Headers.Host = new HostName("i.instagram.com");
                request.Content = new HttpFormUrlEncodedContent(fields);
                var response = await httpClient.SendRequestAsync(request);
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
                            session.Username = loginFailReason.TwoFactorLoginInfo.Username;
                        session.TwoFactorInfo = loginFailReason.TwoFactorLoginInfo;
                        //2FA is required!
                        return Result<LoginResult>.Fail(LoginResult.TwoFactorRequired, "Two Factor Authentication is required", json);
                    }
                    if (loginFailReason.ErrorType == "checkpoint_challenge_required"
                       /* || !string.IsNullOrEmpty(loginFailReason.Message) && loginFailReason.Message == "challenge_required"*/)
                    {
                        session.ChallengeInfo = loginFailReason.Challenge;

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

                session.AuthorizationToken = GetAuthToken(response.Headers);
                session.Username = loginInfo.User.Username;
                session.LoggedInUser = loginInfo.User;
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
        /// <param name="session"></param>
        /// <returns>
        ///     Success --> is succeed
        ///     TwoFactorRequired --> requires 2FA login.
        ///     BadPassword --> Password is wrong
        ///     InvalidUser --> User/phone number is wrong
        ///     Exception --> Something wrong happened
        ///     ChallengeRequired --> You need to pass Instagram challenge
        /// </returns>
        public static async Task<Result<LoginResult>> LoginWithFacebookAsync(string fbAccessToken, UserSessionData session)
        {
            if (session.IsAuthenticated)
            {
                return Result<LoginResult>.Success(LoginResult.Success);
            }

            try
            {
                var httpClient = new HttpClient();
                SetDefaultRequestHeaders(httpClient, session);
                if (string.IsNullOrEmpty(fbAccessToken)) throw new ArgumentNullException(nameof(fbAccessToken));
                if (GetCsrfToken() == string.Empty)
                {
                    var firstResponse = await httpClient.GetAsync(UriCreator.BaseInstagramUri);
                    DebugLogger.LogResponse(firstResponse);
                }

                var instaUri = UriCreator.GetFacebookSignUpUri();

                var data = new JObject
                {
                    {"dryrun", "true"},
                    {"phone_id", session.Device.PhoneId.ToString()},
                    {"_csrftoken", GetCsrfToken()},
                    {"adid", Guid.NewGuid().ToString()},
                    {"guid",  session.Device.Uuid.ToString()},
                    {"_uuid",  session.Device.Uuid.ToString()},
                    {"device_id", session.Device.DeviceId},
                    {"waterfall_id", Guid.NewGuid().ToString()},
                    {"fb_access_token", fbAccessToken},
                };

                session.FacebookAccessToken = fbAccessToken;
                var request = GetSignedRequest(instaUri, data);
                var response = await httpClient.SendRequestAsync(request);
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
                            session.Username = loginFailReason.TwoFactorLoginInfo.Username;
                        session.TwoFactorInfo = loginFailReason.TwoFactorLoginInfo;
                        return Result<LoginResult>.Fail(LoginResult.TwoFactorRequired, "Two Factor Authentication is required", json);
                    }
                    if (loginFailReason.ErrorType == "checkpoint_challenge_required")
                    {
                        session.ChallengeInfo = loginFailReason.Challenge;

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

                session.AuthorizationToken = GetAuthToken(response.Headers);
                session.LoggedInUser = loginInfoUser;
                session.FacebookUserId = fbUserId;
                session.Username = loginInfoUser.Username;
                session.Password = "LOGGED_IN_THROUGH_FB";
                return Result<LoginResult>.Success(LoginResult.Success, json);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception);
                return Result<LoginResult>.Except(exception, LoginResult.Exception);
            }
        }

        public static async Task<Result<LoginResult>> LoginWithTwoFactorAsync(string verificationCode, UserSessionData session)
        {
            if (session.TwoFactorInfo == null)
                return Result<LoginResult>.Except(new ArgumentNullException(nameof(session.TwoFactorInfo),
                    "Cannot login with two factor before logging in normally"));

            try
            {
                var httpClient = new HttpClient();
                SetDefaultRequestHeaders(httpClient, session);
                var twoFactorData = new JObject
                {
                    {"verification_code", verificationCode},
                    {"username", session.Username},
                    {"device_id", session.Device.DeviceId},
                    {"two_factor_identifier", session.TwoFactorInfo.TwoFactorIdentifier}
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
                var response = await httpClient.SendRequestAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                DebugLogger.LogResponse(response);

                if (!response.IsSuccessStatusCode)
                {
                    var loginFailReason = JsonConvert.DeserializeObject<LoginFailedResponse>(json);

                    if (loginFailReason.ErrorType == "sms_code_validation_code_invalid")
                        return Result<LoginResult>.Fail(LoginResult.InvalidCode, "Please check the security code.", json);
                    if (loginFailReason.Challenge != null)
                    {
                        session.ChallengeInfo = loginFailReason.Challenge;
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

                session.AuthorizationToken = GetAuthToken(response.Headers);
                session.Username = loginInfo.User.Username;
                session.LoggedInUser = loginInfo.User;
                session.TwoFactorInfo = null;
                return Result<LoginResult>.Success(LoginResult.Success);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception);
                return Result<LoginResult>.Except(exception, LoginResult.Exception);
            }
        }
    }
}