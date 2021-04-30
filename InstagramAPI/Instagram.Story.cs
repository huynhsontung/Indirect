using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Web.Http;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using InstagramAPI.Classes.Core;

namespace InstagramAPI
{
    public partial class Instagram
    {
        private const string ReelsCapabilities =
            "[{\"name\":\"SUPPORTED_SDK_VERSIONS\",\"value\":\"66.0,67.0,68.0,69.0,70.0,71.0,72.0,73.0,74.0,75.0,76.0,77.0,78.0,79.0,80.0,81.0,82.0,83.0,84.0,85.0,86.0,87.0,88.0\"},{\"name\":\"FACE_TRACKER_VERSION\",\"value\":\"14\"},{\"name\":\"segmentation\",\"value\":\"segmentation_enabled\"},{\"name\":\"COMPRESSION\",\"value\":\"ETC2_COMPRESSION\"}]";

        public async Task<Result<Reel[]>> GetReelsTrayFeed(ReelsTrayFetchReason fetchReason = ReelsTrayFetchReason.ColdStart)
        {
            ValidateLoggedIn();
            try
            {
                var uri = UriCreator.GetReelsTrayUri();
                string reason;
                switch (fetchReason)
                {
                    case ReelsTrayFetchReason.WarmStartWithFeed:
                        reason = "warm_start_with_feed";
                        break;
                    case ReelsTrayFetchReason.PullToRefresh:
                        reason = "pull_to_refresh";
                        break;
                    default:
                        reason = "cold_start";
                        break;
                }
                var data = new Dictionary<string, string>
                {
                    {"supported_capabilities_new", ReelsCapabilities},
                    {"reason", reason},    // this can be "cold_start", "warm_start_with_feed", or "pull_to_refresh"
                    {"timezone_offset", ((int) DateTimeOffset.Now.Offset.TotalSeconds).ToString()},
                    {"_csrftoken", Session.CsrfToken},
                    {"_uuid", Device.Uuid.ToString()}
                };
                var response = await _httpClient.PostAsync(uri, new HttpFormUrlEncodedContent(data));
                var json = await response.Content.ReadAsStringAsync();
                DebugLogger.LogResponse(response);
                if (response.StatusCode != HttpStatusCode.Ok)
                    return Result<Reel[]>.Fail(json, response.ReasonPhrase);
                var payload = JsonConvert.DeserializeObject<JObject>(json);
                if (payload["status"]?.ToString() != "ok") return Result<Reel[]>.Fail(json);
                var reels = payload["tray"].ToObject<Reel[]>();
                return Result<Reel[]>.Success(reels, json);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                return Result<Reel[]>.Except(e);
            }
        }

        public async Task<Result<Dictionary<long, Reel>>> GetReels(ICollection<long> userIds)
        {
            ValidateLoggedIn();
            try
            {
                if (userIds == null || userIds.Count == 0) 
                    throw new ArgumentException("user ids is empty", nameof(userIds));
                var uri = UriCreator.GetReelsMediaUri();
                var data = new JObject
                {
                    {"supported_capabilities_new", ReelsCapabilities},
                    {"source", "reel_feed_timeline"},
                    {"_csrftoken", Session.CsrfToken},
                    {"_uid", Session.LoggedInUser.Pk.ToString()},
                    {"_uuid", Device.Uuid.ToString()},
                    {"user_ids", new JArray(userIds.Select(x => x.ToString()).ToArray())}
                };
                var body = new Dictionary<string,string>
                {
                    {"signed_body", $"SIGNATURE.{data.ToString(Formatting.None)}"}
                };
                var response = await _httpClient.PostAsync(uri, new HttpFormUrlEncodedContent(body));
                var json = await response.Content.ReadAsStringAsync();
                DebugLogger.LogResponse(response);
                if (response.StatusCode != HttpStatusCode.Ok)
                    return Result<Dictionary<long, Reel>>.Fail(json, response.ReasonPhrase);
                var payload = JsonConvert.DeserializeObject<JObject>(json);
                if (payload["status"]?.ToString() != "ok") return Result<Dictionary<long, Reel>>.Fail(json);
                var reels = payload["reels"].ToObject<Dictionary<long, Reel>>();
                return Result<Dictionary<long, Reel>>.Success(reels, json);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                return Result<Dictionary<long, Reel>>.Except(e);
            }
        }

        public async Task<Result<BaseStatusResponse>> MarkStorySeenAsync(string mediaId, long ownerId,
            DateTimeOffset storyTakenAt)
        {
            ValidateLoggedIn();
            try
            {

                var uri = UriCreator.GetMarkStorySeenUri();
                var payload = new JObject
                {
                    {"_csrftoken", Session.CsrfToken},
                    {"_uid", Session.LoggedInUser.Pk.ToString()},
                    {"_uuid", Device.Uuid},
                    {"container_module", "feed_timeline"},
                    {"live_vods_skipped", new JObject()},
                    {"nuxes_skipped", new JObject()},
                    {"nuxes", new JObject()},
                    {
                        "reels", new JObject
                        {
                            {
                                $"{mediaId}_{ownerId}",
                                new JArray
                                {
                                    $"{storyTakenAt.ToUnixTimeSeconds()}_{DateTimeOffset.Now.ToUnixTimeSeconds()}"
                                }
                            }
                        }
                    },
                    {"live_vods", new JObject()},
                    {"reel_media_skipped", new JObject()}
                };
                var body = $"SIGNATURE.{payload.ToString(Formatting.None)}";
                var data = new Dictionary<string, string>
                {
                    {"signed_body", body}
                };
                var response = await _httpClient.PostAsync(uri, new HttpFormUrlEncodedContent(data));
                var json = await response.Content.ReadAsStringAsync();
                DebugLogger.LogResponse(response);
                if (!response.IsSuccessStatusCode)
                    return Result<BaseStatusResponse>.Fail(json, response.ReasonPhrase);
                var obj = JsonConvert.DeserializeObject<BaseStatusResponse>(json);
                return obj.IsOk()
                    ? Result<BaseStatusResponse>.Success(obj, json)
                    : Result<BaseStatusResponse>.Fail(json);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                return Result<BaseStatusResponse>.Except(e);
            }
        }
        
    }
}
