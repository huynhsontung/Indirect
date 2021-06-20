using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using InstagramAPI.Classes.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI.Utils
{
    public class HttpClientManager
    {
        public CookieCollection Cookies => GetCookies();

        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _handler;

        public HttpClientManager(UserSessionData session)
        {
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
            var defaultHeaders = _httpClient.DefaultRequestHeaders;
            defaultHeaders.AcceptEncoding.TryParseAdd("gzip, deflate");
            defaultHeaders.UserAgent.TryParseAdd(session.Device.UserAgent);
            defaultHeaders.Add("X-Ig-Android-Id", session.Device.DeviceId);
            defaultHeaders.Add("X-Ig-App-Locale", "en_US");
            defaultHeaders.Add("X-Ig-Timezone-Offset", ((int)DateTimeOffset.Now.Offset.TotalSeconds).ToString());
            defaultHeaders.Add("X-Ig-Capabilities", ApiVersion.Current.Capabilities);
            defaultHeaders.Add("X-Ig-Connection-Type", "WIFI");
            defaultHeaders.Add("X-Ig-App-ID", ApiVersion.AppId);
            defaultHeaders.Add("X-Fb-Http-Engine", "Liger");

            var loggedInUser = session.LoggedInUser;
            if (loggedInUser != null && loggedInUser.Pk != default)
            {
                defaultHeaders.Add("Ig-Intended-User-Id", loggedInUser.Pk.ToString());
                defaultHeaders.Add("Ig-U-Ds-User-Id", loggedInUser.Pk.ToString());
            }

            var authorizationToken = session.AuthorizationToken;
            if (!string.IsNullOrEmpty(authorizationToken))
            {
                defaultHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AuthorizationToken.Substring(7));
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
            return response;
        }

        public async Task<HttpResponseMessage> GetAsync(Uri requestUri)
        {
            DebugLogger.LogRequest(requestUri);
            var response = await _httpClient.GetAsync(requestUri);
            DebugLogger.LogResponse(response);
            return response;
        }

        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content)
        {
            DebugLogger.LogRequest(requestUri);
            var response = await _httpClient.PostAsync(requestUri, content);
            DebugLogger.LogResponse(response);
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

        internal static string GetAuthToken(HttpResponseHeaders headers)
        {
            headers.TryGetValues("Ig-Set-Authorization", out var tokens);
            return tokens.FirstOrDefault();
        }

        private CookieCollection GetCookies()
        {
            var cookies = _handler.CookieContainer.GetCookies(UriCreator.BaseInstagramUri);
            var fbCookies = _handler.CookieContainer.GetCookies(new Uri("https://www.facebook.com/"));
            cookies.Add(fbCookies);
            return cookies;
        }
    }
}
