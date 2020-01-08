using System.Collections.Generic;
using InstaSharper.API;
using InstaSharper.Classes.Models.Media;

namespace Indirect.Wrapper
{
    // Instagram post wrapper
    class InstaMediaWrapper : InstaMedia
    {
        private readonly InstaApi _instaApi;

        public new List<InstaImageWrapper> Images { get; } = new List<InstaImageWrapper>();

        public new List<InstaVideoWrapper> Videos { get; } = new List<InstaVideoWrapper>();

        public new InstaUserWrapper User { get; set; }

        public InstaMediaWrapper(InstaMedia source, InstaApi api)
        {
            _instaApi = api;
            // todo: finish MediaWrapper
        }
    }
}
