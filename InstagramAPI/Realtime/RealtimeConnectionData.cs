using System.Collections.Generic;
using System.Linq;
using InstagramAPI.Classes.Android;
using InstagramAPI.Classes.Core;
using InstagramAPI.Fbns;

namespace InstagramAPI.Realtime
{
    internal class RealtimeConnectionData : BaseConnectionData
    {
        private const long ClientCapabilities = 183;
        private const long EndpointCapabilities = 0;
        private const long AppId = 567067343352427;
        private const sbyte ClientStack = 3;
        private const int PublishFormat = 1;
        private const int NetworkType = 1;
        private const int NetworkSubtype = 0;
        private const bool NoAutomaticForeground = true;
        private const bool MakeUserAvailableInForeground = false;
        private const bool IsInitiallyForeground = true;
        private const string ClientType = "cookie_auth";
        private const string RegionPreference = "FRC";
        private const string RealtimeDeviceSecret = "";
        private static readonly int[] SubscribeTopics = { 88, 135, 244, 149, 150, 245, 133, 146, 34 };

        #region AppSpecificInfo

        private const string Platform = "android";
        private const string IgMqttRoute = "django";
        private const string PubsubMsgTypeBlacklist = "direct, typing_type";
        private const string AuthCacheEnabled = "1";
        private const string EverclearSubscriptions =
            "{\"inapp_notification_subscribe_comment\":\"17899377895239777\",\"inapp_notification_subscribe_comment_mention_and_reply\":\"17899377895239777\",\"business_import_page_media_delivery_subscribe\":\"17940467278199720\",\"video_call_participant_state_delivery\":\"17977239895057311\",\"inapp_notification_subscribe_story_emoji_reaction\":\"17899377895239777\"}";

        #endregion

        public string Capabilities { get; set; }

        public string AppVersion { get; set; }

        public RealtimeConnectionData(AndroidDevice device, ApiVersion apiVersion)
        {
            UserAgent = device.UserAgent;
            DeviceId = device.PhoneId.ToString();
            Capabilities = apiVersion.Capabilities;
            AppVersion = apiVersion.AppVersion;
            DeviceSecret = RealtimeDeviceSecret;
        }

        public void SetCredential(UserSessionData session)
        {
            UserId = session.LoggedInUser.Pk;
            Password = session.AuthorizationToken;
        }

        public override ConnectPayload ToPayload()
        {
            return new ConnectPayload
            {
                ClientId = ClientId,
                Password = Password,
                ClientInfo = new ClientInfo
                {
                    AppId = AppId,
                    UserAgent = UserAgent,
                    ClientStack = ClientStack,
                    NetworkType = NetworkType,
                    NetworkSubtype = NetworkSubtype,
                    ClientType = ClientType,
                    RegionPreference = RegionPreference,
                    DeviceSecret = DeviceSecret,
                    ClientCapabilities = ClientCapabilities,
                    EndpointCapabilities = EndpointCapabilities,
                    PublishFormat = PublishFormat,
                    NoAutomaticForeground = NoAutomaticForeground,
                    MakeUserAvailableInForeground = MakeUserAvailableInForeground,
                    DeviceId = DeviceId,
                    IsInitiallyForeground = IsInitiallyForeground,
                    ClientMqttSessionId = ClientMqttSessionId,
                    SubscribeTopics = SubscribeTopics.ToList(),
                },
                AppSpecificInfo = new Dictionary<string, string>
                {
                    {"capabilities", Capabilities},
                    {"app_version", AppVersion},
                    {"everclear_subscriptions", EverclearSubscriptions},
                    {"User-Agent", UserAgent},
                    {"Accept-Language", string.Join(", ", Windows.Globalization.ApplicationLanguages.Languages)},
                    {"platform", Platform},
                    {"ig_mqtt_route", IgMqttRoute},
                    {"pubsub_msg_type_blacklist", PubsubMsgTypeBlacklist},
                    {"auth_cache_enabled", AuthCacheEnabled}
                }
            };
        }
    }
}
