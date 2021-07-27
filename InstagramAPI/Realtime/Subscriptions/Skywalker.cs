namespace InstagramAPI.Realtime.Subscriptions
{
    internal static class SkywalkerSubscription
    {
        public static string GetDirectSubscription(long userId) => $"ig/u/v1/{userId}";

        public static string GetLiveSubscription(long userId) => $"ig/live_notification_subscribe/{userId}";
    }
}
