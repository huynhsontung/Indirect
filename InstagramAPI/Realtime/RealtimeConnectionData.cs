using System.Collections.Generic;
using System.Linq;
using InstagramAPI.Classes.Android;
using InstagramAPI.Classes.Core;
using InstagramAPI.Fbns;

namespace InstagramAPI.Realtime
{
    internal class RealtimeConnectionData : BaseConnectionData
    {
        private readonly string _capabilities;
        private readonly string _appVersion;

        public RealtimeConnectionData(AndroidDevice device, ApiVersion apiVersion)
        {
            UserAgent = device.UserAgent;
            DeviceId = device.Uuid.ToString();
            _capabilities = apiVersion.Capabilities;
            _appVersion = apiVersion.AppVersion;
            ClientId = DeviceId.Substring(0, 20);
        }

        public void SetCredential(UserSessionData session)
        {
            UserId = session.LoggedInUser.Pk;
            //Password = $"authorization: {session.AuthorizationToken}";
            Password = $"sessionid={session.SessionId}";
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
                    ClientCapabilities = 183,
                    EndpointCapabilities = 0,
                    PublishFormat = 1,
                    NoAutomaticForeground = true,
                    MakeUserAvailableInForeground = false,
                    DeviceId = DeviceId,
                    IsInitiallyForeground = true,
                    NetworkType = 1,
                    NetworkSubtype = 0,
                    ClientMqttSessionId = ClientMqttSessionId,
                    SubscribeTopics = new List<int> { 88, 135, 244, 149, 150, 245, 133, 146, 34 },
                    ClientType = "cookie_auth",
                    AppId = 567067343352427,
                    RegionPreference = "FRC",
                    DeviceSecret = "",
                    ClientStack = 3,
                },
                AppSpecificInfo = new Dictionary<string, string>
                {
                    {"capabilities", _capabilities},
                    {"app_version", _appVersion},
                    {
                        "everclear_subscriptions",
                        "{\"inapp_notification_subscribe_comment\":\"17899377895239777\",\"inapp_notification_subscribe_comment_mention_and_reply\":\"17899377895239777\",\"business_import_page_media_delivery_subscribe\":\"17940467278199720\",\"video_call_participant_state_delivery\":\"17977239895057311\",\"inapp_notification_subscribe_story_emoji_reaction\":\"17899377895239777\"}"
                    },
                    {"User-Agent", UserAgent},
                    {
                        "Accept-Language",
                        string.Join(", ", Windows.Globalization.ApplicationLanguages.Languages.FirstOrDefault())
                    },
                    {"platform", "android"},
                    {"ig_mqtt_route", "django"},
                    {"pubsub_msg_type_blacklist", "direct, typing_type"},
                    {"auth_cache_enabled", "1"}
                }
            };
        }
    }
}
