using System;
using System.Collections.Generic;
using InstagramAPI.Classes.Media;

namespace InstagramAPI
{
    public class UriCreator
    {
        public static readonly Uri BaseInstagramUri = new Uri("https://i.instagram.com");
        public static readonly Uri BackgroundInstagramUri = new Uri("https://b.i.instagram.com");
        private const string API_SUFFIX = "/api/v1";
        private const string API_SUFFIX_V2 = "/api/v2";

        public static Uri GetGraphQlUri(string queryHash, string variables)
        {
            if (!Uri.TryCreate(BaseInstagramUri, "/graphql/query/", out var instaUri))
                throw new Exception("Cant create URI for GraphQL");
            return new UriBuilder(instaUri) {Query = $"query_hash={queryHash}&variables={variables}"}.Uri;
        }

        public static Uri GetTokenResultUri(string deviceId, string phoneId)
        {
            if (!Uri.TryCreate(BackgroundInstagramUri,
                $"{API_SUFFIX}/zr/token/result/?device_id={deviceId}&custom_device_id={phoneId}&fetch_reason=token_expired",
                out var instaUri))
                throw new Exception("Cant create URI for token result");
            return instaUri;
        }

        public static Uri GetDirectBadgeCountUri()
        {
            if (!Uri.TryCreate(BackgroundInstagramUri, API_SUFFIX + "/direct_v2/get_badge_count/", out var instaUri))
                throw new Exception("Cant create URI for direct badge count");
            return instaUri;
        }

        public static Uri GetAccountFamilyUri()
        {
            if (!Uri.TryCreate(BackgroundInstagramUri, API_SUFFIX + "/multiple_accounts/get_account_family/", out var instaUri))
                throw new Exception("Cant create URI for launcher sync");
            return instaUri;
        }

        public static Uri GetLauncherSyncUri()
        {
            if (!Uri.TryCreate(BackgroundInstagramUri, API_SUFFIX + "/launcher/sync/", out var instaUri))
                throw new Exception("Cant create URI for launcher sync");
            return instaUri;
        }

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

        public static Uri GetTwoFactorLoginUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/accounts/two_factor_login/", out var instaUri))
                throw new Exception("Cant create URI for user 2FA login");
            return instaUri;
        }

        public static Uri GetCurrentUserUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/accounts/current_user?edit=true", out var instaUri))
                throw new Exception("Cant create URI for current user info");
            return instaUri;
        }

        public static Uri GetUserInfoUri(long userId)
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + $"/users/{userId}/info/?from_module=feed_timeline", out var instaUri))
                throw new Exception("Cant create URI for user info");
            return instaUri;
        }

        public static Uri GetDirectInboxUri(string nextId = "")
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/inbox/", out var instaUri))
                throw new Exception("Cant create URI for get inbox");
            return !string.IsNullOrEmpty(nextId)
                ? new UriBuilder(instaUri) { Query = $"persistentBadging=true&use_unified_inbox=true&cursor={nextId}&direction=older&limit=10&thread_message_limit=10" }.Uri
                 : new UriBuilder(instaUri) { Query = "persistentBadging=true&use_unified_inbox=true&limit=10&thread_message_limit=10" }.Uri;
        }

        public static Uri GetDirectInboxInfoUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/inbox/", out var instaUri))
                throw new Exception("Cant create URI for get inbox");
            return new UriBuilder(instaUri) {Query = "persistentBadging=true&use_unified_inbox=true&limit=1&thread_message_limit=0" }.Uri;
        }

        public static Uri GetPendingInboxUri(string nextId = "")
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/pending_inbox/", out var instaUri))
                throw new Exception("Cant create URI for get inbox");
            return !string.IsNullOrEmpty(nextId)
                ? new UriBuilder(instaUri) { Query = $"cursor={nextId}&direction=older" }.Uri
                : new UriBuilder(instaUri).Uri;
        }

        public static Uri GetPendingInboxApproveUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/approve_multiple/", out var instaUri))
                throw new Exception("Cant create URI for get inbox");
            return instaUri;
        }

        public static Uri GetDirectInboxThreadUri(string threadId, string nextId, long seqId)
        {
            if (
                !Uri.TryCreate(BaseInstagramUri, API_SUFFIX + $"/direct_v2/threads/{threadId}",
                    out var instaUri)) throw new Exception("Cant create URI for get inbox thread by id");
            return !string.IsNullOrEmpty(nextId)
                ? new UriBuilder(instaUri) { Query = $"visual_message_return_type=unseen&cursor={nextId}&direction=older&seq_id={seqId}&limit=20" }.Uri
                : new UriBuilder(instaUri) { Query = $"visual_message_return_type=unseen&seq_id={seqId}&limit=20" }.Uri;
        }

        public static Uri GetThreadByRecipientsUri(IEnumerable<long> userIds)
        {
            var idString = string.Join(",", userIds);
            if (!Uri.TryCreate(BaseInstagramUri,
                API_SUFFIX + $"/direct_v2/threads/get_by_participants/?recipient_users=[{idString}]&limit=20", out var instaUri))
                throw new Exception("Cant create URI for get participants recipient user");
            return instaUri;
        }

        public static Uri GetRankedRecipientsUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/ranked_recipients", out var instaUri))
                throw new Exception("Cant create URI (get ranked recipients)");
            return instaUri;
        }

        public static Uri GetRankRecipientsByUserUri(string username, bool includeThreads = true)
        {
            if (!Uri.TryCreate(BaseInstagramUri,
                API_SUFFIX +
                $"/direct_v2/ranked_recipients/?mode=reshare&show_threads={includeThreads.ToString().ToLower()}&query={username}&use_unified_inbox=true", out var instaUri))
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

        public static Uri GetDirectReelShareUri(InstaMediaType mediaType)
        {
            var mediaTypeStr = mediaType == InstaMediaType.Video ? "video" : "image";
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + $"/direct_v2/threads/broadcast/reel_share/?media_type={mediaTypeStr}", out var instaUri))
                throw new Exception("Can't create URI for sending reel share");
            return instaUri; 
        }

        public static Uri GetDirectReelReactUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/threads/broadcast/reel_react/", out var instaUri))
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

        public static Uri GetReelsTrayUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/feed/reels_tray/", out var instaUri))
                throw new Exception("Cant create URI for getting reels tray");
            return instaUri;
        }

        public static Uri GetReelsMediaUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/feed/reels_media/", out var instaUri))
                throw new Exception("Cant create URI for getting reels media");
            return instaUri;
        }

        public static Uri GetMarkStorySeenUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX_V2 + "/media/seen/?reel=1&live_vod=0", out var instaUri))
                throw new Exception("Cant create URI for marking story seen");
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

        public static Uri GetUnsendMessageUri(string threadId, string itemId)
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + $"/direct_v2/threads/{threadId}/items/{itemId}/delete/", out var instaUri))
                throw new Exception("Cant create URI for unsending message");
            return instaUri;
        }

        public static Uri GetDirectThreadItemsUri(string threadId, params string[] itemIds)
        {
            if (itemIds.Length == 0) throw new Exception("At least 1 item id is required");
            if (!Uri.TryCreate(BaseInstagramUri,
                API_SUFFIX + $"/direct_v2/threads/{threadId}/get_items/?item_ids=[{string.Join(",", itemIds)}]",
                out var instaUri))
                throw new Exception("Cant create URI for getting thread items");
            return instaUri;
        }

        public static Uri GetCreateGroupThread()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/create_group_thread/", out var instaUri))
                throw new Exception("Cant create URI for creating group thread");
            return instaUri;
        }

        public static Uri GetDirectUserPresenceUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/get_presence/", out var instaUri))
                throw new Exception("Cant create URI for user presence");
            return instaUri;
        }

        public static Uri GetAnimatedImageSearchUri(string query, string type)
        {
            if (!Uri.TryCreate(BaseInstagramUri,
                API_SUFFIX +
                $"/creatives/story_media_search_keyed_format/?request_surface=direct&q={query}&media_types=[\"{type}\"]",
                out var instaUri))
                throw new Exception("Cant create URI for searching animated image");
            return instaUri;
        }

        public static Uri GetSendAnimatedImageUri()
        {
            if (!Uri.TryCreate(BaseInstagramUri, API_SUFFIX + "/direct_v2/threads/broadcast/animated_media/", out var instaUri))
                throw new Exception("Cant create URI for sending animated media");
            return instaUri;
        }
    }
}
