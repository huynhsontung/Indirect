using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramAPI.Push
{
    // Reference https://github.com/mgp25/Instagram-API/blob/master/src/Push/Notification.php
    public class PushReceivedEventArgs : EventArgs
    {
        private string _notificationContentJson;

        [JsonIgnore] public string Json { get; set; }

        [JsonProperty("token")] public string Token { get; set; }
        [JsonProperty("ck")] public string ConnectionKey { get; set; }
        [JsonProperty("pn")] public string PackageName { get; set; }
        //[JsonProperty("cp")] public string CollapseKey { get; set; }
        [JsonProperty("fbpushnotif")]
        public string NotificationContentJson
        {
            get => _notificationContentJson;
            set
            {
                NotificationContent = JsonConvert.DeserializeObject<PushNotification>(value);
                _notificationContentJson = value;
            }
        }
        [JsonIgnore] public PushNotification NotificationContent { get; set; }
        [JsonProperty("nid")] public string NotificationId { get; set; }
        [JsonProperty("bu")] public string IsBuffered { get; set; }
    }

    public struct BadgeCount
    {
        [JsonProperty("di")] public int Direct { get; set; }
        [JsonProperty("ds")] public int Ds { get; set; }
        [JsonProperty("ac")] public int Activities { get; set; }
    }

    public struct PushNotification
    {
        private string _badgeCountJson;

        [JsonProperty("t")] public string Title { get; set; }
        [JsonProperty("m")] public string Message { get; set; }
        [JsonProperty("tt")] public string TickerText { get; set; }
        [JsonProperty("ig")] public string IgAction { get; set; }
        [JsonProperty("collapse_key")] public string CollapseKey { get; set; }
        [JsonProperty("i")] public string OptionalImage { get; set; }
        [JsonProperty("a")] public string OptionalAvatarUrl { get; set; }
        [JsonProperty("sound")] public string Sound { get; set; }
        [JsonProperty("pi")] public string PushId { get; set; }
        [JsonProperty("c")] public string PushCategory { get; set; }
        [JsonProperty("u")] public long IntendedRecipientUserId { get; set; }
        [JsonProperty("s")] public string SourceUserId { get; set; }
        [JsonProperty("igo")] public string IgActionOverride { get; set; }
        [JsonProperty("bc")]
        public string BadgeCountJson
        {
            get => _badgeCountJson;
            set
            {
                BadgeCount = JsonConvert.DeserializeObject<BadgeCount>(value);
                _badgeCountJson = value;
            }
        }
        [JsonIgnore] public BadgeCount BadgeCount { get; set; }
        [JsonProperty("ia")] public string InAppActors { get; set; }
    }
}
