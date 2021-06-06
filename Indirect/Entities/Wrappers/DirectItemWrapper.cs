using System;
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

        public string Description { get; set; }

        public bool FromMe { get; set; }

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

            Item = source;
            Parent = parent;
            PropertyCopier<DirectItem, DirectItemWrapper>.Copy(source, this);
            ObservableReactions = source.Reactions != null ? new ReactionsWrapper(viewModel, source.Reactions, parent.Users) : new ReactionsWrapper(viewModel);
            FromMe = viewModel.LoggedInUser?.Pk == source.UserId;
            SetDescriptionText();

            if (source.RepliedToMessage != null)
            {
                RepliedItem = new DirectItemWrapper(viewModel, source.RepliedToMessage, Parent);
            }

            // Lookup BaseUser from user id
            if (UserId == parent.Source.ViewerId)
            {
                Sender = parent.Viewer ?? new BaseUser
                {
                    Username = "UNKNOWN_VIEWER",
                    FullName = "UNKNOWN_VIEWER",
                    Pk = parent.Source.ViewerId
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
                case DirectItemType.Media when Media?.Images?.Length > 0:
                    return Media.Images.GetFullImageUri();

                case DirectItemType.MediaShare when MediaShare?.CarouselMedia?.Length > 0:
                    return MediaShare.CarouselMedia[0].Images.GetFullImageUri();

                case DirectItemType.MediaShare when MediaShare?.Images?.Length > 0:
                    return MediaShare.Images.GetFullImageUri();

                case DirectItemType.RavenMedia when RavenMedia?.Images?.Length > 0:
                    return RavenMedia.Images.GetFullImageUri();

                case DirectItemType.RavenMedia when VisualMedia?.Media?.Images?.Length > 0:
                    return VisualMedia.Media.Images.GetFullImageUri();

                case DirectItemType.ReelShare when ReelShareMedia?.Media?.Images?.Length > 0:
                    return ReelShareMedia.Media.Images.GetFullImageUri();

                case DirectItemType.StoryShare when StoryShareMedia?.Media?.Images?.Length > 0:
                    return StoryShareMedia.Media.Images.GetFullImageUri();

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
                case DirectItemType.Media when Media?.Images?.Length > 0:
                    return Media.Images.GetPreviewImageUri();

                case DirectItemType.MediaShare when MediaShare?.CarouselMedia?.Length > 0:
                    return MediaShare.CarouselMedia[0].Images.GetPreviewImageUri();

                case DirectItemType.MediaShare when MediaShare?.Images?.Length > 0:
                    return MediaShare.Images.GetPreviewImageUri();

                case DirectItemType.RavenMedia when RavenMedia?.Images?.Length > 0:
                    return RavenMedia.Images.GetPreviewImageUri();

                case DirectItemType.RavenMedia when VisualMedia?.Media?.Images?.Length > 0:
                    return VisualMedia.Media.Images.GetPreviewImageUri();

                case DirectItemType.ReelShare when ReelShareMedia?.Media?.Images?.Length > 0:
                    return ReelShareMedia.Media.Images.GetPreviewImageUri();

                case DirectItemType.StoryShare when StoryShareMedia?.Media?.Images?.Length > 0:
                    return StoryShareMedia.Media.Images.GetPreviewImageUri();

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
                case DirectItemType.Media when Media?.Videos?.Length > 0:
                    return Media.Videos[0].Url;

                case DirectItemType.MediaShare when MediaShare?.Videos?.Length > 0:
                    return MediaShare.Videos[0].Url;

                case DirectItemType.RavenMedia when RavenMedia?.Videos?.Length > 0:
                    return RavenMedia.Videos[0].Url;

                case DirectItemType.RavenMedia when VisualMedia?.Media?.Videos?.Length > 0:
                    return VisualMedia.Media.Videos[0].Url;

                case DirectItemType.ReelShare when ReelShareMedia?.Media?.Videos?.Length > 0:
                    return ReelShareMedia.Media.Videos[0].Url;

                case DirectItemType.StoryShare when StoryShareMedia?.Media?.Videos?.Length > 0:
                    return StoryShareMedia.Media.Videos[0].Url;

                case DirectItemType.VoiceMedia when VoiceMedia?.Media?.Audio?.AudioSrc != null:
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

        private void SetDescriptionText()
        {
            try
            {
                switch (ItemType)
                {
                    case DirectItemType.ActionLog:
                        Description = ActionLog.Description;
                        break;

                    case DirectItemType.AnimatedMedia:
                        Description = FromMe ? "You sent a GIF" : "Sent you a GIF";
                        break;

                    case DirectItemType.Hashtag:
                        Description = "#" + HashtagMedia.Name;
                        break;

                    case DirectItemType.Like:
                        Description = Like;
                        break;

                    case DirectItemType.Link:
                        Description = Link.Text;
                        break;

                    case DirectItemType.Media:
                        if (Media.MediaType == InstaMediaType.Image)
                            Description = FromMe ? "You sent a photo" : "Sent you a photo";
                        else
                            Description = FromMe ? "You sent a video" : "Sent you a video";
                        break;

                    case DirectItemType.MediaShare:
                        Description = FromMe ? "You shared a post" : "Shared a post";
                        break;

                    case DirectItemType.RavenMedia:
                        var mediaType = RavenMedia?.MediaType ??
                                        VisualMedia.Media.MediaType;
                        if (mediaType == InstaMediaType.Image)
                            Description = FromMe ? "You sent a photo" : "Sent you a photo";
                        else
                            Description = FromMe ? "You sent a video" : "Sent you a video";
                        break;

                    case DirectItemType.ReelShare:
                        switch (ReelShareMedia.Type)
                        {
                            case "reaction":
                                Description = FromMe
                                    ? $"You reacted to their story {ReelShareMedia.Text}"
                                    : $"Reacted to your story {ReelShareMedia.Text}";
                                break;
                            case "reply":
                                Description = FromMe ? "You replied to their story" : "Replied to your story";
                                break;
                            case "mention":
                                Description = FromMe
                                    ? "You mentioned them in your story"
                                    : "Mentioned you in their story";
                                break;
                        }

                        break;

                    case DirectItemType.StoryShare:
                        Description = FromMe ? "You sent a story" : "Sent you a story";
                        break;

                    case DirectItemType.Text:
                        Description = Text;
                        break;

                    case DirectItemType.VoiceMedia:
                        Description = FromMe ? "You sent a voice clip" : "Sent you a voice clip";
                        break;

                    case DirectItemType.VideoCallEvent:
                        if (VideoCallEvent?.Action == "video_call_started")
                        {
                            Description = FromMe ? "You started a video chat" : "Video chat started";
                        }
                        else
                        {
                            Description = "Video chat ended";
                        }
                        break;

                    case DirectItemType.Profile:
                        Description = FromMe ? "You sent a profile" : "Sent a profile";
                        break;

                    default:
                        Description = ItemType.ToString();
                        break;
                }
            }
            catch (Exception)
            {
                // pass
            }
        }
    }
}
