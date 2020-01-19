using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Media.Core;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using InstaSharper.API;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Classes.Models.Media;

namespace Indirect.Wrapper
{
    class InstaDirectInboxItemWrapper : InstaDirectInboxItem
    {
        private readonly InstaApi _instaApi;

        public InstaDirectInboxThreadWrapper Parent { get; }
        public new InstaDirectReactionsWrapper Reactions { get; }
        // public new InstaInboxMediaWrapper Media { get; set; }
        // public new InstaMediaWrapper MediaShare { get; set; }
        // public new InstaStoryShareWrapper StoryShare { get; set; }
        // public new InstaVisualMediaWrapper RavenMedia { get; set; }
        // public new InstaVisualMediaContainerWrapper VisualMedia { get; set; }
        // public new InstaUserShortWrapper ProfileMedia { get; set; }
        // public new List<InstaMediaWrapper> ProfileMediasPreview { get; set; }
        // public new InstaMediaWrapper FelixShareMedia { get; set; }
        // public new InstaReelShareWrapper ReelShareMedia { get; set; }
        // public new InstaDirectBroadcastWrapper LiveViewerInvite { get; set; }

        public Uri NavigateUri
        {
            get {
                switch (ItemType)
                {
                    case InstaDirectThreadItemType.Text when !string.IsNullOrEmpty(Text) && Text[0] == '#' && !Text.Contains(' '):
                        return new Uri("https://www.instagram.com/explore/tags/" + Text.Substring(1));

                    case InstaDirectThreadItemType.Link:
                        return Uri.TryCreate(LinkMedia.LinkContext.LinkUrl, UriKind.Absolute, out var uri) ? uri : null;

                    case InstaDirectThreadItemType.MediaShare:
                        return new Uri("https://www.instagram.com/p/" + MediaShare.Code);

                    case InstaDirectThreadItemType.Hashtag:
                        return new Uri("https://www.instagram.com/explore/tags/" + HashtagMedia.Name.ToLower());
                    
                    default:
                        return null;
                }
            }
        }

        public int PreviewImageHeight
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

                    case InstaDirectThreadItemType.ReelShare:
                        return GetPreviewImage(ReelShareMedia.Media.ImageList)?.Height ?? 0;

                    case InstaDirectThreadItemType.AnimatedMedia:
                        return AnimatedMedia.Media.Height;

                    default:
                        return 0;
                }
            }
        }

        public int PreviewImageWidth
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

                    case InstaDirectThreadItemType.ReelShare:
                        return GetPreviewImage(ReelShareMedia.Media.ImageList)?.Width ?? 0;

                    case InstaDirectThreadItemType.AnimatedMedia:
                        return AnimatedMedia.Media.Width;

                    default:
                        return 0;
                }
            }
        }

        public int FullImageHeight
        {
            get
            {
                switch (ItemType)
                {
                    case InstaDirectThreadItemType.Media:
                        return GetFullImage(Media.Images)?.Height ?? 0;

                    case InstaDirectThreadItemType.RavenMedia when RavenMedia != null:
                        return GetFullImage(RavenMedia.Images)?.Height ?? 0;

                    case InstaDirectThreadItemType.RavenMedia when VisualMedia != null:
                        return GetFullImage(VisualMedia.Media.Images)?.Height ?? 0;

                    case InstaDirectThreadItemType.ReelShare:
                        return GetFullImage(ReelShareMedia.Media.ImageList)?.Height ?? 0;

                    case InstaDirectThreadItemType.AnimatedMedia:
                        return AnimatedMedia.Media.Height;

                    default:
                        return 0;
                }
            }
        }

        public int FullImageWidth
        {
            get
            {
                switch (ItemType)
                {
                    case InstaDirectThreadItemType.Media:
                        return GetFullImage(Media.Images)?.Width ?? 0;

                    case InstaDirectThreadItemType.RavenMedia when RavenMedia != null:
                        return GetFullImage(RavenMedia.Images)?.Width ?? 0;

                    case InstaDirectThreadItemType.RavenMedia when VisualMedia != null:
                        return GetFullImage(VisualMedia.Media.Images)?.Width ?? 0;

                    case InstaDirectThreadItemType.ReelShare:
                        return GetFullImage(ReelShareMedia.Media.ImageList)?.Width ?? 0;

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

                    case InstaDirectThreadItemType.MediaShare:
                        url = GetPreviewImage(MediaShare.Images)?.Url;
                        return url != null ? new Uri(url) : null;

                    case InstaDirectThreadItemType.RavenMedia when RavenMedia != null:
                        url = GetPreviewImage(RavenMedia.Images)?.Url;
                        return url != null ? new Uri(url) : null;

                    case InstaDirectThreadItemType.RavenMedia when VisualMedia != null:
                        url = GetPreviewImage(VisualMedia.Media.Images)?.Url;
                        return url != null ? new Uri(url) : null;

                    case InstaDirectThreadItemType.ReelShare:
                        url = GetPreviewImage(ReelShareMedia.Media.ImageList)?.Url;
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
                        return GetFullImageUri(Media.Images);

                    case InstaDirectThreadItemType.MediaShare:
                        return GetFullImageUri(MediaShare.Images);

                    case InstaDirectThreadItemType.RavenMedia when RavenMedia != null:
                        return GetFullImageUri(RavenMedia.Images);

                    case InstaDirectThreadItemType.RavenMedia when VisualMedia != null:
                        return GetFullImageUri(VisualMedia.Media.Images);

                    case InstaDirectThreadItemType.ReelShare:
                        return GetFullImageUri(ReelShareMedia.Media.ImageList);

                    case InstaDirectThreadItemType.AnimatedMedia:
                        return PreviewImageUri;

                    default:
                        return null;
                }
            }
        }

        public int VideoWidth => (int) (RavenMedia?.Width ?? VisualMedia?.Media?.Width ?? Media?.OriginalWidth ?? ReelShareMedia.Media.OriginalWidth);
        public int VideoHeight => (int) (RavenMedia?.Height ?? VisualMedia?.Media?.Height ?? Media?.OriginalHeight ?? ReelShareMedia.Media.OriginalHeight);

        public Uri VideoUri
        {
            get
            {
                switch (ItemType)
                {
                    case InstaDirectThreadItemType.Media when Media.Videos.Count > 0:
                        return new Uri(Media.Videos.First().Url);

                    case InstaDirectThreadItemType.MediaShare when MediaShare.Videos.Count > 0:
                        return new Uri(MediaShare.Videos.First().Url);

                    case InstaDirectThreadItemType.RavenMedia when RavenMedia != null && RavenMedia.Videos.Count > 0:
                        return new Uri(RavenMedia.Videos.First().Url);
        
                    case InstaDirectThreadItemType.RavenMedia when VisualMedia != null && VisualMedia.Media.Videos.Count > 0:
                        return new Uri(VisualMedia.Media.Videos.First().Url);

                    case InstaDirectThreadItemType.ReelShare:
                        return new Uri(ReelShareMedia.Media.VideoList.First().Url);

                    case InstaDirectThreadItemType.VoiceMedia:
                        return new Uri(VoiceMedia.Media.Audio.AudioSource);
        
                    default:
                        return null;
                }
            }
        }

        public bool IsNavigateUriValid => NavigateUri?.IsAbsoluteUri ?? false;
        

        public InstaDirectInboxItemWrapper(InstaDirectInboxItem source, InstaDirectInboxThreadWrapper parent, InstaApi api)
        {
            _instaApi = api;
            Parent = parent;
            Text = source.Text;
            UserId = source.UserId;
            TimeStamp = source.TimeStamp;
            ItemId = source.ItemId;
            ItemType = source.ItemType;
            Reactions = source.Reactions != null ? new InstaDirectReactionsWrapper(source.Reactions, parent.ViewerId) : new InstaDirectReactionsWrapper();
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
            ProfileMedia = source.ProfileMedia;
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

        private static InstaImage GetFullImage(List<InstaImage> imageCandidates)
        {
            if (imageCandidates == null || imageCandidates.Count == 0) return null;
            var image = imageCandidates.OrderByDescending(x => x.Height + x.Width).First();
            return image;
        }

        private static Uri GetFullImageUri(List<InstaImage> imageCandidates)
        {
            if (imageCandidates == null || imageCandidates.Count == 0) return null;
            var image = imageCandidates.OrderByDescending(x => x.Height + x.Width).First();
            return new Uri(image.Url);
        }

        public void LikeItem()
        {
            if (string.IsNullOrEmpty(Parent.ThreadId) || string.IsNullOrEmpty(ItemId)) return;
            _instaApi.MessagingProcessor.LikeItemAsync(Parent.ThreadId, ItemId);
        }

        public void UnlikeItem()
        {
            if (string.IsNullOrEmpty(Parent.ThreadId) || string.IsNullOrEmpty(ItemId)) return;
            _instaApi.MessagingProcessor.UnlikeItemAsync(Parent.ThreadId, ItemId);
        }
    }
}
