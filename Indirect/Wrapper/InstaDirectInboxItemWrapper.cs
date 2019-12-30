using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Media.Core;
using Windows.UI.Xaml.Media.Imaging;
using InstaSharper.API;
using InstaSharper.Classes.Models.Direct;

namespace Indirect.Wrapper
{
    class InstaDirectInboxItemWrapper : InstaDirectInboxItem
    {
        private readonly IInstaApi _instaApi;

        public new InstaDirectReactionsWrapper Reactions { get; set; }
        public new InstaInboxMediaWrapper Media { get; set; }
        public new InstaMediaWrapper MediaShare { get; set; }
        public new InstaStoryShareWrapper StoryShare { get; set; }
        public new InstaVisualMediaWrapper RavenMedia { get; set; }
        public new InstaVisualMediaContainerWrapper VisualMedia { get; set; }
        public new InstaUserShortWrapper ProfileMedia { get; set; }
        public new List<InstaMediaWrapper> ProfileMediasPreview { get; set; }
        public new InstaMediaWrapper FelixShareMedia { get; set; }
        public new InstaReelShareWrapper ReelShareMedia { get; set; }
        public new InstaDirectBroadcastWrapper LiveViewerInvite { get; set; }

        private BitmapImage _localImage;
        public BitmapImage PreviewImage
        {
            get
            {
                switch (ItemType)
                {
                    case InstaDirectThreadItemType.Media:
                        return GetPreviewImage(Media?.Images);

                    case InstaDirectThreadItemType.RavenMedia when RavenMedia != null:
                        return GetPreviewImage(RavenMedia?.Images);

                    case InstaDirectThreadItemType.RavenMedia when VisualMedia != null:
                        return GetPreviewImage(VisualMedia?.Media?.Images);

                    case InstaDirectThreadItemType.AnimatedMedia:
                        if (_localImage != null) return _localImage;
                        _localImage = new BitmapImage(new Uri(AnimatedMedia.Media.Url));
                        _localImage.DecodePixelHeight = AnimatedMedia.Media.Height;
                        _localImage.DecodePixelWidth = AnimatedMedia.Media.Width;
                        return _localImage;

                    default:
                        return null;
                }
            }
        }

        public BitmapImage FullImage
        {
            get
            {
                switch (ItemType)
                {
                    case InstaDirectThreadItemType.Media:
                        return GetFullImage(Media.Images, Media.OriginalWidth, Media.OriginalHeight);

                    case InstaDirectThreadItemType.RavenMedia when RavenMedia != null:
                        return GetFullImage(RavenMedia.Images, RavenMedia.Width, RavenMedia.Height);

                    case InstaDirectThreadItemType.RavenMedia when VisualMedia != null:
                        return GetFullImage(VisualMedia.Media.Images, VisualMedia.Media.Width, VisualMedia.Media.Height);

                    case InstaDirectThreadItemType.AnimatedMedia:
                        return PreviewImage;

                    default:
                        return null;
                }
            }
        }

        private MediaSource _mediaSource;
        public MediaSource MediaSource
        {
            get
            {
                if (_mediaSource != null) return _mediaSource;
                switch (ItemType)
                {
                    case InstaDirectThreadItemType.Media:
                        return Media.Videos.FirstOrDefault()?.Video;
        
                    case InstaDirectThreadItemType.RavenMedia when RavenMedia != null:
                        return RavenMedia.Videos.FirstOrDefault()?.Video;
        
                    case InstaDirectThreadItemType.RavenMedia when VisualMedia != null:
                        return VisualMedia.Media.Videos.FirstOrDefault()?.Video;
        
                    default:
                        return null;
                }
            }
        }

        public InstaDirectInboxItemWrapper(InstaDirectInboxItem source, IInstaApi api)
        {
            _instaApi = api;
            Text = source.Text;
            UserId = source.UserId;
            TimeStamp = source.TimeStamp;
            ItemId = source.ItemId;
            ItemType = source.ItemType;
            Reactions = source.Reactions != null ? new InstaDirectReactionsWrapper(source.Reactions) : null;
            Media = source.Media != null ? new InstaInboxMediaWrapper(source.Media, api) : null;
            MediaShare = source.MediaShare != null ? new InstaMediaWrapper(source.MediaShare, api) : null;
            ClientContext = source.ClientContext;
            StoryShare = source.StoryShare != null ? new InstaStoryShareWrapper(source.StoryShare, api) : null;
            RavenMedia = source.RavenMedia != null ? new InstaVisualMediaWrapper(source.RavenMedia, api) : null;
            VisualMedia = source.VisualMedia != null ? new InstaVisualMediaContainerWrapper(source.VisualMedia, api) : null;
            RavenViewMode = source.RavenViewMode;
            RavenSeenUserIds = source.RavenSeenUserIds;
            RavenReplayChainCount = source.RavenReplayChainCount;
            RavenSeenCount = source.RavenSeenCount;
            RavenExpiringMediaActionSummary = source.RavenExpiringMediaActionSummary;
            ActionLog = source.ActionLog;
            ProfileMedia = source.ProfileMedia != null ? new InstaUserShortWrapper(source.ProfileMedia, api) : null;
            if (source.ProfileMediasPreview != null)
            {
                ProfileMediasPreview = new List<InstaMediaWrapper>();
                ProfileMediasPreview.AddRange(source.ProfileMediasPreview.Select(x => new InstaMediaWrapper(x, api)));
            }
            Placeholder = source.Placeholder;
            LinkMedia = source.LinkMedia;
            LocationMedia = source.LocationMedia;
            FelixShareMedia = source.FelixShareMedia != null ? new InstaMediaWrapper(source.FelixShareMedia, api) : null;
            ReelShareMedia = source.ReelShareMedia != null ? new InstaReelShareWrapper(source.ReelShareMedia, api) : null;
            VoiceMedia = source.VoiceMedia; // todo: investigate whether voice received in single request
            AnimatedMedia = source.AnimatedMedia;
            HashtagMedia = source.HashtagMedia;
            LiveViewerInvite = source.LiveViewerInvite != null ? new InstaDirectBroadcastWrapper(source.LiveViewerInvite, api) : null;
            FromMe = source.FromMe;
        }

        private static BitmapImage GetPreviewImage(List<InstaImageWrapper> imageCandidates)
        {
            if (imageCandidates == null || imageCandidates.Count == 0) return null;
            var image = imageCandidates.OrderBy(x => x.Height + x.Width).First();
            return image.Image;
        }

        private static BitmapImage GetFullImage(List<InstaImageWrapper> imageCandidates, int originalWidth, int originalHeight)
        {
            if (imageCandidates == null || imageCandidates.Count == 0) return null;
            var image = imageCandidates.Single(x => x.Width == originalWidth && x.Height == originalHeight);
            return image.Image;
        }
    }
}
