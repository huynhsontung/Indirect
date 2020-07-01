using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Android;
using InstagramAPI.Push;
using Ionic.Zlib;
using Thrift;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport.Client;
using Windows.Networking.NetworkOperators;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using CompressionLevel = Ionic.Zlib.CompressionLevel;
using CompressionMode = Ionic.Zlib.CompressionMode;
namespace InstagramAPI.Sync
{
    public static class RealtimePayload
    {
        private const short CLIENT_ID = 1;
        private const short CLIENT_INFO = 4;
        private const short PASSWORD = 5;

        private const short USER_ID = 1;
        private const short USER_AGENT = 2;
        private const short CLIENT_CAPABILITIES = 3;
        private const short ENDPOINT_CAPABILITIES = 4;
        private const short PUBLISH_FORMAT = 5;
        private const short NO_AUTOMATIC_FOREGROUND = 6;
        private const short MAKE_USER_AVAILABLE_IN_FOREGROUND = 7;
        private const short DEVICE_ID = 8;
        private const short IS_INITIALLY_FOREGROUND = 9;
        private const short NETWORK_TYPE = 10;
        private const short NETWORK_SUBTYPE = 11;
        private const short CLIENT_MQTT_SESSION_ID = 12;
        private const short SUBSCRIBE_TOPICS = 14;
        private const short CLIENT_TYPE = 15;
        private const short APP_ID = 16;
        private const short DEVICE_SECRET = 20;
        private const short CLIENT_STACK = 21;

        private static TMemoryBufferTransport _memoryBufferTransport; // doesn't need manual disposal
        private static TCompactProtocol _thrift;
        private static FbnsConnectionData _payloadData = new FbnsConnectionData();
        private static Instagram Instagram;
        public static async Task<IBuffer> BuildPayload(Instagram instagram)
        {
            Instagram = instagram;
            _memoryBufferTransport = new TMemoryBufferTransport();
            _thrift = new TCompactProtocol(_memoryBufferTransport);
            _payloadData = new FbnsConnectionData(); // since we don't need payload data, just ignore it

            var rawPayload = await ToThrift();
            var dataStream = new MemoryStream(512);
            using (var zlibStream = new ZlibStream(dataStream, CompressionMode.Compress, CompressionLevel.Level9, true))
                await zlibStream.WriteAsync(rawPayload, 0, rawPayload.Length);

            var compressed = dataStream.GetWindowsRuntimeBuffer(0, (int)dataStream.Length);
            return compressed;
        }
        private static async Task<byte[]> ToThrift()
        {
            var device = Instagram.Device;
            var baseHttpFilter = new HttpBaseProtocolFilter();
            var cookies = baseHttpFilter.CookieManager.GetCookies(new Uri("https://i.instagram.com"));
            var sessionId = cookies.FirstOrDefault(xx => xx.Name == "sessionid");

            await WriteString(CLIENT_ID, device.Uuid.ToString().Substring(0, 20));

            #region Write struct ClientInfo
            await WriteStructBegin(CLIENT_INFO);
            await WriteInt64(USER_ID, Instagram.Session.LoggedInUser.Pk);
            await WriteString(USER_AGENT, FbnsUserAgent.BuildFbUserAgent(device));
            await WriteInt64(CLIENT_CAPABILITIES, 183);
            await WriteInt64(ENDPOINT_CAPABILITIES, _payloadData.EndpointCapabilities);
            await WriteInt32(PUBLISH_FORMAT, _payloadData.PublishFormat);
            await WriteBool(NO_AUTOMATIC_FOREGROUND, _payloadData.NoAutomaticForeground);
            await WriteBool(MAKE_USER_AVAILABLE_IN_FOREGROUND, _payloadData.MakeUserAvailableInForeground);
            await WriteString(DEVICE_ID, device.Uuid.ToString());
            await WriteBool(IS_INITIALLY_FOREGROUND, _payloadData.IsInitiallyForeground);
            await WriteInt32(NETWORK_TYPE, _payloadData.NetworkType);
            await WriteInt32(NETWORK_SUBTYPE, _payloadData.NetworkSubtype);
            if (_payloadData.ClientMqttSessionId == 0)
            {
                var difference = DateTime.Today.DayOfWeek - DayOfWeek.Monday;
                var lastMonday = new DateTimeOffset(DateTime.Today.Subtract(TimeSpan.FromDays(difference > 0 ? difference : 7)));
                _payloadData.ClientMqttSessionId = DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastMonday.ToUnixTimeMilliseconds();
            }

            await WriteInt64(CLIENT_MQTT_SESSION_ID, _payloadData.ClientMqttSessionId);
            await WriteListInt32(SUBSCRIBE_TOPICS, new int[] { 88, 135, 149, 150, 133, 146 });
            await WriteString(CLIENT_TYPE, "cookie_auth");
            await WriteInt64(APP_ID, 567067343352427);
            await WriteString(DEVICE_SECRET, "");
            await WriteByte(CLIENT_STACK, _payloadData.ClientStack);
            await WriteFieldStop();
            await WriteStructEnd();
            #endregion

            await WriteString(PASSWORD, $"sessionid={sessionId}");
            await WriteUsername();
            await WriteFieldStop();
            return _memoryBufferTransport.GetBuffer();
        }

        private static async Task WriteString(short id, string str)
        {
            if (str == null) str = "";
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.String, id), System.Threading.CancellationToken.None);
            await _thrift.WriteStringAsync(str, System.Threading.CancellationToken.None);
        }

        private static async Task WriteStructBegin(short id)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.Struct, id), System.Threading.CancellationToken.None);
            /*
             * From Thrift source code:
             * Write a struct begin. This doesn't actually put anything on the wire. We
             * use it as an opportunity to put special placeholder markers on the field
             * stack so we can get the field id deltas correct.
             */
            await _thrift.WriteStructBeginAsync(new TStruct(), System.Threading.CancellationToken.None);
        }

        private static async Task WriteStructEnd()
        {
            await _thrift.WriteStructEndAsync(System.Threading.CancellationToken.None);
        }

        private static async Task WriteInt64(short id, long value)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.I64, id), System.Threading.CancellationToken.None);
            await _thrift.WriteI64Async(value, System.Threading.CancellationToken.None);
        }

        private static async Task WriteInt32(short id, int value)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.I32, id), System.Threading.CancellationToken.None);
            await _thrift.WriteI32Async(value, System.Threading.CancellationToken.None);
        }

        private static async Task WriteByte(short id, sbyte value)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.Byte, id), System.Threading.CancellationToken.None);
            await _thrift.WriteByteAsync(value, System.Threading.CancellationToken.None);
        }

        private static async Task WriteBool(short id, bool value)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.Bool, id), System.Threading.CancellationToken.None);
            await _thrift.WriteBoolAsync(value, System.Threading.CancellationToken.None);
        }

        private static async Task WriteListInt32(short id, int[] values)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.List, id), System.Threading.CancellationToken.None);
            await _thrift.WriteListBeginAsync(new TList(TType.I32, values.Length), System.Threading.CancellationToken.None);
            foreach (var value in values)
            {
                await _thrift.WriteI32Async(value, System.Threading.CancellationToken.None);
            }
        }

        private static async Task WriteUsername()
        {
            var apiVersion = ApiVersion.CurrentApiVersion;
            var dic = new Dictionary<string, string>
            {
                {"app_version", apiVersion.AppVersion},
                {"X-IG-Capabilities", apiVersion.Capabilities},
                {"everclear_subscriptions", "{\"inapp_notification_subscribe_comment\":\"17899377895239777\",\"inapp_notification_subscribe_comment_mention_and_reply\":\"17899377895239777\",\"video_call_participant_state_delivery\":\"17977239895057311\",\"presence_subscribe\":\"17846944882223835\"}"},
                {"User-Agent", FbnsUserAgent.BuildFbUserAgent(Instagram.Device)},
                {"Accept-Language", "en_US"},
                {"platform", "android"},
                {"ig_mqtt_route", "django"},
                //{"pubsub_msg_type_blacklist","direct, typing_type"}, // remove this if you want to get direct items as well
                {"auth_cache_enabled", "0"},
            };
            await WriteMap(10, dic);
        }

        private static async Task WriteMap(short id, Dictionary<string, string> dic)
        {
            await _thrift.WriteFieldBeginAsync(new TField(null, TType.Map, id), System.Threading.CancellationToken.None);
            await _thrift.WriteMapBeginAsync(new TMap(TType.String, TType.String, dic.Count), System.Threading.CancellationToken.None);
            foreach (var item in dic)
            {
                await _thrift.WriteStringAsync(item.Key, System.Threading.CancellationToken.None);
                await _thrift.WriteStringAsync(item.Value, System.Threading.CancellationToken.None);
            }
            await _thrift.WriteMapEndAsync(System.Threading.CancellationToken.None);
            await _thrift.WriteFieldEndAsync(System.Threading.CancellationToken.None);
        }
        private static async Task WriteFieldStop()
        {
            await _thrift.WriteFieldStopAsync(System.Threading.CancellationToken.None);
        }
    }
}
