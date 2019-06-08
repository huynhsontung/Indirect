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
    class ClientVerificationData
    {
        const long FBNS_CLIENT_CAPABILITIES = 439;
        const long FBNS_ENDPOINT_CAPABILITIES = 128;
        const long FBNS_APP_ID = 567310203415052;
        const sbyte FBNS_CLIENT_STACK = 3;
        const int FBNS_PUBLISH_FORMAT = 1;

        const int CLIENT_ID = 1;
        const int CLIENT_INFO = 4;
        const int PASSWORD = 5;

        const int USER_ID = 1;
        const int USER_AGENT = 2;
        const int CLIENT_CAPABILITIES = 3;
        const int ENDPOINT_CAPABILITIES = 4;
        const int PUBLISH_FORMAT = 5;
        const int NO_AUTOMATIC_FOREGROUND = 6;
        const int MAKE_USER_AVAILABLE_IN_FOREGROUND = 7;
        const int DEVICE_ID = 8;
        const int IS_INITIALLY_FOREGROUND = 9;
        const int NETWORK_TYPE = 10;
        const int NETWORK_SUBTYPE = 11;
        const int CLIENT_MQTT_SESSION_ID = 12;
        const int SUBSCRIBE_TOPICS = 14;
        const int CLIENT_TYPE = 15;
        const int APP_ID = 16;
        const int DEVICE_SECRET = 20;
        const int CLIENT_STACK = 21;

        public string ClientId { get; set; }

        #region ClientInfo
        public long UserId { get; set; }
        public string UserAgent { get; set; }
        public long ClientCapabilities { get; set; } = FBNS_CLIENT_CAPABILITIES;
        public long EndpointCapabilities { get; set; } = FBNS_ENDPOINT_CAPABILITIES;
        public int PublishFormat { get; set; }
        public bool NoAutomaticForeground { get; set; }
        public bool MakeUserAvailableInForeground { get; set; }
        public string DeviceId { get; set; }
        public bool IsInitiallyForeground { get; set; }
        public int NetworkType { get; set; }
        public int NetworkSubtype { get; set; }
        public long ClientMqttSessionId { get; set; }
        public int[] SubscribeTopics { get; set; }
        public string ClientType { get; set; }
        public long AppId { get; set; } = FBNS_APP_ID;
        public string DeviceSecret { get; set; }
        public sbyte ClientStack { get; set; } = FBNS_CLIENT_STACK;
        #endregion

        public string Password { get; set; }

        private TMemoryBufferTransport _memoryBufferTransport;
        private TCompactProtocol _thriftCompactProtocol;

        public ClientVerificationData()
        {
            _memoryBufferTransport = new TMemoryBufferTransport();
            _thriftCompactProtocol = new TCompactProtocol(_memoryBufferTransport);
        }
        

    }
}
