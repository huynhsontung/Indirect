using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstaSharper.API;
using InstaSharper.Classes.Models.Direct;

namespace InstantMessaging.Wrapper
{
    class InstaVisualMediaContainerWrapper : InstaVisualMediaContainer
    {
        public new InstaVisualMediaWrapper Media { get; set; }

        public InstaVisualMediaContainerWrapper(InstaVisualMediaContainer source, IInstaApi api)
        {
            UrlExpireAt = source.UrlExpireAt;
            Media = new InstaVisualMediaWrapper(source.Media, api);
            SeenCount = source.SeenCount;
            ReplayExpiringAtUs = source.ReplayExpiringAtUs;
            ViewMode = source.ViewMode;
            SeenCount = source.SeenCount;
        }
    }
}
