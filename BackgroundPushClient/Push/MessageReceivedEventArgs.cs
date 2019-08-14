using System;
using Newtonsoft.Json;

namespace BackgroundPushClient.Push
{
    // Reference https://github.com/mgp25/Instagram-API/blob/master/src/Push/Notification.php
    internal sealed class MessageReceivedEventArgs : EventArgs
    {
        private string _notificationContentJson;

        [JsonIgnore] public string Json { get; internal set; }

        [JsonProperty("token")] public string Token { get; internal set; }
        [JsonProperty("ck")] public string ConnectionKey { get; internal set; }
        [JsonProperty("pn")] public string PackageName { get; internal set; }
        [JsonProperty("cp")] public string CollapseKey { get; internal set; }
        [JsonProperty("fbpushnotif")] internal string NotificationContentJson
        {
            get => _notificationContentJson;
            set
            {
                NotificationContent = JsonConvert.DeserializeObject<PushNotification>(value);
                _notificationContentJson = value;
            }
        }
        [JsonIgnore] public PushNotification NotificationContent { get; internal set; }
        [JsonProperty("nid")] public string NotificationId { get; internal set; }
        [JsonProperty("bu")] public string IsBuffered { get; internal set; }
    }

    public sealed class BadgeCount
    {
        [JsonProperty("di")] public int Direct { get; internal set; }
        [JsonProperty("ds")] public int Ds { get; internal set; }
        [JsonProperty("ac")] public int Activities { get; internal set; }
    }

    public sealed class PushNotification
    {
        private string _badgeCountJson;

        [JsonProperty("t")] public string Title { get; internal set; }
        [JsonProperty("m")] public string Message { get; internal set; }
        [JsonProperty("tt")] public string TickerText { get; internal set; }
        [JsonProperty("ig")] public string IgAction { get; internal set; }
        [JsonProperty("collapse_key")] public string CollapseKey { get; internal set; }
        [JsonProperty("i")] public string OptionalImage { get; internal set; }
        [JsonProperty("a")] public string OptionalAvatarUrl { get; internal set; }
        [JsonProperty("sound")] public string Sound { get; internal set; }
        [JsonProperty("pi")] public string PushId { get; internal set; }
        [JsonProperty("c")] public string PushCategory { get; internal set; }
        [JsonProperty("u")] public string IntendedRecipientUserId { get; internal set; }
        [JsonProperty("s")] public string SourceUserId { get; internal set; }
        [JsonProperty("igo")] public string IgActionOverride { get; internal set; }
        [JsonProperty("bc")] internal string BadgeCountJson
        {
            get => _badgeCountJson;
            set
            {
                BadgeCount = JsonConvert.DeserializeObject<BadgeCount>(value);
                _badgeCountJson = value;
            }
        }
        [JsonIgnore] public BadgeCount BadgeCount { get; internal set; }
        [JsonProperty("ia")] public string InAppActors { get; internal set; }
    }
}
