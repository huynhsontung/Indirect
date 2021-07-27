using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using InstagramAPI.Classes.Core;
using InstagramAPI.Classes.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI.Utils
{
    public class HttpClientManager
    {
        public event EventHandler LoginRequired;

        public CookieCollection Cookies => GetCookies();

        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _handler;
        private readonly UserSessionData _session;

        public HttpClientManager(UserSessionData session)
        {
            _session = session;
            if (session.Cookies != null && session.Cookies.Count > 0)
            {
                var cookieContainer = new CookieContainer();
                cookieContainer.Add(session.Cookies);
                _handler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
            }
            else
            {
                _handler = new HttpClientHandler()
                    {AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate};
            }

            _httpClient = new HttpClient(_handler);
            SetDefaultRequestHeaders(session);
        }

        private void SetDefaultRequestHeaders(UserSessionData session)
        {
            var runtimeLanguages = Windows.Globalization.ApplicationLanguages.Languages;
            var primaryLanguage = Instagram.GetCurrentLocale() ?? "en_US";
            var defaultHeaders = _httpClient.DefaultRequestHeaders;
            defaultHeaders.ConnectionClose = true;
            defaultHeaders.AcceptEncoding.TryParseAdd("gzip, deflate");
            defaultHeaders.UserAgent.TryParseAdd(session.Device.UserAgent);
            defaultHeaders.Add("X-Ig-Device-Id", session.Device.PhoneId.ToString());
            defaultHeaders.Add("X-Ig-Android-Id", session.Device.DeviceId);
            //defaultHeaders.Add("X-Ig-Mapped-Locale", primaryLanguage);
            defaultHeaders.Add("X-Ig-Device-Locale", primaryLanguage);
            defaultHeaders.Add("X-Ig-App-Locale", primaryLanguage);
            defaultHeaders.Add("X-Ig-Timezone-Offset", ((int)DateTimeOffset.Now.Offset.TotalSeconds).ToString());
            defaultHeaders.Add("X-Ig-Capabilities", ApiVersion.Current.Capabilities);
            defaultHeaders.Add("X-Ig-Connection-Type", "WIFI");
            defaultHeaders.Add("X-Ig-App-ID", ApiVersion.AppId);
            defaultHeaders.Add("X-Fb-Http-Engine", "Liger");
            //defaultHeaders.Add("X-Fb-Client-Ip", "True");
            //defaultHeaders.Add("X-Fb-Server-Cluster", "True");
            //defaultHeaders.Add("X-Bloks-Version-Id", "927f06374b80864ae6a0b04757048065714dc50ff15d2b8b3de8d0b6de961649");
            //defaultHeaders.Add("X-Bloks-Is-Layout-Rtl", "false");
            //defaultHeaders.Add("X-Bloks-Is-Panorama-Enabled", "true");
            //defaultHeaders.Add("X-Pigeon-Session-Id", session.PigeonSessionId);
            defaultHeaders.Add("X-Ig-Www-Claim", session.WwwClaim);
            defaultHeaders.Add("Ig-Intended-User-Id", "0");

            foreach (var runtimeLanguage in runtimeLanguages)
            {
                defaultHeaders.AcceptLanguage.ParseAdd(runtimeLanguage);
            }

            var loggedInUser = session.LoggedInUser;
            if (loggedInUser != null && loggedInUser.Pk != default)
            {
                defaultHeaders.Remove("Ig-Intended-User-Id");
                defaultHeaders.Add("Ig-Intended-User-Id", loggedInUser.Pk.ToString());
            }

            var authorizationToken = session.AuthorizationToken;
            if (!string.IsNullOrEmpty(authorizationToken))
            {
                defaultHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AuthorizationToken.Substring(7));
            }

            var mid = session.Mid;
            if (!string.IsNullOrEmpty(mid))
            {
                defaultHeaders.Add("X-Mid", mid);
            }
        }

        public string GetCsrfToken()
        {
            var cookieContainer = _handler.CookieContainer;
            var cookies = cookieContainer.GetCookies(UriCreator.BaseInstagramUri);
            var csrfToken = cookies["csrftoken"];
            return csrfToken?.Value ?? string.Empty;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            DebugLogger.LogRequest(request);
            var response = await _httpClient.SendAsync(request);
            DebugLogger.LogResponse(response);
            UpdateHeaders(response);
            if (!response.IsSuccessStatusCode)
            {
                await CheckLoginRequired(response);
            }

            return response;
        }

        public async Task<HttpResponseMessage> GetAsync(Uri requestUri)
        {
            DebugLogger.LogRequest(requestUri);
            var response = await _httpClient.GetAsync(requestUri);
            DebugLogger.LogResponse(response);
            UpdateHeaders(response);
            if (!response.IsSuccessStatusCode)
            {
                await CheckLoginRequired(response);
            }

            return response;
        }

        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content)
        {
            DebugLogger.LogRequest(requestUri);
            var response = await _httpClient.PostAsync(requestUri, content);
            DebugLogger.LogResponse(response);
            UpdateHeaders(response);
            if (!response.IsSuccessStatusCode)
            {
                await CheckLoginRequired(response);
            }

            return response;
        }

        internal static HttpRequestMessage GetSignedRequest(Uri uri, JObject data)
        {
            var payload = data.ToString(Formatting.None);
            return GetSignedRequest(uri, payload);
        }

        internal static HttpRequestMessage GetSignedRequest(Uri uri, string payload)
        {
            var signature = $"SIGNATURE.{payload}";
            var fields = new Dictionary<string, string>
            {
                {"signed_body", signature}
            };
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new FormUrlEncodedContent(fields);
            request.Properties.Add("signed_body", signature);
            //request.Properties.Add("ig_sig_key_version", "4");
            return request;
        }

        internal async Task SyncServerConfig()
        {
            var dict = new Dictionary<string, string>(2)
            {
                ["id"] = _session.Device.Uuid.ToString(),
                ["server_config_retrieval"] = "1"
            };

            var payload = JsonConvert.SerializeObject(dict);
            var request = GetSignedRequest(UriCreator.GetLauncherSyncUri(), payload);
            await SendAsync(request);
        }

        private CookieCollection GetCookies()
        {
            var cookies = _handler.CookieContainer.GetCookies(UriCreator.BaseInstagramUri);
            var fbCookies = _handler.CookieContainer.GetCookies(new Uri("https://www.facebook.com/"));
            cookies.Add(fbCookies);
            return cookies;
        }

        private async Task CheckLoginRequired(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            var obj = JsonConvert.DeserializeObject<DefaultResponse>(json);
            if (obj?.Message == "challenge_required" || obj?.Message == "login_required")
            {
                LoginRequired?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UpdateHeaders(HttpResponseMessage response)
        {
            var session = _session;
            foreach (var header in response.Headers)
            {
                var key = header.Key.ToLower();
                switch (key)
                {
                    case "x-ig-set-www-claim":
                        lock (_httpClient)
                        {
                            var wwwClaim = session.WwwClaim = header.Value.FirstOrDefault();
                            _httpClient.DefaultRequestHeaders.Remove("X-Ig-Www-Claim");
                            _httpClient.DefaultRequestHeaders.Add("X-Ig-Www-Claim", wwwClaim);
                        }

                        break;

                    case "ig-set-x-mid":
                        lock (_httpClient)
                        {
                            var mid = session.Mid = header.Value.FirstOrDefault();
                            _httpClient.DefaultRequestHeaders.Remove("X-Mid");
                            _httpClient.DefaultRequestHeaders.Add("X-Mid", mid);
                        }

                        break;

                    case "ig-set-authorization":
                        lock (_httpClient)
                        {
                            var auth = session.AuthorizationToken = header.Value.FirstOrDefault();
                            if (!string.IsNullOrEmpty(auth))
                            {
                                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Substring(7)); ;
                            }
                        }

                        break;

                    case "ig-set-password-encryption-key-id":
                        var keyId = header.Value.FirstOrDefault();
                        if (!string.IsNullOrEmpty(keyId))
                        {
                            session.PasswordEncryptionKeyId = byte.Parse(keyId);
                        }

                        break;

                    case "ig-set-password-encryption-pub-key":
                        var pubkey = header.Value.FirstOrDefault();
                        if (!string.IsNullOrEmpty(pubkey))
                        {
                            session.PasswordEncryptionPubKey = CryptographicBuffer.DecodeFromBase64String(pubkey);
                        }

                        break;

                    default:
                        if (key.Contains("ig-set-ig-u-"))
                        {
                            key = key.Replace("ig-set-", "", StringComparison.OrdinalIgnoreCase);
                            lock (_httpClient)
                            {
                                _httpClient.DefaultRequestHeaders.Remove(key);
                                _httpClient.DefaultRequestHeaders.Add(key, header.Value);
                            }
                        }

                        break;
                }
            }
        }
    }
}
