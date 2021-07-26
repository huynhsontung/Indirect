using System;
using System.Collections.Generic;
using System.Linq;
using InstagramAPI.Classes.Android;
using InstagramAPI.Fbns;
using Newtonsoft.Json;

namespace InstagramAPI.Push
{
    public sealed class PushConnectionData : BaseConnectionData
    {
        private const int MessageTopicId = 76;
        private const int RegRespTopicId = 80;

        private const long FbnsClientCapabilities = 183;
        private const long FbnsEndpointCapabilities = 128;
        private const long FbnsAppId = 567310203415052;
        private const sbyte FbnsClientStack = 3;
        private const int FbnsPublishFormat = 1;
        private const int FbnsNetworkType = 1;
        private const int FbnsNetworkSubtype = 0;
        private const bool FbnsNoAutomaticForeground = true;
        private const bool FbnsMakeUserAvailableInForeground = false;
        private const bool FbnsIsInitiallyForeground = false;
        private const string FbnsClientType = "device_auth";
        private static readonly int[] FbnsSubscribeTopics = { MessageTopicId, RegRespTopicId };

        public PushConnectionData(AndroidDevice device)
        {
            UserAgent = PushUserAgent.BuildFbUserAgent(device);
        }

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

        public override ConnectPayload ToPayload()
        {
            return new ConnectPayload
            {
                ClientId = ClientId,
                Password = Password,
                ClientInfo = new ClientInfo
                {
                    UserId = UserId,
                    UserAgent = UserAgent,
                    ClientMqttSessionId = ClientMqttSessionId,
                    ClientCapabilities = FbnsClientCapabilities,
                    EndpointCapabilities = FbnsEndpointCapabilities,
                    PublishFormat = FbnsPublishFormat,
                    NoAutomaticForeground = FbnsNoAutomaticForeground,
                    MakeUserAvailableInForeground = FbnsMakeUserAvailableInForeground,
                    DeviceId = DeviceId,
                    IsInitiallyForeground = FbnsIsInitiallyForeground,
                    NetworkType = FbnsNetworkType,
                    NetworkSubtype = FbnsNetworkSubtype,
                    SubscribeTopics = FbnsSubscribeTopics.ToList(),
                    ClientType = FbnsClientType,
                    AppId = FbnsAppId,
                    DeviceSecret = DeviceSecret,
                    ClientStack = FbnsClientStack,
                }
            };
        }
    }
}
