using System.Collections.Generic;
using System.Linq;
using InstaSharper.API;
using InstaSharper.Classes.Models.Direct;

namespace Indirect.Wrapper
{
    class InstaVisualMediaWrapper : InstaVisualMedia
    {
        public new List<InstaImageWrapper> Images { get; set; } = new List<InstaImageWrapper>(2);

        public new List<InstaVideoWrapper> Videos { get; set; } = new List<InstaVideoWrapper>(2);

        public InstaVisualMediaWrapper(InstaVisualMedia source, InstaApi api)
        {
            MediaId = source.MediaId;
            InstaIdentifier = source.InstaIdentifier;
            TrackingToken = source.TrackingToken;
            Width = source.Width;
            Height = source.Height;
            UrlExpireAt = source.UrlExpireAt;
            MediaType = source.MediaType; 
            Images.AddRange(source.Images.Select(x => new InstaImageWrapper(x, api)));
            Videos.AddRange(source.Videos.Select(x => new InstaVideoWrapper(x, api)));
        }
    }
}
