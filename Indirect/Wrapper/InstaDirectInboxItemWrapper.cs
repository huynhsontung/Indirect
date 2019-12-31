using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Media.Core;
using Windows.UI.Xaml.Media.Imaging;
using InstaSharper.API;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Classes.Models.Media;

namespace Indirect.Wrapper
{
    class InstaDirectInboxItemWrapper : InstaDirectInboxItem
    {
        private readonly IInstaApi _instaApi;

        public new InstaDirectReactionsWrapper Reactions { get; set; }
        // public new InstaInboxMediaWrapper Media { get; set; }
        // public new InstaMediaWrapper MediaShare { get; set; }
        // public new InstaStoryShareWrapper StoryShare { get; set; }
        // public new InstaVisualMediaWrapper RavenMedia { get; set; }
        // public new InstaVisualMediaContainerWrapper VisualMedia { get; set; }
        public new InstaUserShortWrapper ProfileMedia { get; set; }
        // public new List<InstaMediaWrapper> ProfileMediasPreview { get; set; }
        // public new InstaMediaWrapper FelixShareMedia { get; set; }
        // public new InstaReelShareWrapper ReelShareMedia { get; set; }
        // public new InstaDirectBroadcastWrapper LiveViewerInvite { get; set; }

        public int PreviewMediaHeight
        {
            get
            {
                switch (ItemType)
                {
                    case InstaDirectThreadItemType.Media:
                        return GetPreviewImage(Media.Images)?.Height ?? 0;

                    case InstaDirectThreadItemType.RavenMedia when RavenMedia != null:
                        return GetPreviewImage(RavenMedia.Images)?.Height ?? 0;

                    case InstaDirectThreadItemType.RavenMedia when VisualMedia != null:
                        return GetPreviewImage(VisualMedia.Media.Images)?.Height ?? 0;

                    case InstaDirectThreadItemType.AnimatedMedia:
                        return AnimatedMedia.Media.Height;

                    default:
                        return 0;
                }
            }
        }

        public int PreviewMediaWidth
        {
            get
            {
                switch (ItemType)
                {
                    case InstaDirectThreadItemType.Media:
                        return GetPreviewImage(Media.Images)?.Width ?? 0;

                    case InstaDirectThreadItemType.RavenMedia when RavenMedia != null:
                        return GetPreviewImage(RavenMedia.Images)?.Width ?? 0;

                    case InstaDirectThreadItemType.RavenMedia when VisualMedia != null:
                        return GetPreviewImage(VisualMedia.Media.Images)?.Width ?? 0;

                    case InstaDirectThreadItemType.AnimatedMedia:
                        return AnimatedMedia.Media.Width;

                    default:
                        return 0;
                }
            }
        }

        public Uri PreviewImageUri
        {
            get
            {
                string url;
                switch (ItemType)
                {
                    case InstaDirectThreadItemType.Media:
                        url = GetPreviewImage(Media.Images)?.Url;
                        return url != null ? new Uri(url) : null;


                    case InstaDirectThreadItemType.RavenMedia when RavenMedia != null:
                        url = GetPreviewImage(RavenMedia.Images)?.Url;
                        return url != null ? new Uri(url) : null;

                    case InstaDirectThreadItemType.RavenMedia when VisualMedia != null:
                        url = GetPreviewImage(VisualMedia.Media.Images)?.Url;
                        return url != null ? new Uri(url) : null;

                    case InstaDirectThreadItemType.AnimatedMedia:
                        return new Uri(AnimatedMedia.Media.Url);

                    default:
                        return null;
                }
            }
        }

        public Uri FullImageUri
        {
            get
            {
                switch (ItemType)
                {
                    case InstaDirectThreadItemType.Media:
                        return GetFullImageUri(Media.Images, Media.OriginalWidth, Media.OriginalHeight);

                    case InstaDirectThreadItemType.RavenMedia when RavenMedia != null:
                        return GetFullImageUri(RavenMedia.Images, RavenMedia.Width, RavenMedia.Height);

                    case InstaDirectThreadItemType.RavenMedia when VisualMedia != null:
                        return GetFullImageUri(VisualMedia.Media.Images, VisualMedia.Media.Width, VisualMedia.Media.Height);

                    case InstaDirectThreadItemType.AnimatedMedia:
                        return PreviewImageUri;

                    default:
                        return null;
                }
            }
        }

        public Uri VideoUri
        {
            get
            {
                switch (ItemType)
                {
                    case InstaDirectThreadItemType.Media:
                        return new Uri(Media.Videos.FirstOrDefault()?.Url);
        
                    case InstaDirectThreadItemType.RavenMedia when RavenMedia != null:
                        return new Uri(RavenMedia.Videos.FirstOrDefault()?.Url);
        
                    case InstaDirectThreadItemType.RavenMedia when VisualMedia != null:
                        return new Uri(VisualMedia.Media.Videos.FirstOrDefault()?.Url);
        
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
            Media = source.Media;
            MediaShare = source.MediaShare;
            ClientContext = source.ClientContext;
            StoryShare = source.StoryShare;
            RavenMedia = source.RavenMedia;
            VisualMedia = source.VisualMedia;
            RavenViewMode = source.RavenViewMode;
            RavenSeenUserIds = source.RavenSeenUserIds;
            RavenReplayChainCount = source.RavenReplayChainCount;
            RavenSeenCount = source.RavenSeenCount;
            RavenExpiringMediaActionSummary = source.RavenExpiringMediaActionSummary;
            ActionLog = source.ActionLog;
            ProfileMedia = source.ProfileMedia != null ? new InstaUserShortWrapper(source.ProfileMedia, api) : null;
            ProfileMediasPreview = source.ProfileMediasPreview;
            Placeholder = source.Placeholder;
            LinkMedia = source.LinkMedia;
            LocationMedia = source.LocationMedia;
            FelixShareMedia = source.FelixShareMedia;
            ReelShareMedia = source.ReelShareMedia;
            VoiceMedia = source.VoiceMedia; // todo: investigate whether voice received in single request
            AnimatedMedia = source.AnimatedMedia;
            HashtagMedia = source.HashtagMedia;
            LiveViewerInvite = source.LiveViewerInvite;
            FromMe = source.FromMe;
        }

        private static InstaImage GetPreviewImage(List<InstaImage> imageCandidates)
        {
            if (imageCandidates == null || imageCandidates.Count == 0) return null;
            var image = imageCandidates.OrderBy(x => x.Height + x.Width).First();
            return image;
        }

        private static InstaImage GetFullImage(List<InstaImage> imageCandidates, int originalWidth, int originalHeight)
        {
            if (imageCandidates == null || imageCandidates.Count == 0) return null;
            var image = imageCandidates.Single(x => x.Width == originalWidth && x.Height == originalHeight);
            return image;
        }

        private static Uri GetFullImageUri(List<InstaImage> imageCandidates, int originalWidth, int originalHeight)
        {
            if (imageCandidates == null || imageCandidates.Count == 0) return null;
            var image = imageCandidates.Single(x => x.Width == originalWidth && x.Height == originalHeight);
            return new Uri(image.Url);
        }
    }
}
