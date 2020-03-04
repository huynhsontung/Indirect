using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Android;
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
            defaultHeaders.Connection.TryParseAdd("Keep-Alive");
            defaultHeaders.UserAgent.TryParseAdd(Device.UserAgent);
            defaultHeaders.AcceptEncoding.TryParseAdd("gzip, deflate");
            defaultHeaders.Accept.TryParseAdd("*/*");
            defaultHeaders.AcceptLanguage.TryParseAdd("en-US");
            defaultHeaders.TryAdd("X-IG-Capabilities", "3brTBw==");
            defaultHeaders.TryAdd("X-IG-Connection-Type", "WIFI");
            defaultHeaders.TryAdd("X-IG-App-ID", "567067343352427");
            defaultHeaders.TryAdd("X-FB-HTTP-Engine", "Liger");
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
            _logger?.LogRequest(requestUri);
            var response = await _httpClient.GetAsync(requestUri);
            _logger?.LogResponse(response);
            return response;
        }

        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, IHttpContent content)
        {
            _logger?.LogRequest(requestUri);
            var response = await _httpClient.PostAsync(requestUri, content);
            _logger?.LogResponse(response);
            return response;
        }

        public static HttpRequestMessage GetSignedRequest(Uri uri,
            JObject data)
        {
            var hash = CryptoHelper.CalculateHash(ApiVersion.CurrentApiVersion.SignatureKey,
                data.ToString(Formatting.None));
            var payload = data.ToString(Formatting.None);
            return GetSignedRequest(uri, hash, payload);
        }

        public static HttpRequestMessage GetSignedRequest(Uri uri, string hash, string payload)
        {
            var signature = $"{hash}.{payload}";
            var fields = new Dictionary<string, string>
            {
                {"signed_body", signature},
                {"ig_sig_key_version", "4"}
            };
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new HttpFormUrlEncodedContent(fields);
            request.Properties.Add("signed_body", signature);
            request.Properties.Add("ig_sig_key_version", "4");
            return request;
        }

        // private static async Task<IHttpContent> DecompressHttpContent(IHttpContent content)
        // {
        //     var encoding = content.Headers.ContentEncoding;
        //     var isGzip = encoding.Contains(new HttpContentCodingHeaderValue("gzip"));
        //     var isDeflate = encoding.Contains(new HttpContentCodingHeaderValue("deflate"));
        //     if (!isGzip && !isDeflate && encoding.Count != 0)
        //     {
        //         throw new ArgumentException("DecompressHttpContent: Compression type not supported.");
        //     }
        //
        //     if (encoding.Count == 0)
        //     {
        //         return content;
        //     }
        //
        //     var decompressed = new InMemoryRandomAccessStream();
        //     var data = await content.ReadAsInputStreamAsync();
        //     if (isDeflate)
        //     {
        //         using (var deflateStream = new DeflateStream(data.AsStreamForRead(), CompressionMode.Decompress))
        //         {
        //             await deflateStream.CopyToAsync(decompressed.AsStreamForWrite()).ConfigureAwait(false);
        //         }
        //     }
        //     else if (isGzip)
        //     {
        //         using (var gzipStream = new GZipStream(data.AsStreamForRead(), CompressionMode.Decompress))
        //         {
        //             await gzipStream.CopyToAsync(decompressed.AsStreamForWrite()).ConfigureAwait(false);
        //         }
        //     }
        //
        //     decompressed.Seek(0);
        //     var newContent = new HttpStreamContent(decompressed);
        //     newContent.Headers.ContentType = content.Headers.ContentType;
        //     newContent.Headers.ContentLanguage.ParseAdd(content.Headers.ContentLanguage.ToString());
        //     newContent.Headers.ContentLength = decompressed.Size;
        //     return newContent;
        // }
    }
}
