using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Indirect.Entities.Wrappers;
using Indirect.Utilities;
using InstagramAPI;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Media;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Utils;

namespace Indirect.Services
{
    internal class ChatService
    {
        private readonly Instagram _api;
        
        public ChatService(Instagram api)
        {
            _api = api;
        }

        public async Task SendMessage(DirectThreadWrapper thread, string content)
        {
            content = content.Trim(' ', '\n', '\r');
            if (string.IsNullOrEmpty(content)) return;
            content = content.Replace('\r', '\n');
            var tokens = content.Split("\t\n ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var links = tokens.Where(x =>
                x.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
                x.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase) ||
                x.StartsWith("www.", StringComparison.InvariantCultureIgnoreCase)).ToList();
            Result<DirectThread[]> result;
            Result<ItemAckPayloadResponse> ackResult;   // for links and hashtags
            try
            {
                if (!string.IsNullOrEmpty(thread.ThreadId))
                {
                    if (links.Any())
                    {
                        ackResult = await _api.SendLinkAsync(content, links, thread.ThreadId);
                        return;
                    }

                    result = await _api.SendTextAsync(null, thread.ThreadId, content);
                }
                else
                {
                    if (links.Any())
                    {
                        ackResult = await _api.SendLinkToRecipientsAsync(content, links,
                            thread.Users.Select(x => x.Pk).ToArray());
                        return;
                    }

                    result = await _api.SendTextAsync(thread.Users.Select(x => x.Pk),
                        null, content);
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                //await HandleException("Failed to send message");
                return;
            }

            if (result.IsSucceeded && result.Value.Length > 0)
            {
                // SyncClient will take care of updating. Update here is just for precaution.
                thread.Update(result.Value[0]);
                // await Inbox.UpdateInbox();
            }
        }

        public async Task SendAnimatedImage(DirectThreadWrapper thread, string imageId, bool isSticker)
        {
            try
            {
                if (string.IsNullOrEmpty(thread?.ThreadId)) return;
                var result = await _api.SendAnimatedImageAsync(imageId, isSticker, thread.ThreadId);
                if (result.IsSucceeded && result.Value.Length > 0)
                {
                    thread.Update(result.Value[0]);
                }
            }
            catch (Exception)
            {
                //await HandleException("Failed to send GIF");
            }
        }

        public async Task SendLike(DirectThreadWrapper thread)
        {
            try
            {
                if (string.IsNullOrEmpty(thread.ThreadId)) return;
                var result = await _api.SendLikeAsync(thread.ThreadId);
                //if (result.IsSucceeded) UpdateInboxAndSelectedThread();
            }
            catch (Exception)
            {
                //await HandleException("Failed to send like");
            }
        }

        public async Task Unsend(DirectItemWrapper item)
        {
            var result = await _api.UnsendMessageAsync(item.Parent.ThreadId, item.ItemId);
            if (result.IsSucceeded)
            {
                await item.Parent.RemoveItem(item.ItemId);
            }
        }

        public async Task SendFile(DirectThreadWrapper thread, StorageFile file, Action<UploaderProgress> progress)
        {
            try
            {
                if (file.ContentType.Contains("image", StringComparison.OrdinalIgnoreCase))
                {
                    var properties = await file.Properties.GetImagePropertiesAsync();
                    int imageHeight = (int)properties.Height;
                    int imageWidth = (int)properties.Width;
                    IBuffer buffer;
                    if (properties.Width > 1080 || properties.Height > 1080)
                    {
                        buffer = await Helpers.CompressImage(file, 1080, 1080);
                        double widthRatio = (double)1080 / imageWidth;
                        double heightRatio = (double)1080 / imageHeight;
                        double scaleRatio = Math.Min(widthRatio, heightRatio);
                        imageHeight = (int)Math.Floor(imageHeight * scaleRatio);
                        imageWidth = (int)Math.Floor(imageWidth * scaleRatio);
                    }
                    else
                    {
                        if (file.FileType.Contains("png", StringComparison.OrdinalIgnoreCase))
                            buffer = await Helpers.CompressImage(file, imageWidth, imageHeight);
                        else
                            buffer = await FileIO.ReadBufferAsync(file);
                    }

                    await SendBuffer(thread, buffer, imageWidth, imageHeight, progress);
                }


                // Not yet tested
                if (file.ContentType.Contains("video", StringComparison.OrdinalIgnoreCase))
                {
                    var properties = await file.Properties.GetVideoPropertiesAsync();
                    if (properties.Duration > TimeSpan.FromMinutes(1)) return;
                    var buffer = await FileIO.ReadBufferAsync(file);
                    var instaVideo = new InstaVideo()
                    {
                        UploadBuffer = buffer,
                        Width = (int)properties.Width,
                        Height = (int)properties.Height,
                    };
                    var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.VideosView);
                    var thumbnailBuffer = new Windows.Storage.Streams.Buffer((uint)thumbnail.Size);
                    await thumbnail.ReadAsync(thumbnailBuffer, (uint)thumbnail.Size, InputStreamOptions.None);
                    var thumbnailImage = new InstaImage()
                    {
                        UploadBuffer = thumbnailBuffer,
                        Width = (int)thumbnail.OriginalWidth,
                        Height = (int)thumbnail.OriginalHeight
                    };
                    await _api.SendDirectVideoAsync(progress,
                        new InstaVideoUpload(instaVideo, thumbnailImage), thread.ThreadId);
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                //await HandleException("Failed to send message");
            }
        }


        /// <summary>
        /// For screenshot in clipboard
        /// </summary>
        public async Task SendStream(DirectThreadWrapper thread, IRandomAccessStream stream, Action<UploaderProgress> progress)
        {
            try
            {
                stream.Seek(0);
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                int imageHeight = bitmap.PixelHeight;
                int imageWidth = bitmap.PixelWidth;

                IBuffer buffer;
                if (imageWidth > 1080 || imageHeight > 1080)
                {
                    buffer = await Helpers.CompressImage(stream, 1080, 1080);
                    double widthRatio = (double)1080 / imageWidth;
                    double heightRatio = (double)1080 / imageHeight;
                    double scaleRatio = Math.Min(widthRatio, heightRatio);
                    imageHeight = (int)Math.Floor(imageHeight * scaleRatio);
                    imageWidth = (int)Math.Floor(imageWidth * scaleRatio);
                }
                else
                {
                    stream.Seek(0);
                    buffer = await Helpers.CompressImage(stream, imageWidth, imageHeight);  // Force jpeg
                }

                await SendBuffer(thread, buffer, imageWidth, imageHeight, progress);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                //await HandleException("Failed to send message");
            }
        }

        private async Task SendBuffer(DirectThreadWrapper thread, IBuffer buffer, int imageWidth, int imageHeight, Action<UploaderProgress> progress)
        {
            var instaImage = new InstaImage
            {
                UploadBuffer = buffer,
                Width = imageWidth,
                Height = imageHeight
            };
            if (string.IsNullOrEmpty(thread.ThreadId)) return;
            var uploadId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _api.SendDirectPhotoAsync(instaImage, thread.ThreadId, uploadId, progress);
        }
    }
}
