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
                    return Media.Images.GetFullImage();

                case DirectItemType.RavenMedia when RavenMedia != null:
                    return RavenMedia.Images.GetFullImage();

                case DirectItemType.RavenMedia when VisualMedia != null:
                    return VisualMedia.Media.Images.GetFullImage();

                case DirectItemType.ReelShare:
                    return ReelShareMedia.Media.Images.GetFullImage();

                case DirectItemType.StoryShare:
                    return StoryShareMedia.Media?.Images.GetFullImage();

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
                    return Media.Images.GetFullImageUri();

                case DirectItemType.MediaShare when MediaShare.CarouselMedia?.Length > 0:
                    return MediaShare.CarouselMedia[0].Images.GetFullImageUri();

                case DirectItemType.MediaShare:
                    return MediaShare.Images.GetFullImageUri();

                case DirectItemType.RavenMedia when RavenMedia != null:
                    return RavenMedia.Images.GetFullImageUri();

                case DirectItemType.RavenMedia when VisualMedia != null:
                    return VisualMedia.Media.Images.GetFullImageUri();

                case DirectItemType.ReelShare:
                    return ReelShareMedia.Media.Images.GetFullImageUri();

                case DirectItemType.StoryShare:
                    return StoryShareMedia.Media?.Images.GetFullImageUri();

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
                    return Media.Images.GetPreviewImageUri();

                case DirectItemType.MediaShare when MediaShare.CarouselMedia?.Length > 0:
                    return MediaShare.CarouselMedia[0].Images.GetPreviewImageUri();

                case DirectItemType.MediaShare:
                    return MediaShare.Images.GetPreviewImageUri();

                case DirectItemType.RavenMedia when RavenMedia != null:
                    return RavenMedia.Images.GetPreviewImageUri();

                case DirectItemType.RavenMedia when VisualMedia != null:
                    return VisualMedia.Media.Images.GetPreviewImageUri();

                case DirectItemType.ReelShare:
                    return ReelShareMedia.Media.Images.GetPreviewImageUri();

                case DirectItemType.StoryShare:
                    return StoryShareMedia.Media?.Images.GetPreviewImageUri();

                case DirectItemType.AnimatedMedia:
                    return AnimatedMedia.Image.Url;

                case DirectItemType.Clip:
                    return Clip?.Clip?.Images.GetPreviewImageUri();

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
                    return ReelShareMedia.Media?.Videos[0].Url;

                case DirectItemType.StoryShare:
                    return StoryShareMedia.Media?.Videos[0].Url;

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
