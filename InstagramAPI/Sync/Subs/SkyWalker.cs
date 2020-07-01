namespace InstagramAPI.Sync.Subs
{
    static class SkyWalker
    {
        public static string DirectSubscribe(string userId) => $"ig/u/v1/${userId}";
        public static string LiveSubscribe(string userId) => $"ig/live_notification_subscribe/${userId}";
    }
}
