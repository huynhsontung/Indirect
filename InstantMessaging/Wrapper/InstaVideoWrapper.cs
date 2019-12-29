using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Streaming.Adaptive;
using Windows.Storage.Streams;
using InstaSharper.API;
using InstaSharper.Classes.Models.Media;
using InstaSharper.Helpers;

namespace InstantMessaging.Wrapper
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
