using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using InstagramAPI.Classes.Media;
using InstagramAPI.Utils;

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
            if (!url.IsAbsoluteUri) return;
            var fileName = System.IO.Path.GetFileName(url.LocalPath);
            var extension = System.IO.Path.GetExtension(url.LocalPath);
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(extension)) return;
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads,
                SuggestedFileName = fileName
            };

            savePicker.FileTypeChoices.Add("Media", new List<string> { extension });
            var saveFile = await savePicker.PickSaveFileAsync();
            if (saveFile != null)
            {
                try
                {
                    CachedFileManager.DeferUpdates(saveFile);
                    var response = await ((App) App.Current).ViewModel.InstaApi.HttpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsByteArrayAsync();
                        await FileIO.WriteBufferAsync(saveFile, content.AsBuffer());
                    }

                    await CachedFileManager.CompleteUpdatesAsync(saveFile);
                }
                catch (FileLoadException e)
                {
                    DebugLogger.LogException(e, false);
                }
                catch (Exception e)
                {
                    DebugLogger.LogException(e);
                }
            }
        }
    }
}
