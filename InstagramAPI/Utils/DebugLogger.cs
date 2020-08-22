using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json;

namespace InstagramAPI.Utils
{
    public enum LogLevel
    {
        None = 0,
        Exceptions = 1,
        Info = 2,
        Request = 3,
        Response = 4,
        All = 5
    }

    public static class DebugLogger
    {
        public static LogLevel LogLevel { get; set; } = LogLevel.None;

        public static void Log(this object source, object message)
        {
            Debug.WriteLine($"[{DateTime.Now.ToString(CultureInfo.CurrentCulture)} - {source?.GetType().Name}]: {message}");
        }

        public static void Log(string type, object message)
        {
            Debug.WriteLine($"[{DateTime.Now.ToString(CultureInfo.CurrentCulture)} - {type}]: {message}");
        }

        public static void LogRequest(HttpRequestMessage request)
        {
            if (LogLevel < LogLevel.Request) return;
            WriteSeprator();
            Write($"Request: {request.Method} {request.RequestUri}");
            WriteHeaders(request.Headers);
            WriteProperties(request.Properties);
            if (request.Method == HttpMethod.Post)
                WriteRequestContent(request.Content);
        }

        public static void LogRequest(Uri uri)
        {
            if (LogLevel < LogLevel.Request) return;
            Write($"Request: {uri}");
        }

        public static void LogResponse(HttpResponseMessage response)
        {
            if (LogLevel < LogLevel.Response) return;
            Write($"Response: {response.RequestMessage.Method} {response.RequestMessage.RequestUri} [{response.StatusCode}]");
            WriteContent(response.Content, Formatting.None, 0);
        }

        public static void LogException(Exception ex, bool track = true)
        {
#if !DEBUG
            if (track) Crashes.TrackError(ex);
#endif
            if (LogLevel < LogLevel.Exceptions) return;
#if NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47 || NET471 || NET472 || NETSTANDARD1_0 || NETSTANDARD1_1 || NETSTANDARD1_2 || NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6 || NETSTANDARD2_0 || NETSTANDARD2_1 || NETSTANDARD2_2 || NETSTANDARD2_3
            Console.WriteLine($"Exception: {ex}");
            Console.WriteLine($"Stacktrace: {ex.StackTrace}");
#else
            Log("Exception", ex);
            Debug.WriteLine($"Stacktrace: {ex.StackTrace}");
#endif
        }

        public static void LogInfo(string info)
        {
            if (LogLevel < LogLevel.Info) return;
            Write($"Info:{Environment.NewLine}{info}");
        }

        private static void WriteHeaders(HttpRequestHeaderCollection headers)
        {
            if (headers == null) return;
            if (!headers.Any()) return;
            Write("Headers:");
            foreach (var item in headers)
                Write($"{item.Key}:{JsonConvert.SerializeObject(item.Value)}");
        }

        private static void WriteProperties(IDictionary<string, object> properties)
        {
            if (properties == null) return;
            if (properties.Count == 0) return;
            Write($"Properties:\n{JsonConvert.SerializeObject(properties, Formatting.Indented)}");
        }

        private static async void WriteContent(IHttpContent content, Formatting formatting, int maxLength = 0)
        {
            Write("Content:");
            var raw = await content.ReadAsStringAsync();
            if (formatting == Formatting.Indented) raw = FormatJson(raw);
            raw = raw.Contains("<!DOCTYPE html>") ? "got html content!" : raw;
            if ((raw.Length > maxLength) & (maxLength != 0))
                raw = raw.Substring(0, maxLength);
            Write(raw);
        }
        private static async void WriteRequestContent(IHttpContent content, int maxLength = 0)
        {
            Write("Content:");
            var raw = await content.ReadAsStringAsync();
            if ((raw.Length > maxLength) & (maxLength != 0))
                raw = raw.Substring(0, maxLength);
            Write(WebUtility.UrlDecode(raw));
        }

        private static void WriteSeprator()
        {
            var sep = new StringBuilder();
            for (var i = 0; i < 100; i++) sep.Append("-");
            Write(sep.ToString());
        }

        private static string FormatJson(string json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

        private static void Write(string message)
        {
            Debug.WriteLine($"[{DateTime.Now.ToString(CultureInfo.CurrentCulture)}]:\t{message}");
        }
    }
}