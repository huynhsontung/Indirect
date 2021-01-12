using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using Windows.UI.Xaml;
using Indirect.Utilities;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Media;
using InstagramAPI.Classes.User;

namespace Indirect.Entities.Wrappers
{
    class DirectItemWrapper : DirectItem, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly MainViewModel _viewModel;
        private bool _showTimestampHeader;
        private bool _showNameHeader;

        public DirectItem Item { get; }
        public DirectThreadWrapper Parent { get; }
        public ReactionsWrapper ObservableReactions { get; }
        public BaseUser Sender { get; }
        public DirectItemWrapper RepliedItem { get; }

        public bool ShowTimestampHeader
        {
            get => _showTimestampHeader;
            set
            {
                _showTimestampHeader = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowTimestampHeader)));
            }
        }

        public bool ShowNameHeader
        {
            get => _showNameHeader;
            set
            {
                _showNameHeader = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowNameHeader)));
            }
        }

        public bool IsReplyable => GetItemReplyable();

        public HorizontalAlignment HorizontalAlignment => GetHorizontalAlignment();

        public Uri NavigateUri => GetNavigateUri();

        public int FullImageHeight => GetFullImage()?.Height ?? 0;

        public int FullImageWidth => GetFullImage()?.Width ?? 0;

        public Uri PreviewImageUri => GetPreviewImageUri();

        public Uri FullImageUri => GetFullImageUri();

        public int VideoWidth => GetVideo()?.OriginalWidth ?? 0;

        public int VideoHeight => GetVideo()?.OriginalHeight ?? 0;

        public Uri VideoUri => GetVideoUri();

        public bool IsNavigateUriValid => NavigateUri?.IsAbsoluteUri ?? false;
        

        public DirectItemWrapper(MainViewModel viewModel, DirectItem source, DirectThreadWrapper parent)
        {
            Contract.Requires(viewModel != null);
            Contract.Requires(source != null);
            Contract.Requires(parent != null);
            
            _viewModel = viewModel;
            Item = source;
            Parent = parent;
            PropertyCopier<DirectItem, DirectItemWrapper>.Copy(source, this);
            ObservableReactions = source.Reactions != null ? new ReactionsWrapper(viewModel, source.Reactions, parent.Users) : new ReactionsWrapper(viewModel);

            if (source.RepliedToMessage != null)
            {
                RepliedItem = new DirectItemWrapper(_viewModel, source.RepliedToMessage, Parent);
            }

            // Lookup BaseUser from user id
            if (UserId == parent.ViewerId)
            {
                Sender = parent.Viewer ?? new BaseUser
                {
                    Username = "UNKNOWN_VIEWER",
                    FullName = "UNKNOWN_VIEWER",
                    Pk = parent.ViewerId
                };
            }
            else
            {
                Sender = parent.Users.FirstOrDefault(u => u.Pk == UserId) ?? new BaseUser
                {
                    Username = "UNKNOWN_USER",
                    FullName = "UNKNOWN_USER"
                };
            }
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

        private Uri GetNavigateUri()
        {
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

                case DirectItemType.Clip when !string.IsNullOrEmpty(Clip?.Clip?.Code):
                    return new Uri("https://www.instagram.com/p/" + Clip.Clip.Code);

                default:
                    return null;
            }
        }
        
        private InstaImage GetFullImage()
        {
            switch (ItemType)
            {
                case DirectItemType.Media:
                    return GetFullImage(Media.Images);

                case DirectItemType.RavenMedia when RavenMedia != null:
                    return GetFullImage(RavenMedia.Images);

                case DirectItemType.RavenMedia when VisualMedia != null:
                    return GetFullImage(VisualMedia.Media.Images);

                case DirectItemType.ReelShare:
                    return GetFullImage(ReelShareMedia.Media.Images);

                case DirectItemType.StoryShare:
                    return GetFullImage(StoryShareMedia.Media?.Images);

                case DirectItemType.AnimatedMedia:
                    return AnimatedMedia.Image;

                default:
                    return null;
            }
        }

        private Uri GetFullImageUri()
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

        private Uri GetPreviewImageUri()
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

                case DirectItemType.Clip:
                    return GetPreviewImage(Clip?.Clip?.Images)?.Url;

                default:
                    return null;
            }
        }

        private InstaMedia GetVideo()
        {
            switch (ItemType)
            {
                case DirectItemType.RavenMedia when RavenMedia != null:
                    return RavenMedia;

                case DirectItemType.RavenMedia when VisualMedia != null:
                    return VisualMedia.Media;

                case DirectItemType.Media when Media != null:
                    return Media;

                case DirectItemType.ReelShare when ReelShareMedia != null:
                    return ReelShareMedia.Media;

                case DirectItemType.StoryShare when StoryShareMedia != null:
                    return StoryShareMedia.Media;

                default:
                    return null;
            }
        }

        private Uri GetVideoUri()
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

        private HorizontalAlignment GetHorizontalAlignment()
        {
            if (ItemType == DirectItemType.ActionLog)
            {
                return HorizontalAlignment.Center;
            }

            return FromMe ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }

        private bool GetItemReplyable()
        {
            switch (ItemType)
            {
                case DirectItemType.Text:
                case DirectItemType.MediaShare:
                case DirectItemType.Like:
                case DirectItemType.Link:
                case DirectItemType.Media:
                    return true;
                
                case DirectItemType.Unknown:
                case DirectItemType.ReelShare:
                case DirectItemType.Placeholder:
                case DirectItemType.RavenMedia:
                case DirectItemType.StoryShare:
                case DirectItemType.ActionLog:
                case DirectItemType.Profile:
                case DirectItemType.Location:
                case DirectItemType.FelixShare:
                case DirectItemType.VoiceMedia:
                case DirectItemType.AnimatedMedia:
                case DirectItemType.Hashtag:
                case DirectItemType.LiveViewerInvite:
                case DirectItemType.VideoCallEvent:
                default:
                    return false;
            }
        }
    }
}
