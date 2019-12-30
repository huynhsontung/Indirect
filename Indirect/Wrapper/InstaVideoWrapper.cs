using System;
using Windows.Media.Core;
using InstaSharper.API;
using InstaSharper.Classes.Models.Media;

namespace Indirect.Wrapper
{
    class InstaVideoWrapper : InstaVideo
    {
        public MediaSource Video { get; set; }

        private readonly IInstaApi _instaApi;
        public InstaVideoWrapper(InstaVideo source, IInstaApi api)
        {
            _instaApi = api;
            Url = source.Url;
            Width = source.Width;
            Height = source.Height;
            Type = source.Type;
            Length = source.Length;
            Id = source.Id;
            Video = MediaSource.CreateFromUri(new Uri(Url));
        }
    }
}
