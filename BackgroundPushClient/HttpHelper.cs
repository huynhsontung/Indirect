using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using InstaSharper.API;
using InstaSharper.Classes;
using InstaSharper.Classes.DeviceInfo;

namespace BackgroundPushClient
{
    internal class HttpHelper
    {
        const string ACCEPT_LANGUAGE = "en-US";
        const string IG_APP_ID = "567067343352427";

        public static HttpRequestMessage GetDefaultRequest(HttpMethod method, Uri uri, AndroidDevice deviceInfo)
        {
            var request = new HttpRequestMessage(method, uri);
            request.Headers.Connection.ParseAdd("Keep-Alive");
            request.Headers.UserAgent.ParseAdd(deviceInfo.UserAgent);
            request.Headers.AcceptEncoding.ParseAdd(HttpRequestProcessor.ACCEPT_ENCODING);
            request.Headers.Accept.ParseAdd("*/*");
            request.Headers.AcceptLanguage.ParseAdd(ACCEPT_LANGUAGE);
            request.Headers.Add("X-IG-Capabilities", "3brTBw==");
            request.Headers.Add("X-IG-Connection-Type", "WIFI");
            request.Headers.Add("X-IG-App-ID", IG_APP_ID);
            request.Headers.Add("X-FB-HTTP-Engine", "Liger");
            request.Properties.Add(new KeyValuePair<string, object>("X-Google-AD-ID",
                deviceInfo.GoogleAdId.ToString()));
            return request;
        }
    }
}
