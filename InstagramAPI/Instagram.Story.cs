using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Story;
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
                _logger?.LogResponse(response);
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
                _logger?.LogException(e);
                return Result<Reel[]>.Except(e);
            }
        }

        public async Task<Result<Reel[]>> GetReels(string[] userIds)
        {
            ValidateLoggedIn();
            try
            {
                const string queryHash = "04334405dbdef91f2c4e207b84c204d7";
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
                _logger?.LogResponse(response);
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
                _logger?.LogException(e);
                return Result<Reel[]>.Except(e);
            }
        }
    }
}
