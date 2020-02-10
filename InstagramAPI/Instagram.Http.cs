using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;
using InstagramAPI.Utils;
using HttpResponseMessage = Windows.Web.Http.HttpResponseMessage;

namespace InstagramAPI
{
    public partial class Instagram
    {
        private void SetDefaultRequestHeaders()
        {
            var defaultHeaders = _httpClient.DefaultRequestHeaders;
            defaultHeaders.Connection.ParseAdd("Keep-Alive");
            defaultHeaders.UserAgent.ParseAdd(Device.UserAgent);
            defaultHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
            defaultHeaders.Accept.ParseAdd("*/*");
            defaultHeaders.AcceptLanguage.ParseAdd("en-US");
            defaultHeaders.Add("X-IG-Capabilities", "3brTBw==");
            defaultHeaders.Add("X-IG-Connection-Type", "WIFI");
            defaultHeaders.Add("X-IG-App-ID", "567067343352427");
            defaultHeaders.Add("X-FB-HTTP-Engine", "Liger");
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
