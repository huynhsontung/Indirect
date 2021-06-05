using System;
using System.Collections.Generic;
using Windows.Foundation.Metadata;
using Newtonsoft.Json;

namespace InstagramAPI.Push
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class FbnsConnectionData
    {
        private const int MESSAGE_TOPIC_ID = 76;
        private const int REG_RESP_TOPIC_ID = 80;

        private const long FBNS_CLIENT_CAPABILITIES = 439;
        private const long FBNS_ENDPOINT_CAPABILITIES = 128;
        private const long FBNS_APP_ID = 567310203415052;
        private const sbyte FBNS_CLIENT_STACK = 3;
        private const int FBNS_PUBLISH_FORMAT = 1;
        private const int FBNS_NETWORK_TYPE = 1;
        private const int FBNS_NETWORK_SUBTYPE = 0;
        private const bool FBNS_NO_AUTOMATIC_FOREGROUND = true;
        private const bool FBNS_MAKE_USER_AVAILABLE_IN_FOREGROUND = false;
        private const bool FBNS_IS_INITIALLY_FOREGROUND = false;
        private const string FBNS_CLIENT_TYPE = "device_auth";
        private static readonly int[] FBNS_SUBSCRIBE_TOPICS = {MESSAGE_TOPIC_ID, REG_RESP_TOPIC_ID};

        [JsonProperty]
        public string ClientId { get; set; } = Guid.NewGuid().ToString().Substring(0, 20);
        
        [JsonProperty]
        public string UserAgent { get; set; }

        [JsonProperty]
        public long ClientMqttSessionId { get; set; }

        public long ClientCapabilities => FBNS_CLIENT_CAPABILITIES;
        public long EndpointCapabilities => FBNS_ENDPOINT_CAPABILITIES;
        public int PublishFormat => FBNS_PUBLISH_FORMAT;
        public bool NoAutomaticForeground => FBNS_NO_AUTOMATIC_FOREGROUND;
        public bool MakeUserAvailableInForeground => FBNS_MAKE_USER_AVAILABLE_IN_FOREGROUND;
        public bool IsInitiallyForeground => FBNS_IS_INITIALLY_FOREGROUND;
        public int NetworkType => FBNS_NETWORK_TYPE;
        public int NetworkSubtype => FBNS_NETWORK_SUBTYPE;
        public int[] SubscribeTopics => FBNS_SUBSCRIBE_TOPICS;
        public string ClientType => FBNS_CLIENT_TYPE;
        public long AppId => FBNS_APP_ID;
        public sbyte ClientStack => FBNS_CLIENT_STACK;

        #region DeviceAuth

        [JsonProperty]
        public long UserId { get; private set; }

        [JsonProperty]
        public string Password { get; private set; }

        [JsonProperty]
        public string DeviceId { get; private set; }

        [JsonProperty]
        public string DeviceSecret { get; private set; }

        //[JsonProperty]
        //public string FbnsToken { get; internal set; }

        //[JsonProperty]
        //public DateTimeOffset FbnsTokenLastUpdated { get; internal set; }

        #endregion


        public void UpdateAuth(string json)
        {
            if(string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));

            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            var ck = data["ck"];
            var cs = data["cs"];
            var di = data["di"];
            var ds = data["ds"];

            if (!string.IsNullOrEmpty(ck))
                UserId = long.Parse(ck);

            if (!string.IsNullOrEmpty(cs))
                Password = cs;

            if (!string.IsNullOrEmpty(di))
            {
                DeviceId = di;
                ClientId = di.Substring(0, 20);
            }

            if (!string.IsNullOrEmpty(ds))
                DeviceSecret = ds;

            // TODO: sr, rc ?
        }

        public void LoadFromAppSettings()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var composite = (Windows.Storage.ApplicationDataCompositeValue)localSettings.Values["_fbnsConnectionData"];
            if (composite == null) return;
            ClientId = (string) composite["ClientId"];
            UserAgent = (string) composite["UserAgent"];
            ClientMqttSessionId = (long) composite["ClientMqttSessionId"];
            UserId = (long) composite["UserId"];
            Password = (string) composite["Password"];
            DeviceId = (string) composite["DeviceId"];
            DeviceSecret = (string) composite["DeviceSecret"];
            //FbnsToken = (string) composite["_fbnsToken"];
            //FbnsTokenLastUpdated = (DateTimeOffset) composite["FbnsTokenLastUpdated"];
        }

        public static void RemoveFromAppSettings()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values.Remove("_fbnsConnectionData");
        }
    }
}
