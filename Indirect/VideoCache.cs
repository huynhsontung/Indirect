using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;
using Microsoft.Toolkit.Uwp.UI;

namespace Indirect
{
    class VideoCache : CacheBase<MediaSource>
    {
        private static VideoCache _instance;
        public static VideoCache Instance => _instance ?? (_instance = new VideoCache());

        private VideoCache() { }
        
        protected override Task<MediaSource> InitializeTypeAsync(Stream stream, List<KeyValuePair<string, object>> initializerKeyValues = null)
        {
            return Task.Run(() => MediaSource.CreateFromStream(stream.AsRandomAccessStream(), "video/mp4"));
        }

        protected override Task<MediaSource> InitializeTypeAsync(StorageFile baseFile, List<KeyValuePair<string, object>> initializerKeyValues = null)
        {
            return Task.Run(() => MediaSource.CreateFromStorageFile(baseFile));
        }
    }
}
