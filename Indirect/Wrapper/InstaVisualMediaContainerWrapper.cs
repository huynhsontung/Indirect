using InstaSharper.API;
using InstaSharper.Classes.Models.Direct;

namespace Indirect.Wrapper
{
    class InstaVisualMediaContainerWrapper : InstaVisualMediaContainer
    {
        public new InstaVisualMediaWrapper Media { get; set; }

        public InstaVisualMediaContainerWrapper(InstaVisualMediaContainer source, IInstaApi api)
        {
            UrlExpireAt = source.UrlExpireAt;
            Media = source.Media != null ? new InstaVisualMediaWrapper(source.Media, api) : null;
            SeenCount = source.SeenCount;
            ReplayExpiringAtUs = source.ReplayExpiringAtUs;
            ViewMode = source.ViewMode;
            SeenUserIds = source.SeenUserIds;
        }
    }
}
