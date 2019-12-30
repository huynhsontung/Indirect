using System.Collections.Generic;
using System.Linq;
using InstaSharper.API;
using InstaSharper.Classes.Models.Direct;

namespace Indirect.Wrapper
{
    class InstaInboxMediaWrapper : InstaInboxMedia
    {
        private readonly IInstaApi _instaApi;

        public new List<InstaImageWrapper> Images { get; } = new List<InstaImageWrapper>();

        public new List<InstaVideoWrapper> Videos { get; } = new List<InstaVideoWrapper>();

        public InstaInboxMediaWrapper(InstaInboxMedia source, IInstaApi api)
        {
            _instaApi = api;
            OriginalWidth = source.OriginalWidth;
            OriginalHeight = source.OriginalHeight;
            MediaType = source.MediaType;
            Images.AddRange(source.Images.Select(x => new InstaImageWrapper(x, api)));
            Videos.AddRange(source.Videos.Select(x=> new InstaVideoWrapper(x, api)));
        }
    }
}
