using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Windows.Media.Core;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using InstagramAPI;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Direct.ItemContent;
using InstagramAPI.Classes.Media;

namespace Indirect.Wrapper
{
    class InstaDirectInboxItemWrapper : DirectItem
    {
        private readonly Instagram _instaApi;

        private readonly DirectItem _sourceItem;

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
                    case DirectItemType.Text when !string.IsNullOrEmpty(MediaTypeNames.Text) && Text[0] == '#' && !MediaTypeNames.Text.Contains(' '):
                        return new Uri("https://www.instagram.com/explore/tags/" + MediaTypeNames.Text.Substring(1));

                    case DirectItemType.Link:
                        return Uri.TryCreate(LinkMedia.LinkContext.LinkUrl, UriKind.Absolute, out var uri) ? uri : null;

                    case DirectItemType.MediaShare:
                        return new Uri("https://www.instagram.com/p/" + MediaShare.Code);

                    case DirectItemType.Hashtag:
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
                    case DirectItemType.Media:
                        return GetPreviewImage(Media.Images)?.Height ?? 0;

                    case DirectItemType.RavenMedia when RavenMedia != null:
                        return GetPreviewImage(RavenMedia.Images)?.Height ?? 0;

                    case DirectItemType.RavenMedia when VisualMedia != null:
                        return GetPreviewImage(VisualMedia.Media.Images)?.Height ?? 0;

                    case DirectItemType.ReelShare:
                        return GetPreviewImage(ReelShareMedia.Media.ImageList)?.Height ?? 0;

                    case DirectItemType.AnimatedMedia:
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
                    case DirectItemType.Media:
                        return GetPreviewImage(Media.Images)?.Width ?? 0;

                    case DirectItemType.RavenMedia when RavenMedia != null:
                        return GetPreviewImage(RavenMedia.Images)?.Width ?? 0;

                    case DirectItemType.RavenMedia when VisualMedia != null:
                        return GetPreviewImage(VisualMedia.Media.Images)?.Width ?? 0;

                    case DirectItemType.ReelShare:
                        return GetPreviewImage(ReelShareMedia.Media.ImageList)?.Width ?? 0;

                    case DirectItemType.AnimatedMedia:
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
                    case DirectItemType.Media:
                        return GetFullImage(Media.Images)?.Height ?? 0;

                    case DirectItemType.RavenMedia when RavenMedia != null:
                        return GetFullImage(RavenMedia.Images)?.Height ?? 0;

                    case DirectItemType.RavenMedia when VisualMedia != null:
                        return GetFullImage(VisualMedia.Media.Images)?.Height ?? 0;

                    case DirectItemType.ReelShare:
                        return GetFullImage(ReelShareMedia.Media.ImageList)?.Height ?? 0;

                    case DirectItemType.AnimatedMedia:
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
                    case DirectItemType.Media:
                        return GetFullImage(Media.Images)?.Width ?? 0;

                    case DirectItemType.RavenMedia when RavenMedia != null:
                        return GetFullImage(RavenMedia.Images)?.Width ?? 0;

                    case DirectItemType.RavenMedia when VisualMedia != null:
                        return GetFullImage(VisualMedia.Media.Images)?.Width ?? 0;

                    case DirectItemType.ReelShare:
                        return GetFullImage(ReelShareMedia.Media.ImageList)?.Width ?? 0;

                    case DirectItemType.AnimatedMedia:
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
                    case DirectItemType.Media:
                        url = GetPreviewImage(Media.Images)?.Url;
                        return url != null ? new Uri(url) : null;

                    case DirectItemType.MediaShare:
                        url = GetPreviewImage(MediaShare.Images)?.Url;
                        return url != null ? new Uri(url) : null;

                    case DirectItemType.RavenMedia when RavenMedia != null:
                        url = GetPreviewImage(RavenMedia.Images)?.Url;
                        return url != null ? new Uri(url) : null;

                    case DirectItemType.RavenMedia when VisualMedia != null:
                        url = GetPreviewImage(VisualMedia.Media.Images)?.Url;
                        return url != null ? new Uri(url) : null;

                    case DirectItemType.ReelShare:
                        url = GetPreviewImage(ReelShareMedia.Media.ImageList)?.Url;
                        return url != null ? new Uri(url) : null;

                    case DirectItemType.AnimatedMedia:
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
                    case DirectItemType.Media:
                        return GetFullImageUri(Media.Images);

                    case DirectItemType.MediaShare:
                        return GetFullImageUri(MediaShare.Images);

                    case DirectItemType.RavenMedia when RavenMedia != null:
                        return GetFullImageUri(RavenMedia.Images);

                    case DirectItemType.RavenMedia when VisualMedia != null:
                        return GetFullImageUri(VisualMedia.Media.Images);

                    case DirectItemType.ReelShare:
                        return GetFullImageUri(ReelShareMedia.Media.ImageList);

                    case DirectItemType.AnimatedMedia:
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
                    case DirectItemType.Media when Media.Videos.Count > 0:
                        return new Uri(Media.Videos.First().Url);

                    case DirectItemType.MediaShare when MediaShare.Videos.Count > 0:
                        return new Uri(MediaShare.Videos.First().Url);

                    case DirectItemType.RavenMedia when RavenMedia != null && RavenMedia.Videos.Count > 0:
                        return new Uri(RavenMedia.Videos.First().Url);
        
                    case DirectItemType.RavenMedia when VisualMedia != null && VisualMedia.Media.Videos.Count > 0:
                        return new Uri(VisualMedia.Media.Videos.First().Url);

                    case DirectItemType.ReelShare:
                        return new Uri(ReelShareMedia.Media.VideoList.First().Url);

                    case DirectItemType.VoiceMedia:
                        return new Uri(VoiceMedia.Media.Audio.AudioSource);
        
                    default:
                        return null;
                }
            }
        }

        public bool IsNavigateUriValid => NavigateUri?.IsAbsoluteUri ?? false;
        

        public InstaDirectInboxItemWrapper(DirectItem source, InstaDirectInboxThreadWrapper parent, Instagram api)
        {
            _instaApi = api;
            _sourceItem = source;
            Parent = parent;
            UserId = source.UserId;
            Timestamp = source.Timestamp;
            ItemId = source.ItemId;
            ItemType = source.ItemType;
            Reactions = source.Reactions != null ? new InstaDirectReactionsWrapper(source.Reactions, parent.ViewerId) : new InstaDirectReactionsWrapper();
            ClientContext = source.ClientContext;
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
