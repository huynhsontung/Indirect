using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using InstagramAPI.Classes.Core;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HttpMethod = Windows.Web.Http.HttpMethod;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
using HttpResponseMessage = Windows.Web.Http.HttpResponseMessage;

namespace InstagramAPI
{
    public partial class Instagram
    {
        private void SetDefaultRequestHeaders()
        {
            var defaultHeaders = _httpClient.DefaultRequestHeaders;
            defaultHeaders.Clear();
            defaultHeaders.UserAgent.TryParseAdd(Device.UserAgent);
            defaultHeaders.AcceptEncoding.TryParseAdd("gzip, deflate");
            defaultHeaders.AcceptLanguage.TryParseAdd("en-US");
            defaultHeaders.TryAdd("X-Ig-Android-Id", Device.DeviceId);
            defaultHeaders.TryAdd("X-Ig-App-Locale", "en_US");
            defaultHeaders.TryAdd("X-Ig-Timezone-Offset", ((int) DateTimeOffset.Now.Offset.TotalSeconds).ToString());
            defaultHeaders.TryAdd("X-Ig-Capabilities", ApiVersion.Current.Capabilities);
            defaultHeaders.TryAdd("X-Ig-Connection-Type", "WIFI");
            defaultHeaders.TryAdd("X-Ig-App-ID", "567067343352427");
            defaultHeaders.TryAdd("X-Fb-Http-Engine", "Liger");
        }

        public static string GetCsrfToken()
        {
            var baseFilter = new HttpBaseProtocolFilter();
            var cookieManager = baseFilter.CookieManager;
            var cookies = cookieManager.GetCookies(UriCreator.BaseInstagramUri);
            var csrfToken = cookies.SingleOrDefault(cookie => cookie.Name == "csrftoken");
            return csrfToken?.Value ?? string.Empty;
        }

        public async Task<HttpResponseMessage> GetAsync(Uri requestUri)
        {
            DebugLogger.LogRequest(requestUri);
            var response = await _httpClient.GetAsync(requestUri);
            DebugLogger.LogResponse(response);
            return response;
        }

        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, IHttpContent content)
        {
            DebugLogger.LogRequest(requestUri);
            var response = await _httpClient.PostAsync(requestUri, content);
            DebugLogger.LogResponse(response);
            return response;
        }

        public static HttpRequestMessage GetSignedRequest(Uri uri,
            JObject data)
        {
            var payload = data.ToString(Formatting.None);
            return GetSignedRequest(uri, payload);
        }

        public static HttpRequestMessage GetSignedRequest(Uri uri, string payload)
        {
            var signature = $"SIGNATURE.{payload}";
            var fields = new Dictionary<string, string>
            {
                {"signed_body", signature}
            };
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new HttpFormUrlEncodedContent(fields);
            request.Properties.Add("signed_body", signature);
            //request.Properties.Add("ig_sig_key_version", "4");
            return request;
        }
    }
}
