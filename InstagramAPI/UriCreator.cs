using System;
using System.Collections.Generic;

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

        public static Uri GetFacebookSignUpUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/fb/facebook_signup/", out var instaUri))
                throw new Exception("Cant create URI for facebook sign up url");
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
                ? new UriBuilder(instaUri) { Query = $"persistentBadging=true&use_unified_inbox=true&cursor={nextId}&direction=older&limit=20&thread_message_limit=10" }.Uri
                 : new UriBuilder(instaUri) { Query = "persistentBadging=true&use_unified_inbox=true&limit=20&thread_message_limit=10" }.Uri;
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

        public static Uri GetThreadByRecipientsUri(IEnumerable<long> userIds)
        {
            var idString = string.Join(",", userIds);
            if (!Uri.TryCreate(BaseInstagramUri,
                API_SUFFIX + $"/direct_v2/threads/get_by_participants/?recipient_users=[{idString}]", out var instaUri))
                throw new Exception("Cant create URI for get participants recipient user");
            return instaUri;
        }

        public static Uri GetRankedRecipientsUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/ranked_recipients", out var instaUri))
                throw new Exception("Cant create URI (get ranked recipients)");
            return instaUri;
        }

        public static Uri GetRankRecipientsByUserUri(string username)
        {
            if (!Uri.TryCreate(BaseInstagramUri,
                API_SUFFIX +
                $"/direct_v2/ranked_recipients/?mode=raven&show_threads=true&query={username}&use_unified_inbox=true", out var instaUri))
                throw new Exception("Cant create URI for get rank recipients by username");
            return instaUri;
        }

        public static Uri GetDirectThreadBroadcastLikeUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/threads/broadcast/like/", out var instaUri))
                throw new Exception("Cant create URI for broadcast post live likes");
            return instaUri;
        }

        public static Uri GetDirectSendMessageUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/threads/broadcast/text/", out var instaUri))
                throw new Exception("Cant create URI for sending message");
            return instaUri;
        }

        public static Uri GetSendDirectLinkUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/threads/broadcast/link/", out var instaUri))
                throw new Exception("Cant create URI for send link to direct thread");
            return instaUri;
        }

        public static Uri GetDirectSendPhotoUri(string uploadId)
        {
            if (!Uri.TryCreate(BaseInstagramUri, $"/rupload_igphoto/{uploadId}", out var instaUri))
                throw new Exception("Cant create URI for sending photo to direct");
            return instaUri;
        }

        public static Uri GetDirectConfigPhotoUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/threads/broadcast/configure_photo/", out var instaUri))
                throw new Exception("Cant create URI to config photo");
            return instaUri;
        }

        public static Uri GetStoryUploadPhotoUri(string uploadId, int fileHashCode)
        {
            if (!Uri.TryCreate(BaseInstagramUri, $"/rupload_igphoto/{uploadId}_0_{fileHashCode}", out var instaUri))
                throw new Exception("Cant create URI for story upload photo");
            return instaUri;
        }

        public static Uri GetStoryUploadVideoUri(string uploadId, int fileHashCode)
        {
            if (!Uri.TryCreate(BaseInstagramUri, $"/rupload_igvideo/{uploadId}_0_{fileHashCode}", out var instaUri))
                throw new Exception("Cant create URI for story upload video");
            return instaUri;
        }

        public static Uri GetStoryMediaInfoUploadUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/media/mas_opt_in_info/", out var instaUri))
                throw new Exception("Cant create URI for story media info");
            return instaUri;
        }

        public static Uri GetDirectConfigVideoUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/threads/broadcast/configure_video/", out var instaUri))
                throw new Exception("Cant create URI for direct config video");
            return instaUri;
        }

        public static Uri GetVideoStoryConfigureUri(bool isVideo = false)
        {
            if (!Uri.TryCreate(BaseInstagramUri, isVideo ? API_SUFFIX + "/media/configure_to_story/?video=1" : API_SUFFIX + "/media/configure_to_story/", out var instaUri))
                throw new Exception("Can't create URI for configuring story media");
            return instaUri;
        }

        public static Uri GetLikeUnlikeDirectMessageUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/threads/broadcast/reaction/", out var instaUri))
                throw new Exception("Cant create URI for like direct message");
            return instaUri;
        }

        public static Uri GetDirectThreadSeenUri(string threadId, string itemId)
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + $"/direct_v2/threads/{threadId}/items/{itemId}/seen/", out var instaUri))
                throw new Exception("Cant create URI for seen thread");
            return instaUri;
        }

        public static Uri GetDirectUserPresenceUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/get_presence/", out var instaUri))
                throw new Exception("Cant create URI for user presence");
            return instaUri;
        }
    }
}
