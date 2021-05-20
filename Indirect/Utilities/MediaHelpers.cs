using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Web.Http;
using InstagramAPI.Classes.Media;

namespace Indirect.Utilities
{
    internal static class MediaHelpers
    {
        public static InstaImage GetPreviewImage(this ICollection<InstaImage> imageCandidates)
        {
            if (imageCandidates == null || imageCandidates.Count == 0) return null;
            var candidates = imageCandidates.OrderBy(x => x.Height + x.Width).ToArray();
            var image = candidates.FirstOrDefault(x => x.Height != x.Width) ?? candidates[0];
            return image;
        }

        public static InstaImage GetFullImage(this ICollection<InstaImage> imageCandidates)
        {
            if (imageCandidates == null || imageCandidates.Count == 0) return null;
            var candidates = imageCandidates.OrderByDescending(x => x.Height + x.Width).ToArray();
            var image = candidates.FirstOrDefault(x => x.Height != x.Width) ?? candidates[0];
            return image;
        }

        public static Uri GetFullImageUri(this ICollection<InstaImage> imageCandidates)
        {
            return GetFullImage(imageCandidates)?.Url;
        }

        public static Uri GetPreviewImageUri(this ICollection<InstaImage> imageCandidates)
        {
            return GetPreviewImage(imageCandidates)?.Url;
        }

        public static async Task DownloadMedia(Uri url)
        {
            var fileName = System.IO.Path.GetFileName(url.LocalPath);
            var extension = System.IO.Path.GetExtension(url.LocalPath);
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads,
                SuggestedFileName = fileName
            };
            savePicker.FileTypeChoices.Add("Media", new List<string> { extension });
            var saveFile = await savePicker.PickSaveFileAsync();
            if (saveFile != null)
            {
                CachedFileManager.DeferUpdates(saveFile);
                var client = new HttpClient();
                var response = await client.TryGetAsync(url);
                if (response.Succeeded)
                {
                    var content = await response.ResponseMessage.Content.ReadAsBufferAsync();
                    await FileIO.WriteBufferAsync(saveFile, content);
                }

                await CachedFileManager.CompleteUpdatesAsync(saveFile);
            }
        }
    }
}
