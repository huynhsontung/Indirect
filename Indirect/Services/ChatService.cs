using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
using InstagramAPI.Utils;
using InstagramAPI.Classes.Core;

namespace Indirect.Services
{
    internal class ChatService
    {
        private readonly MainViewModel _viewModel;
        private Instagram Api => _viewModel.InstaApi;

        public ChatService(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public async Task SendLink(DirectThreadWrapper thread, string text, List<string> links)
        {
            Contract.Requires(thread != null);
            Contract.Requires(!string.IsNullOrEmpty(text));
            Contract.Requires(links?.Count > 0);

            try
            {
                if (!string.IsNullOrEmpty(thread.ThreadId))
                {
                    await Api.SendLinkAsync(text, links, thread.ThreadId);
                }
                else
                {
                    await Api.SendLinkToRecipientsAsync(text, links,
                        thread.Users.Select(x => x.Pk).ToArray());
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
            }
        }

        public async Task<DirectThread> SendTextMessage(DirectThreadWrapper thread, string text)
        {
            Contract.Requires(thread != null);
            Contract.Requires(!string.IsNullOrEmpty(text));

            try
            {
                Result<DirectThread[]> result;
                if (!string.IsNullOrEmpty(thread.ThreadId))
                {
                    result = await Api.SendTextAsync(null, thread.ThreadId, text);
                }
                else
                {
                    result = await Api.SendTextAsync(thread.Users.Select(x => x.Pk),
                        null, text);
                }

                return result.Value?[0];
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                return null;
            }
        }

        public async Task ReplyToItem(DirectItemWrapper item, string message)
        {
            Contract.Requires(item != null);
            Contract.Requires(!string.IsNullOrEmpty(message));
            try
            {
                if (string.IsNullOrEmpty(item.Parent.ThreadId))
                {
                    return;
                } 
                await Api.ReplyToItemAsync(item.Source, item.Parent.ThreadId, message);
            }
            catch (Exception)
            {
                // pass
            }
        }

        public async Task SendAnimatedImage(DirectThreadWrapper thread, string imageId, bool isSticker)
        {
            try
            {
                if (string.IsNullOrEmpty(thread?.ThreadId)) return;
                var result = await Api.SendAnimatedImageAsync(imageId, isSticker, thread.ThreadId);
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
                var result = await Api.SendLikeAsync(thread.ThreadId);
                //if (result.IsSucceeded) UpdateInboxAndSelectedThread();
            }
            catch (Exception)
            {
                //await HandleException("Failed to send like");
            }
        }

        public async Task ReactToItem(DirectItemWrapper item, string emoji)
        {
            Contract.Requires(item != null);

            if (item.Source.ItemType == DirectItemType.ActionLog) return;
            
            try
            {
                await Api.LikeItemAsync(item.Parent.ThreadId, item.Source.ItemId, emoji);
            }
            catch (Exception)
            {
                // pass
            }
        }

        public async Task RemoveReactionToItem(DirectItemWrapper item)
        {
            Contract.Requires(item != null);

            try
            {
                await Api.UnlikeItemAsync(item.Parent.ThreadId, item.Source.ItemId);
            }
            catch (Exception)
            {
                // pass
            }
        }

        public async Task Unsend(DirectItemWrapper item)
        {
            var result = await Api.UnsendMessageAsync(item.Parent.ThreadId, item.Source.ItemId);
            if (result.IsSucceeded)
            {
                item.Parent.RemoveItem(item.Source.ItemId);
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
                    await Api.SendDirectVideoAsync(progress,
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
            await Api.SendDirectPhotoAsync(instaImage, thread.ThreadId, uploadId, progress);
        }
    }
}
