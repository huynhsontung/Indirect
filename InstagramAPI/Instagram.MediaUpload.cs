using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Direct.ItemContent;
using InstagramAPI.Classes.Media;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using InstagramAPI.Classes.Core;

namespace InstagramAPI
{
    partial class Instagram
    {
        public async Task<Result<ItemAckPayloadResponse>> SendDirectPhotoAsync(InstaImage image, string threadId,
            long uploadId,
            Action<UploaderProgress> progress = null)
        {
            var upProgress = new UploaderProgress
            {
                Caption = string.Empty,
                UploadState = InstaUploadState.Preparing
            };
            try
            {
                var entityName = "direct_" + uploadId;
                var uri = UriCreator.GetDirectSendPhotoUri(entityName);
                upProgress.UploadId = uploadId.ToString();
                progress?.Invoke(upProgress);
                var ruploadParams = new JObject(
                    new JProperty("media_type", 1),
                    new JProperty("upload_id", uploadId.ToString()),
                    new JProperty("upload_media_height", image.Height),
                    new JProperty("upload_media_width", image.Width));
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
                requestMessage.Headers.Add("X-Entity-Name", entityName);
                requestMessage.Headers.Add("X-Instagram-Rupload-Params", ruploadParams.ToString(Formatting.None));
                requestMessage.Headers.Add("Offset", "0");
                var uploadBuffer = image.UploadBuffer;
                var content = new ByteArrayContent(uploadBuffer.ToArray());
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                requestMessage.Headers.Add("X-Entity-Length", uploadBuffer.Length.ToString());
                requestMessage.Content = content;
                upProgress.UploadState = InstaUploadState.Uploading;
                progress?.Invoke(upProgress);
                var response = await HttpClient.SendAsync(requestMessage);
                var json = await response.Content.ReadAsStringAsync();

                var ruploadResp = JsonConvert.DeserializeObject<RuploadResponse>(json);
                if (!response.IsSuccessStatusCode || !ruploadResp.IsOk())
                {
                    upProgress.UploadState = InstaUploadState.Error;
                    progress?.Invoke(upProgress);
                    return Result<ItemAckPayloadResponse>.Fail(json);
                }

                var uploadIdResp = ruploadResp.UploadId;
                upProgress.UploadState = InstaUploadState.Uploaded;
                progress?.Invoke(upProgress);
                var configUri = UriCreator.GetDirectConfigPhotoUri();
                var config = new Dictionary<string, string>(7)
                {
                    ["action"] = "send_item",
                    ["allow_full_aspect_ratio"] = "1",
                    ["content_type"] = "photo",
                    ["mutation_token"] = Guid.NewGuid().ToString(),
                    ["sampled"] = "1",
                    ["thread_id"] = threadId,
                    ["upload_id"] = uploadIdResp
                };
                response = await HttpClient.PostAsync(configUri, new FormUrlEncodedContent(config));
                json = await response.Content.ReadAsStringAsync();

                var obj = JsonConvert.DeserializeObject<ItemAckResponse>(json);
                if (!response.IsSuccessStatusCode || !obj.IsOk())
                {
                    upProgress.UploadState = InstaUploadState.Error;
                    progress?.Invoke(upProgress);
                    return Result<ItemAckPayloadResponse>.Fail(json, obj.Message);
                }

                upProgress.UploadState = InstaUploadState.Completed;
                progress?.Invoke(upProgress);
                return Result<ItemAckPayloadResponse>.Success(obj.Payload, json, obj.Message);
            }
            catch (Exception exception)
            {
                upProgress.UploadState = InstaUploadState.Error;
                progress?.Invoke(upProgress);
                DebugLogger.LogException(exception);
                return Result<ItemAckPayloadResponse>.Except(exception);
            }
        }

        /// <summary>
        ///     Send video to direct thread (single) with progress
        /// </summary>
        /// <param name="progress">Progress action</param>
        /// <param name="video">Video to upload (no need to set thumbnail)</param>
        /// <param name="threadId">Thread id</param>
        public async Task<Result<bool>> SendDirectVideoAsync(Action<UploaderProgress> progress, InstaVideoUpload video, string threadId)
        {
            ValidateLoggedIn();
            return await SendVideoAsync(progress, true, false, null, VisualMediaViewMode.Replayable, InstaStoryType.Both, null, threadId, video);
        }

        /// <summary>
        ///     Send video story, direct video, disappearing video
        /// </summary>
        /// <param name="isDirectVideo">Direct video</param>
        /// <param name="isDisappearingVideo">Disappearing video</param>
        private async Task<Result<bool>> SendVideoAsync(Action<UploaderProgress> progress, bool isDirectVideo, bool isDisappearingVideo, string caption,
            VisualMediaViewMode viewMode, InstaStoryType storyType, string recipients, string threadId, InstaVideoUpload video, Uri uri = null, StoryUploadOptions uploadOptions = null)
        {
            var upProgress = new UploaderProgress
            {
                Caption = caption ?? string.Empty,
                UploadState = InstaUploadState.Preparing
            };
            try
            {
                var uploadId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                var videoHashCode = video.Video.Url?.GetHashCode() ?? $"C:\\{GenerateRandomString(13)}.mp4".GetHashCode();
                var waterfallId = Guid.NewGuid().ToString();
                var videoEntityName = $"{uploadId}_0_{videoHashCode}";
                var videoUri = UriCreator.GetStoryUploadVideoUri(uploadId, videoHashCode);
                var retryContext = GetRetryContext();
                HttpRequestMessage request;
                HttpResponseMessage response;
                string videoUploadParams = null;
                string json = null;
                upProgress.UploadId = uploadId;
                progress?.Invoke(upProgress);
                var videoUploadParamsObj = new JObject();
                if (isDirectVideo)
                {
                    videoUploadParamsObj = new JObject
                    {
                        {"upload_media_height", "0"},
                        {"direct_v2", "1"},
                        {"upload_media_width", "0"},
                        {"upload_media_duration_ms", "0"},
                        {"upload_id", uploadId},
                        {"retry_context", retryContext},
                        {"media_type", "2"}
                    };

                    videoUploadParams = JsonConvert.SerializeObject(videoUploadParamsObj);
                    request = new HttpRequestMessage(HttpMethod.Get, videoUri);
                    request.Headers.Add("X_FB_VIDEO_WATERFALL_ID", waterfallId);
                    request.Headers.Add("X-Instagram-Rupload-Params", videoUploadParams);
                    response = await HttpClient.SendAsync(request);
                    json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        upProgress.UploadState = InstaUploadState.Error;
                        progress?.Invoke(upProgress);
                        return Result<bool>.Fail(json, response.ReasonPhrase);
                    }
                }
                else
                {
                    videoUploadParamsObj = new JObject
                    {
                        {"_csrftoken", HttpClient.GetCsrfToken()},
                        {"_uid", Session.LoggedInUser.Pk},
                        {"_uuid", Device.Uuid.ToString()},
                        {
                            "media_info", new JObject
                            {
                                {"capture_mode", "normal"},
                                {"media_type", 2},
                                {"caption", caption ?? string.Empty},
                                {"mentions", new JArray()},
                                {"hashtags", new JArray()},
                                {"locations", new JArray()},
                                {"stickers", new JArray()},
                            }
                        }
                    };
                    request = HttpClientManager.GetSignedRequest(UriCreator.GetStoryMediaInfoUploadUri(), videoUploadParamsObj);
                    response = await HttpClient.SendAsync(request);
                    json = await response.Content.ReadAsStringAsync();

                    videoUploadParamsObj = new JObject
                    {
                        {"upload_media_height", "0"},
                        {"upload_media_width", "0"},
                        {"upload_media_duration_ms", "0"},
                        {"upload_id", uploadId},
                        {"retry_context", "{\"num_step_auto_retry\":0,\"num_reupload\":0,\"num_step_manual_retry\":0}"},
                        {"media_type", "2"}
                    };
                    if (isDisappearingVideo)
                    {
                        videoUploadParamsObj.Add("for_direct_story", "1");
                    }
                    else
                    {
                        switch (storyType)
                        {
                            case InstaStoryType.SelfStory:
                            default:
                                videoUploadParamsObj.Add("for_album", "1");
                                break;

                            case InstaStoryType.Direct:
                                videoUploadParamsObj.Add("for_direct_story", "1");
                                break;

                            case InstaStoryType.Both:
                                videoUploadParamsObj.Add("for_album", "1");
                                videoUploadParamsObj.Add("for_direct_story", "1");
                                break;
                        }
                    }
                    videoUploadParams = JsonConvert.SerializeObject(videoUploadParamsObj);
                    request = new HttpRequestMessage(HttpMethod.Get, videoUri);
                    request.Headers.Add("X_FB_VIDEO_WATERFALL_ID", waterfallId);
                    request.Headers.Add("X-Instagram-Rupload-Params", videoUploadParams);
                    response = await HttpClient.SendAsync(request);
                    json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        upProgress.UploadState = InstaUploadState.Error;
                        progress?.Invoke(upProgress);
                        return Result<bool>.Fail(json, response.ReasonPhrase);
                    }
                }

                // video part
                IBuffer videoUploadBuffer;
                if (video.Video.UploadBuffer == null)
                {
                    if (video.Video.Url == null) throw new NullReferenceException("No upload buffer or file path are provided for video upload.");
                    var videoFile = await StorageFile.GetFileFromPathAsync(video.Video.Url.AbsolutePath);
                    videoUploadBuffer = await FileIO.ReadBufferAsync(videoFile);
                }
                else
                    videoUploadBuffer = video.Video.UploadBuffer;

                var videoContent = new ByteArrayContent(videoUploadBuffer.ToArray());

                request = new HttpRequestMessage(HttpMethod.Post, videoUri) { Content = videoContent };
                upProgress.UploadState = InstaUploadState.Uploading;
                progress?.Invoke(upProgress);
                var vidExt = Path.GetExtension(video.Video.Url?.AbsolutePath ?? $"C:\\{GenerateRandomString(13)}.mp4")
                    .Replace(".", "").ToLower();
                if (vidExt == "mov")
                    request.Headers.Add("X-Entity-Type", "video/quicktime");
                else
                    request.Headers.Add("X-Entity-Type", "video/mp4");

                request.Headers.Add("Offset", "0");
                request.Headers.Add("X-Instagram-Rupload-Params", videoUploadParams);
                request.Headers.Add("X-Entity-Name", videoEntityName);
                request.Headers.Add("X-Entity-Length", videoUploadBuffer.Length.ToString());
                request.Headers.Add("X_FB_VIDEO_WATERFALL_ID", waterfallId);
                response = await HttpClient.SendAsync(request);
                json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    upProgress.UploadState = InstaUploadState.Error;
                    progress?.Invoke(upProgress);
                    return Result<bool>.Fail(json, response.ReasonPhrase);
                }
                upProgress.UploadState = InstaUploadState.Uploaded;
                progress?.Invoke(upProgress);
                //upProgress = progressContent?.UploaderProgress;
                if (!isDirectVideo)
                {
                    upProgress.UploadState = InstaUploadState.UploadingThumbnail;
                    progress?.Invoke(upProgress);
                    var photoHashCode = video.VideoThumbnail.Url?.GetHashCode() ?? $"C:\\{GenerateRandomString(13)}.jpg".GetHashCode();
                    var photoEntityName = $"{uploadId}_0_{photoHashCode}";
                    var photoUri = UriCreator.GetStoryUploadPhotoUri(uploadId, photoHashCode);
                    var photoUploadParamsObj = new JObject
                    {
                        {"retry_context", retryContext},
                        {"media_type", "2"},
                        {"upload_id", uploadId},
                        {"image_compression", "{\"lib_name\":\"moz\",\"lib_version\":\"3.1.m\",\"quality\":\"95\"}"},
                    };

                    var photoUploadParams = JsonConvert.SerializeObject(photoUploadParamsObj);
                    IBuffer thumbnailUploadBuffer;
                    if (video.VideoThumbnail.UploadBuffer == null)
                    {
                        if (video.VideoThumbnail.Url == null) throw new NullReferenceException("No upload buffer or file path are provided for video thumbnail upload.");
                        var videoFile = await StorageFile.GetFileFromPathAsync(video.VideoThumbnail.Url.AbsolutePath);
                        thumbnailUploadBuffer = await FileIO.ReadBufferAsync(videoFile);
                    }
                    else
                        thumbnailUploadBuffer = video.VideoThumbnail.UploadBuffer;
                    var imageContent = new ByteArrayContent(thumbnailUploadBuffer.ToArray());
                    imageContent.Headers.Add("Content-Transfer-Encoding", "binary");
                    imageContent.Headers.Add("Content-Type", "application/octet-stream");
                    request = new HttpRequestMessage(HttpMethod.Post, photoUri);
                    request.Content = imageContent;
                    request.Headers.Add("X-Entity-Type", "image/jpeg");
                    request.Headers.Add("Offset", "0");
                    request.Headers.Add("X-Instagram-Rupload-Params", photoUploadParams);
                    request.Headers.Add("X-Entity-Name", photoEntityName);
                    request.Headers.Add("X-Entity-Length", thumbnailUploadBuffer.Length.ToString());
                    request.Headers.Add("X_FB_PHOTO_WATERFALL_ID", waterfallId);
                    response = await HttpClient.SendAsync(request);
                    json = await response.Content.ReadAsStringAsync();
                    upProgress.UploadState = InstaUploadState.ThumbnailUploaded;
                    progress?.Invoke(upProgress);
                }
                return await ConfigureVideo(progress, upProgress, uploadId, isDirectVideo, isDisappearingVideo, caption, viewMode, storyType, recipients, threadId, uri, uploadOptions);
            }
            catch (Exception exception)
            {
                upProgress.UploadState = InstaUploadState.Error;
                progress?.Invoke(upProgress);
                DebugLogger.LogException(exception);
                return Result<bool>.Except(exception);
            }
        }

        private async Task<Result<bool>> ConfigureVideo(Action<UploaderProgress> progress, UploaderProgress upProgress, string uploadId, bool isDirectVideo, bool isDisappearingVideo, string caption,
            VisualMediaViewMode viewMode, InstaStoryType storyType, string recipients, string threadId, Uri uri, StoryUploadOptions uploadOptions = null)
        {
            try
            {
                upProgress.UploadState = InstaUploadState.Configuring;
                progress?.Invoke(upProgress);
                var instaUri = UriCreator.GetDirectConfigVideoUri();
                var retryContext = GetRetryContext();
                var clientContext = Guid.NewGuid().ToString();

                if (isDirectVideo)
                {
                    var data = new Dictionary<string, string>
                    {
                        {"action", "send_item"},
                        {"client_context", clientContext},
                        {"_csrftoken", HttpClient.GetCsrfToken()},
                        {"video_result", ""},
                        {"_uuid", Device.Uuid.ToString()},
                        {"upload_id", uploadId}
                    };
                    if (!string.IsNullOrEmpty(recipients))
                        data.Add("recipient_users", $"[[{recipients}]]");
                    else
                        data.Add("thread_ids", $"[{threadId}]");

                    instaUri = UriCreator.GetDirectConfigVideoUri();
                    var request = new HttpRequestMessage(HttpMethod.Post, instaUri);
                    request.Content = new FormUrlEncodedContent(data);
                    request.Headers.Add("retry_context", retryContext);
                    var response = await HttpClient.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        upProgress.UploadState = InstaUploadState.Error;
                        progress?.Invoke(upProgress);
                        return Result<bool>.Fail(json, response.ReasonPhrase);
                    }
                    var obj = JsonConvert.DeserializeObject<DefaultResponse>(json);

                    if (obj.Status.ToLower() == "ok")
                    {
                        upProgress.UploadState = InstaUploadState.Configured;
                        progress?.Invoke(upProgress);
                    }
                    else
                    {
                        upProgress.UploadState = InstaUploadState.Completed;
                        progress?.Invoke(upProgress);
                    }

                    return obj.IsOk()
                        ? Result<bool>.Success(true, json, obj.Message)
                        : Result<bool>.Fail(json, obj.Message);
                }
                else
                {
                    var rnd = new Random();
                    var data = new JObject
                    {
                        {"filter_type", "0"},
                        {"timezone_offset", "16200"},
                        {"_csrftoken", HttpClient.GetCsrfToken()},
                        {"client_shared_at", (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - rnd.Next(25,55)).ToString()},
                        {"story_media_creation_date", (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - rnd.Next(50,70)).ToString()},
                        {"media_folder", "Camera"},
                        {"source_type", "4"},
                        {"video_result", ""},
                        {"_uid", Session.LoggedInUser.Pk.ToString()},
                        {"_uuid", Device.Uuid.ToString()},
                        {"caption", caption ?? string.Empty},
                        {"date_time_original", DateTimeOffset.Now.ToString("yyyy-dd-MMTh:mm:ss-0fffZ")},
                        {"capture_type", "normal"},
                        {"mas_opt_in", "NOT_PROMPTED"},
                        {"upload_id", uploadId},
                        {"client_timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds()},
                        {
                            "device", new JObject{
                                {"manufacturer", Device.HardwareManufacturer},
                                {"model", Device.HardwareModel},
                                {"android_release", Device.AndroidVersion.VersionNumber},
                                {"android_version", Device.AndroidVersion.APILevel}
                            }
                        },
                        {"length", 0},
                        {
                            "extra", new JObject
                            {
                                {"source_width", 0},
                                {"source_height", 0}
                            }
                        },
                        {"audio_muted", false},
                        {"poster_frame_index", 0},
                    };
                    if (isDisappearingVideo)
                    {
                        data.Add("view_mode", viewMode.ToString().ToLower());
                        data.Add("configure_mode", "2");
                        data.Add("recipient_users", "[]");
                        data.Add("thread_ids", $"[{threadId}]");
                    }
                    else
                    {
                        switch (storyType)
                        {
                            case InstaStoryType.SelfStory:
                            default:
                                data.Add("configure_mode", "1");
                                break;
                            case InstaStoryType.Direct:
                                data.Add("configure_mode", "2");
                                data.Add("view_mode", "replayable");
                                data.Add("recipient_users", "[]");
                                data.Add("thread_ids", $"[{threadId}]");
                                break;
                            case InstaStoryType.Both:
                                data.Add("configure_mode", "3");
                                data.Add("view_mode", "replayable");
                                data.Add("recipient_users", "[]");
                                data.Add("thread_ids", $"[{threadId}]");
                                break;
                        }

                        if (uri != null)
                        {
                            var webUri = new JArray
                            {
                                new JObject
                                {
                                    {"webUri", uri.ToString()}
                                }
                            };
                            var storyCta = new JArray
                            {
                                new JObject
                                {
                                    {"links",  webUri}
                                }
                            };
                            data.Add("story_cta", storyCta.ToString(Formatting.None));
                        }

                        if (uploadOptions != null)
                        {
                            if (uploadOptions.Hashtags?.Count > 0)
                            {
                                var hashtagArr = new JArray();
                                foreach (var item in uploadOptions.Hashtags)
                                    hashtagArr.Add(item.ToJson());

                                data.Add("story_hashtags", hashtagArr.ToString(Formatting.None));
                            }

                            if (uploadOptions.Locations?.Count > 0)
                            {
                                var locationArr = new JArray();
                                foreach (var item in uploadOptions.Locations)
                                    locationArr.Add(item.ToJson());

                                data.Add("story_locations", locationArr.ToString(Formatting.None));
                            }
                            if (uploadOptions.Slider != null)
                            {
                                var sliderArr = new JArray
                                {
                                    uploadOptions.Slider.ToJson()
                                };

                                data.Add("story_sliders", sliderArr.ToString(Formatting.None));
                                if (uploadOptions.Slider.IsSticker)
                                    data.Add("story_sticker_ids", $"emoji_slider_{uploadOptions.Slider.Emoji}");
                            }
                            else
                            {
                                if (uploadOptions.Polls?.Count > 0)
                                {
                                    var pollArr = new JArray();
                                    foreach (var item in uploadOptions.Polls)
                                        pollArr.Add(item.ToJson());

                                    data.Add("story_polls", pollArr.ToString(Formatting.None));
                                }
                                if (uploadOptions.Questions?.Count > 0)
                                {
                                    var questionArr = new JArray();
                                    foreach (var item in uploadOptions.Questions)
                                        questionArr.Add(item.ToJson());

                                    data.Add("story_questions", questionArr.ToString(Formatting.None));
                                }
                            }
                            if (uploadOptions.Countdown != null)
                            {
                                var countdownArr = new JArray
                                {
                                    uploadOptions.Countdown.ToJson()
                                };

                                data.Add("story_countdowns", countdownArr.ToString(Formatting.None));
                                data.Add("story_sticker_ids", "countdown_sticker_time");
                            }
                        }
                    }
                    instaUri = UriCreator.GetVideoStoryConfigureUri(true);
                    var request = HttpClientManager.GetSignedRequest(instaUri, data);

                    request.Headers.Add("retry_context", retryContext);
                    var response = await HttpClient.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var mediaResponse = JsonConvert.DeserializeObject<DefaultResponse>(json);

                        if (mediaResponse.IsOk())
                        {
                            upProgress.UploadState = InstaUploadState.Configured;
                            progress?.Invoke(upProgress);
                        }
                        else
                        {
                            upProgress.UploadState = InstaUploadState.Completed;
                            progress?.Invoke(upProgress);
                        }
                        return mediaResponse.IsOk() ? Result<bool>.Success(true) : Result<bool>.Fail(json, mediaResponse.Message);
                    }
                    upProgress.UploadState = InstaUploadState.Error;
                    progress?.Invoke(upProgress);
                    return Result<bool>.Fail(json, response.ReasonPhrase);
                }
            }
            catch (Exception exception)
            {
                upProgress.UploadState = InstaUploadState.Error;
                progress?.Invoke(upProgress);
                DebugLogger.LogException(exception);
                return Result<bool>.Except(exception);
            }
        }

    }
}
