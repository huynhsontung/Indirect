using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Indirect.Utilities;
using InstagramAPI;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Media;
using InstagramAPI.Classes.User;

namespace Indirect.Wrapper
{
    class InstaDirectInboxItemWrapper : DirectItem, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Instagram _instaApi;

        private readonly DirectItem _sourceItem;

        public InstaDirectInboxThreadWrapper Parent { get; }
        public new InstaDirectReactionsWrapper Reactions { get; }
        public BaseUser Sender { get; }

        private bool _showTimestampHeader;
        public bool ShowTimestampHeader
        {
            get => _showTimestampHeader;
            set
            {
                _showTimestampHeader = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowTimestampHeader)));
            }
        }

        private bool _showNameHeader;
        public bool ShowNameHeader
        {
            get => _showNameHeader;
            set
            {
                _showNameHeader = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowNameHeader)));
            }
        }

        public Uri NavigateUri
        {
            get {
                switch (ItemType)
                {
                    case DirectItemType.Text when !string.IsNullOrEmpty(Text) && Text[0] == '#' && !Text.Contains(' '):
                        return new Uri("https://www.instagram.com/explore/tags/" + Text.Substring(1));

                    case DirectItemType.Link:
                        return Uri.TryCreate(Link.LinkContext.LinkUrl, UriKind.Absolute, out var uri) ? uri : null;

                    case DirectItemType.MediaShare:
                        return new Uri("https://www.instagram.com/p/" + MediaShare.Code);

                    case DirectItemType.Hashtag:
                        return new Uri("https://www.instagram.com/explore/tags/" + HashtagMedia.Name.ToLower(CultureInfo.CurrentCulture));

                    case DirectItemType.Profile:
                        return Profile.ProfileUrl;
                    
                    default:
                        return null;
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
                        return GetFullImage(ReelShareMedia.Media.Images)?.Height ?? 0;

                    case DirectItemType.StoryShare:
                        return GetFullImage(StoryShareMedia.Media?.Images)?.Height ?? 0;

                    case DirectItemType.AnimatedMedia:
                        return AnimatedMedia.Image.Height;

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
                        return GetFullImage(ReelShareMedia.Media.Images)?.Width ?? 0;

                    case DirectItemType.StoryShare:
                        return GetFullImage(StoryShareMedia.Media?.Images)?.Width ?? 0;

                    case DirectItemType.AnimatedMedia:
                        return AnimatedMedia.Image.Width;

                    default:
                        return 0;
                }
            }
        }

        public Uri PreviewImageUri
        {
            get
            {
                switch (ItemType)
                {
                    case DirectItemType.Media:
                        return GetPreviewImage(Media.Images)?.Url;

                    case DirectItemType.MediaShare when MediaShare.CarouselMedia?.Length > 0:
                        return GetPreviewImage(MediaShare.CarouselMedia[0].Images)?.Url;

                    case DirectItemType.MediaShare:
                        return GetPreviewImage(MediaShare.Images)?.Url;

                    case DirectItemType.RavenMedia when RavenMedia != null:
                        return GetPreviewImage(RavenMedia.Images)?.Url;

                    case DirectItemType.RavenMedia when VisualMedia != null:
                        return GetPreviewImage(VisualMedia.Media.Images)?.Url;

                    case DirectItemType.ReelShare:
                        return GetPreviewImage(ReelShareMedia.Media.Images)?.Url;

                    case DirectItemType.StoryShare:
                        return GetPreviewImage(StoryShareMedia.Media?.Images)?.Url;

                    case DirectItemType.AnimatedMedia:
                        return AnimatedMedia.Image.Url;

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

                    case DirectItemType.MediaShare when MediaShare.CarouselMedia?.Length > 0:
                        return GetFullImageUri(MediaShare.CarouselMedia[0].Images);

                    case DirectItemType.MediaShare:
                        return GetFullImageUri(MediaShare.Images);

                    case DirectItemType.RavenMedia when RavenMedia != null:
                        return GetFullImageUri(RavenMedia.Images);

                    case DirectItemType.RavenMedia when VisualMedia != null:
                        return GetFullImageUri(VisualMedia.Media.Images);

                    case DirectItemType.ReelShare:
                        return GetFullImageUri(ReelShareMedia.Media.Images);

                    case DirectItemType.StoryShare:
                        return GetFullImageUri(StoryShareMedia.Media?.Images);

                    case DirectItemType.AnimatedMedia:
                        return PreviewImageUri;

                    default:
                        return null;
                }
            }
        }

        public int VideoWidth
        {
            get
            {
                switch (ItemType)
                {
                    case DirectItemType.RavenMedia when RavenMedia != null:
                        return RavenMedia.OriginalWidth ?? 0;

                    case DirectItemType.RavenMedia when VisualMedia != null:
                        return VisualMedia.Media.OriginalWidth ?? 0;

                    case DirectItemType.Media when Media != null:
                        return Media.OriginalWidth ?? 0;

                    case DirectItemType.ReelShare when ReelShareMedia != null:
                        return ReelShareMedia.Media.OriginalWidth ?? 0;

                    case DirectItemType.StoryShare when StoryShareMedia != null:
                        return StoryShareMedia.Media?.OriginalWidth ?? 0;

                    default:
                        return 0;
                }
            }
        }

        public int VideoHeight
        {
            get
            {
                switch (ItemType)
                {
                    case DirectItemType.RavenMedia when RavenMedia != null:
                        return RavenMedia.OriginalHeight ?? 0;

                    case DirectItemType.RavenMedia when VisualMedia != null:
                        return VisualMedia.Media.OriginalHeight ?? 0;

                    case DirectItemType.Media when Media != null:
                        return Media.OriginalHeight ?? 0;

                    case DirectItemType.ReelShare when ReelShareMedia != null:
                        return ReelShareMedia.Media.OriginalHeight ?? 0;

                    case DirectItemType.StoryShare when StoryShareMedia != null:
                        return StoryShareMedia.Media?.OriginalHeight ?? 0;

                    default:
                        return 0;
                }
            }
        }

        public Uri VideoUri
        {
            get
            {
                switch (ItemType)
                {
                    case DirectItemType.Media when Media.Videos.Length > 0:
                        return Media.Videos[0].Url;

                    case DirectItemType.MediaShare when MediaShare.Videos.Length > 0:
                        return MediaShare.Videos[0].Url;

                    case DirectItemType.RavenMedia when RavenMedia != null && RavenMedia.Videos.Length > 0:
                        return RavenMedia.Videos[0].Url;
        
                    case DirectItemType.RavenMedia when VisualMedia != null && VisualMedia.Media.Videos.Length > 0:
                        return VisualMedia.Media.Videos[0].Url;

                    case DirectItemType.ReelShare:
                        return ReelShareMedia.Media?.VideoVersions[0].Url;

                    case DirectItemType.StoryShare:
                        return StoryShareMedia.Media?.VideoVersions[0].Url;

                    case DirectItemType.VoiceMedia:
                        return VoiceMedia.Media.Audio.AudioSrc;
        
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
            PropertyCopier<DirectItem, InstaDirectInboxItemWrapper>.Copy(source, this);
            Reactions = source.Reactions != null ? new InstaDirectReactionsWrapper(source.Reactions) : new InstaDirectReactionsWrapper();

            // Lookup BaseUser from user id
            var userExist = api.CentralUserRegistry.TryGetValue(UserId, out var sender);
            Sender = userExist
                ? sender
                : new BaseUser
                {
                    Username = "UNKNOWN_USER",
                    FullName = "UNKNOWN_USER"
                };
        }

        private static InstaImage GetPreviewImage(ICollection<InstaImage> imageCandidates)
        {
            if (imageCandidates == null || imageCandidates.Count == 0) return null;
            var candidates = imageCandidates.OrderBy(x => x.Height + x.Width).ToArray();
            var image = candidates.FirstOrDefault(x => x.Height != x.Width) ?? candidates[0];
            return image;
        }

        private static InstaImage GetFullImage(ICollection<InstaImage> imageCandidates)
        {
            if (imageCandidates == null || imageCandidates.Count == 0) return null;
            var candidates = imageCandidates.OrderByDescending(x => x.Height + x.Width).ToArray();
            var image = candidates.FirstOrDefault(x => x.Height != x.Width) ?? candidates[0];
            return image;
        }

        private static Uri GetFullImageUri(ICollection<InstaImage> imageCandidates)
        {
            return GetFullImage(imageCandidates)?.Url;
        }

        public async void LikeItem()
        {
            if (string.IsNullOrEmpty(Parent.ThreadId) || string.IsNullOrEmpty(ItemId)) return;
            await _instaApi.LikeItemAsync(Parent.ThreadId, ItemId).ConfigureAwait(false);
        }

        public async void UnlikeItem()
        {
            if (string.IsNullOrEmpty(Parent.ThreadId) || string.IsNullOrEmpty(ItemId)) return;
            await _instaApi.UnlikeItemAsync(Parent.ThreadId, ItemId).ConfigureAwait(false);
        }
    }
}
