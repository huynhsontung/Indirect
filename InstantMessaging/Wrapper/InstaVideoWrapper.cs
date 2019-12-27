using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstaSharper.API;
using InstaSharper.Classes.Models.Media;

namespace InstantMessaging.Wrapper
{
    class InstaVideoWrapper : InstaVideo
    {
        // todo: finish VideoWrapper
        private readonly IInstaApi _instaApi;
        public InstaVideoWrapper(InstaVideo source, IInstaApi api)
        {
            _instaApi = api;

        }
    }
}
