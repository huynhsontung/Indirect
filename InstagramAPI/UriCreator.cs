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

        public static Uri GetDirectInboxUri(string NextId = "")
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/inbox/", out var instaUri))
                throw new Exception("Cant create URI for get inbox");
            return !string.IsNullOrEmpty(NextId)
                ? new UriBuilder(instaUri) { Query = $"persistentBadging=true&use_unified_inbox=true&cursor={NextId}&direction=older" }.Uri
                 : new UriBuilder(instaUri) { Query = "persistentBadging=true&use_unified_inbox=true" }.Uri;
        }
    }
}
