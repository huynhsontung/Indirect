using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Indirect.Utilities;
using Indirect.Wrapper;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Media;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Utils;

namespace Indirect
{
    internal partial class ApiContainer
    {
        public async Task SendAnimatedImage(string imageId, bool isSticker)
        {
            try
            {
                var selectedThread = SelectedThread;
                if (string.IsNullOrEmpty(selectedThread?.ThreadId)) return;
                var result = await _instaApi.SendAnimatedImageAsync(imageId, isSticker, selectedThread.ThreadId);
                if (result.IsSucceeded && result.Value.Length > 0)
                {
                    selectedThread.Update(result.Value[0]);
                }
            }
            catch (Exception)
            {
                await HandleException("Failed to send GIF");
            }
        }

        public async Task SendLike()
        {
            try
            {
                var selectedThread = SelectedThread;
                if (string.IsNullOrEmpty(selectedThread.ThreadId)) return;
                var result = await _instaApi.SendLikeAsync(selectedThread.ThreadId);
                //if (result.IsSucceeded) UpdateInboxAndSelectedThread();
            }
            catch (Exception)
            {
                await HandleException("Failed to send like");
            }
        }

        // Send message to the current selected recipient
        public async Task SendMessage(string content)
        {
            var selectedThread = SelectedThread;
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
                if (!string.IsNullOrEmpty(selectedThread.ThreadId))
                {
                    if (links.Any())
                    {
                        ackResult = await _instaApi.SendLinkAsync(content, links, selectedThread.ThreadId);
                        return;
                    }

                    //result = await _instaApi.SendTextAsync(null, selectedThread.ThreadId, content);

                    await _instaApi.SyncClientX.SendDirectTextAsync(selectedThread.ThreadId, content);

                    return;
                }
                else
                {
                    if (links.Any())
                    {
                        ackResult = await _instaApi.SendLinkToRecipientsAsync(content, links,
                            selectedThread.Users.Select(x => x.Pk).ToArray());
                        return;
                    }

                    result = await _instaApi.SendTextAsync(selectedThread.Users.Select(x => x.Pk),
                        null, content);
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                await HandleException("Failed to send message");
                return;
            }

            if (result.IsSucceeded && result.Value.Length > 0)
            {
                // SyncClient will take care of updating. Update here is just for precaution.
                selectedThread.Update(result.Value[0]);
                // await Inbox.UpdateInbox();
            }
        }

        public async Task UnsendMessage(InstaDirectInboxItemWrapper item)
        {
            var result = await _instaApi.UnsendMessageAsync(item.Parent.ThreadId, item.ItemId);
            if (result.IsSucceeded)
            {
                // For redundancy. This should only run on UI thread.
                item.Parent.RemoveItem(item.ItemId);
            }
        }

        public async Task SendFile(StorageFile file, Action<UploaderProgress> progress)
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

                    await SendBuffer(buffer, imageWidth, imageHeight, progress);
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
                    await _instaApi.SendDirectVideoAsync(progress,
                        new InstaVideoUpload(instaVideo, thumbnailImage), SelectedThread.ThreadId);
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                await HandleException("Failed to send message");
            }
        }


        /// <summary>
        /// For screenshot in clipboard
        /// </summary>
        /// <param name="stream"></param>
        public async Task SendStream(IRandomAccessStream stream, Action<UploaderProgress> progress)
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

                await SendBuffer(buffer, imageWidth, imageHeight, progress);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                await HandleException("Failed to send message");
            }
        }

        private async Task SendBuffer(IBuffer buffer, int imageWidth, int imageHeight, Action<UploaderProgress> progress)
        {
            var instaImage = new InstaImage
            {
                UploadBuffer = buffer,
                Width = imageWidth,
                Height = imageHeight
            };
            if (string.IsNullOrEmpty(SelectedThread.ThreadId)) return;
            var uploadId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _instaApi.SendDirectPhotoAsync(instaImage, SelectedThread.ThreadId, uploadId, progress);
        }
    }
}
