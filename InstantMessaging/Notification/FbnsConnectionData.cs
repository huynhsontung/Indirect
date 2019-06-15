using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport.Client;

namespace InstantMessaging.Notification
{
    [Serializable]
    class FbnsConnectionData    // todo: connection data needs to be saved on disk
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
        private static readonly int[] FBNS_SUBSCRIBE_TOPICS = {MESSAGE_TOPIC_ID, REG_RESP_TOPIC_ID};

        public string ClientId { get; set; } = Guid.NewGuid().ToString().Substring(0, 20);

        #region ClientInfo Struct
        public long UserId { get; set; } = 0;
        public string UserAgent { get; set; }
        public long ClientCapabilities { get; set; } = FBNS_CLIENT_CAPABILITIES;
        public long EndpointCapabilities { get; set; } = FBNS_ENDPOINT_CAPABILITIES;
        public int PublishFormat { get; set; } = FBNS_PUBLISH_FORMAT;
        public bool NoAutomaticForeground { get; set; } = FBNS_NO_AUTOMATIC_FOREGROUND;
        public bool MakeUserAvailableInForeground { get; set; } = FBNS_MAKE_USER_AVAILABLE_IN_FOREGROUND;
        public string DeviceId { get; set; }
        public bool IsInitiallyForeground { get; set; } = FBNS_IS_INITIALLY_FOREGROUND;
        public int NetworkType { get; set; } = FBNS_NETWORK_TYPE;
        public int NetworkSubtype { get; set; } = FBNS_NETWORK_SUBTYPE;
        public long ClientMqttSessionId { get; set; }
        public int[] SubscribeTopics { get; set; } = FBNS_SUBSCRIBE_TOPICS;
        public string ClientType { get; set; }
        public long AppId { get; set; } = FBNS_APP_ID;
        public string DeviceSecret { get; set; }
        public sbyte ClientStack { get; set; } = FBNS_CLIENT_STACK;
        #endregion

        public string Password { get; set; }

        public void UpdateAuth(string json)
        {
            // todo: implement read from json
        }
    }
}
