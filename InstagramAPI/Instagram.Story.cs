using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Web.Http;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Classes.Story;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI
{
    public partial class Instagram
    {
        public async Task<Result<Reel[]>> GetReelsTrayFeed()
        {
            ValidateLoggedIn();
            try
            {
                const string queryHash = "04334405dbdef91f2c4e207b84c204d7";
                const string variables =
                    "{\"only_stories\":true,\"stories_prefetch\":true,\"stories_video_dash_manifest\":false}";
                var uri = UriCreator.GetGraphQlUri(queryHash, variables);
                var response = await _httpClient.GetAsync(uri);
                var json = await response.Content.ReadAsStringAsync();
                DebugLogger.LogResponse(response);
                if (response.StatusCode != HttpStatusCode.Ok)
                    return Result<Reel[]>.Fail(json, response.ReasonPhrase);
                var payload = JsonConvert.DeserializeObject<JObject>(json);
                if (payload["status"].ToString() != "ok") return Result<Reel[]>.Fail(json);
                var reelsJson = payload["data"]["user"]["feed_reels_tray"]["edge_reels_tray_to_reel"]["edges"];
                var reels = reelsJson.Select(x => x["node"].ToObject<Reel>()).ToArray();
                return Result<Reel[]>.Success(reels, json);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                return Result<Reel[]>.Except(e);
            }
        }

        public async Task<Result<Reel[]>> GetReels(ICollection<string> userIds)
        {
            ValidateLoggedIn();
            try
            {
                if (userIds == null || userIds.Count == 0) 
                    return Result<Reel[]>.Fail(Array.Empty<Reel>(), "user ids is empty");
                const string queryHash = "f5dc1457da7a4d3f88762dae127e0238";
                var reelIds = new JArray(userIds);
                var variables =
                    $"{{\"reel_ids\": {reelIds.ToString(Formatting.None)}," +
                    $"\"tag_names\": []," +
                    $"\"location_ids\": []," +
                    $"\"highlight_reel_ids\": []," +
                    $"\"precomposed_overlay\": true," +
                    $"\"show_story_viewer_list\": true," +
                    $"\"story_viewer_fetch_count\": 50," +
                    $"\"story_viewer_cursor\": \"\"," +
                    $"\"stories_video_dash_manifest\": false}}";
                var uri = UriCreator.GetGraphQlUri(queryHash, variables);
                var response = await _httpClient.GetAsync(uri);
                var json = await response.Content.ReadAsStringAsync();
                DebugLogger.LogResponse(response);
                if (response.StatusCode != HttpStatusCode.Ok)
                    return Result<Reel[]>.Fail(json, response.ReasonPhrase);
                var payload = JsonConvert.DeserializeObject<JObject>(json);
                if (payload["status"].ToString() != "ok") return Result<Reel[]>.Fail(json);
                var reelsJson = payload["data"]["reels_media"];
                var reels = reelsJson.ToObject<Reel[]>();
                return Result<Reel[]>.Success(reels, json);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                return Result<Reel[]>.Except(e);
            }
        }

        public async Task<Result<BaseStatusResponse>> MarkStorySeenAsync(string mediaId, string ownerId,
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
                                $"{mediaId}_{ownerId}_{ownerId}",
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
