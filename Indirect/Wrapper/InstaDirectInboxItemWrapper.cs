using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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
        public InstaUser Sender { get; }

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
                        return GetPreviewImage(ReelShareMedia.Media.Images)?.Height ?? 0;

                    case DirectItemType.AnimatedMedia:
                        return AnimatedMedia.Image.Height;

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
                        return GetPreviewImage(ReelShareMedia.Media.Images)?.Width ?? 0;

                    case DirectItemType.AnimatedMedia:
                        return AnimatedMedia.Image.Width;

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
                        return GetFullImage(ReelShareMedia.Media.Images)?.Height ?? 0;

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

                    case DirectItemType.MediaShare:
                        return GetPreviewImage(MediaShare.Images)?.Url;

                    case DirectItemType.RavenMedia when RavenMedia != null:
                        return GetPreviewImage(RavenMedia.Images)?.Url;

                    case DirectItemType.RavenMedia when VisualMedia != null:
                        return GetPreviewImage(VisualMedia.Media.Images)?.Url;

                    case DirectItemType.ReelShare:
                        return GetPreviewImage(ReelShareMedia.Media.Images)?.Url;

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

                    case DirectItemType.MediaShare:
                        return GetFullImageUri(MediaShare.Images);

                    case DirectItemType.RavenMedia when RavenMedia != null:
                        return GetFullImageUri(RavenMedia.Images);

                    case DirectItemType.RavenMedia when VisualMedia != null:
                        return GetFullImageUri(VisualMedia.Media.Images);

                    case DirectItemType.ReelShare:
                        return GetFullImageUri(ReelShareMedia.Media.Images);

                    case DirectItemType.AnimatedMedia:
                        return PreviewImageUri;

                    default:
                        return null;
                }
            }
        }

        public int VideoWidth => (int) (RavenMedia?.Width ?? VisualMedia?.Media.Width ?? Media?.OriginalWidth ?? ReelShareMedia.Media.OriginalWidth ?? 0);
        public int VideoHeight => (int) (RavenMedia?.Height ?? VisualMedia?.Media?.Height ?? Media?.OriginalHeight ?? ReelShareMedia.Media.OriginalHeight ?? 0);

        public Uri VideoUri
        {
            get
            {
                switch (ItemType)
                {
                    case DirectItemType.Media when Media.Videos.Count > 0:
                        return Media.Videos.First().Url;

                    case DirectItemType.MediaShare when MediaShare.Videos.Count > 0:
                        return MediaShare.Videos.First().Url;

                    case DirectItemType.RavenMedia when RavenMedia != null && RavenMedia.Videos.Count > 0:
                        return RavenMedia.Videos.First().Url;
        
                    case DirectItemType.RavenMedia when VisualMedia != null && VisualMedia.Media.Videos.Count > 0:
                        return VisualMedia.Media.Videos.First().Url;

                    case DirectItemType.ReelShare:
                        return ReelShareMedia.Media.VideoVersions.First().Url;

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
            RawJson = source.RawJson;
            Description = source.Description;
            UserId = source.UserId;
            Timestamp = source.Timestamp;
            ItemId = source.ItemId;
            ItemType = source.ItemType;
            Reactions = source.Reactions != null ? new InstaDirectReactionsWrapper(source.Reactions, parent.ViewerId) : new InstaDirectReactionsWrapper();
            Like = source.Like;
            Link = source.Link;
            Media = source.Media;
            MediaShare = source.MediaShare;
            RavenMedia = source.RavenMedia;
            VisualMedia = source.VisualMedia;
            ActionLog = source.ActionLog;
            ReelShareMedia = source.ReelShareMedia;
            VoiceMedia = source.VoiceMedia;
            AnimatedMedia = source.AnimatedMedia;
            HashtagMedia = source.HashtagMedia;
            Text = source.Text;
            ClientContext = source.ClientContext;
            FromMe = source.FromMe;

            // Lookup InstaUser from user id
            var userExist = api.CentralUserRegistry.TryGetValue(UserId, out var sender);
            Sender = userExist
                ? sender
                : new InstaUser
                {
                    Username = "UNKNOWN_USER",
                    FullName = "UNKNOWN_USER"
                };
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
            return image.Url;
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
