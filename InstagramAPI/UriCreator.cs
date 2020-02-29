using System;

namespace InstagramAPI
{
    public class UriCreator
    {
        public static readonly Uri BaseInstagramUri = new Uri("https://i.instagram.com");
        private const string API_SUFFIX = "/api/v1";
        private const string API_SUFFIX_V2 = "/api/v2";

        public static Uri GetLoginUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/accounts/login/", out var instaUri))
                throw new Exception("Cant create URI for user login");
            return instaUri;
        }

        public static Uri GetCurrentUserUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/accounts/current_user?edit=true", out var instaUri))
                throw new Exception("Cant create URI for current user info");
            return instaUri;
        }

        public static Uri GetDirectInboxUri(string nextId = "")
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/inbox/", out var instaUri))
                throw new Exception("Cant create URI for get inbox");
            return !string.IsNullOrEmpty(nextId)
                ? new UriBuilder(instaUri) { Query = $"persistentBadging=true&use_unified_inbox=true&cursor={nextId}&direction=older" }.Uri
                 : new UriBuilder(instaUri) { Query = "persistentBadging=true&use_unified_inbox=true" }.Uri;
        }

        public static Uri GetDirectInboxThreadUri(string threadId, string nextId)
        {
            if (
                !Uri.TryCreate(BaseInstagramUri, API_SUFFIX + $"/direct_v2/threads/{threadId}",
                    out var instaUri)) throw new Exception("Cant create URI for get inbox thread by id");
            return !string.IsNullOrEmpty(nextId)
                ? new UriBuilder(instaUri) { Query = $"use_unified_inbox=true&cursor={nextId}&direction=older" }.Uri
                : new UriBuilder(instaUri) { Query = $"use_unified_inbox=true" }.Uri;
        }

        public static Uri GetDirectThreadBroadcastLikeUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/threads/broadcast/like/", out var instaUri))
                throw new Exception("Cant create URI for broadcast post live likes");
            return instaUri;
        }
    }
}
