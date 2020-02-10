using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Indirect.Notification
{
    [Serializable]
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

        public string ClientId { get; set; } = Guid.NewGuid().ToString().Substring(0, 20);
        
        public string UserAgent { get; set; }
        public long ClientCapabilities { get; } = FBNS_CLIENT_CAPABILITIES;
        public long EndpointCapabilities { get; } = FBNS_ENDPOINT_CAPABILITIES;
        public int PublishFormat { get; } = FBNS_PUBLISH_FORMAT;
        public bool NoAutomaticForeground { get; } = FBNS_NO_AUTOMATIC_FOREGROUND;
        public bool MakeUserAvailableInForeground { get; } = FBNS_MAKE_USER_AVAILABLE_IN_FOREGROUND;
        public bool IsInitiallyForeground { get; } = FBNS_IS_INITIALLY_FOREGROUND;
        public int NetworkType { get; } = FBNS_NETWORK_TYPE;
        public int NetworkSubtype { get; } = FBNS_NETWORK_SUBTYPE;
        public long ClientMqttSessionId { get; set; }
        public int[] SubscribeTopics { get; } = FBNS_SUBSCRIBE_TOPICS;
        public string ClientType { get; } = FBNS_CLIENT_TYPE;
        public long AppId { get; } = FBNS_APP_ID;
        public sbyte ClientStack { get; } = FBNS_CLIENT_STACK;

        #region DeviceAuth

        public long UserId { get; private set; }
        public string Password { get; private set; }
        public string DeviceId { get; private set; }
        public string DeviceSecret { get; private set; }

        #endregion

        private string _fbnsToken;
        public string FbnsToken
        {
            get => _fbnsToken;
            set
            {
                _fbnsToken = value;
                FbnsTokenLastUpdated = DateTime.Now;
            }
        }
        public DateTime FbnsTokenLastUpdated { get; private set; }


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
    }
}
