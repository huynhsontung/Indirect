using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using Windows.UI.Xaml;
using Indirect.Utilities;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Direct.ItemContent;
using InstagramAPI.Classes.Media;
using InstagramAPI.Classes.User;
using NeoSmart.Unicode;

namespace Indirect.Entities.Wrappers
{
    internal class DirectItemWrapper : DependencyObject, IEquatable<DirectItemWrapper>
    {
        public static readonly DependencyProperty ShowTimestampHeaderProperty = DependencyProperty.Register(
            nameof(ShowTimestampHeader),
            typeof(string),
            typeof(DirectItemWrapper),
            new PropertyMetadata(false));

        public static readonly DependencyProperty ShowNameHeaderProperty = DependencyProperty.Register(
            nameof(ShowNameHeader),
            typeof(string),
            typeof(DirectItemWrapper),
            new PropertyMetadata(false));

        public DirectItem Source { get; }
        public DirectThreadWrapper Parent { get; }
        public ReactionsWrapper ObservableReactions { get; }
        public BaseUser Sender { get; }
        public DirectItemWrapper RepliedItem { get; }

        public bool ShowTimestampHeader
        {
            get => (bool)GetValue(ShowTimestampHeaderProperty);
            set => SetValue(ShowTimestampHeaderProperty, value);
        }

        public bool ShowNameHeader
        {
            get => (bool)GetValue(ShowNameHeaderProperty);
            set => SetValue(ShowNameHeaderProperty, value);
        }

        public string Description { get; set; }

        public bool FromMe { get; set; }

        public LinkTextForDisplay LinkText { get; }

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

            Source = source;
            Parent = parent;
            ObservableReactions = new ReactionsWrapper(viewModel, source.Reactions, parent.Users);
            FromMe = viewModel.LoggedInUser?.Pk == source.UserId;
            SetDescriptionText();
            LinkText = DeconstructLinkShare(Source.Link);
            CheckItemTextForEmoji(source);

            if (source.RepliedToMessage != null)
            {
                RepliedItem = new DirectItemWrapper(viewModel, source.RepliedToMessage, Parent);
            }

            // Lookup BaseUser from user id
            if (Source.UserId == parent.Source.ViewerId)
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
                Sender = parent.Users.FirstOrDefault(u => u.Pk == Source.UserId) ?? new BaseUser
                {
                    Username = "UNKNOWN_USER",
                    FullName = "UNKNOWN_USER"
                };
            }
        }

        public bool Equals(DirectItemWrapper other)
        {
            return !string.IsNullOrEmpty(Source.ItemId) && Source.ItemId == other?.Source.ItemId;
        }

        private Uri GetNavigateUri()
        {
            switch (Source.ItemType)
            {
                case DirectItemType.Text when !string.IsNullOrEmpty(Source.Text) && Source.Text[0] == '#' && !Source.Text.Contains(' '):
                    return new Uri("https://www.instagram.com/explore/tags/" + Source.Text.Substring(1));

                case DirectItemType.Link:
                    return new UriBuilder(Source.Link.LinkContext.LinkUrl).Uri;

                case DirectItemType.MediaShare:
                    return new Uri("https://www.instagram.com/p/" + Source.MediaShare.Code);

                case DirectItemType.Hashtag:
                    return new Uri("https://www.instagram.com/explore/tags/" + Source.HashtagMedia.Name.ToLower(CultureInfo.CurrentCulture));

                case DirectItemType.Profile:
                    return Source.Profile.ProfileUrl;

                case DirectItemType.Clip when !string.IsNullOrEmpty(Source.Clip?.Clip?.Code):
                    return new Uri("https://www.instagram.com/p/" + Source.Clip.Clip.Code);

                default:
                    return null;
            }
        }
        
        private InstaImage GetFullImage()
        {
            switch (Source.ItemType)
            {
                case DirectItemType.Media:
                    return Source.Media.Images.GetFullImage();

                case DirectItemType.RavenMedia when Source.RavenMedia != null:
                    return Source.RavenMedia.Images.GetFullImage();

                case DirectItemType.RavenMedia when Source.VisualMedia != null:
                    return Source.VisualMedia.Media.Images.GetFullImage();

                case DirectItemType.ReelShare:
                    return Source.ReelShareMedia.Media.Images.GetFullImage();

                case DirectItemType.StoryShare:
                    return Source.StoryShareMedia.Media?.Images.GetFullImage();

                case DirectItemType.AnimatedMedia:
                    return Source.AnimatedMedia.Image;

                default:
                    return null;
            }
        }

        private Uri GetFullImageUri()
        {
            switch (Source.ItemType)
            {
                case DirectItemType.Media when Source.Media?.Images?.Length > 0:
                    return Source.Media.Images.GetFullImageUri();

                case DirectItemType.MediaShare when Source.MediaShare?.CarouselMedia?.Length > 0:
                    return Source.MediaShare.CarouselMedia[0].Images.GetFullImageUri();

                case DirectItemType.MediaShare when Source.MediaShare?.Images?.Length > 0:
                    return Source.MediaShare.Images.GetFullImageUri();

                case DirectItemType.RavenMedia when Source.RavenMedia?.Images?.Length > 0:
                    return Source.RavenMedia.Images.GetFullImageUri();

                case DirectItemType.RavenMedia when Source.VisualMedia?.Media?.Images?.Length > 0:
                    return Source.VisualMedia.Media.Images.GetFullImageUri();

                case DirectItemType.ReelShare when Source.ReelShareMedia?.Media?.Images?.Length > 0:
                    return Source.ReelShareMedia.Media.Images.GetFullImageUri();

                case DirectItemType.StoryShare when Source.StoryShareMedia?.Media?.Images?.Length > 0:
                    return Source.StoryShareMedia.Media.Images.GetFullImageUri();

                case DirectItemType.AnimatedMedia:
                    return PreviewImageUri;

                default:
                    return null;
            }
        }

        private Uri GetPreviewImageUri()
        {
            switch (Source.ItemType)
            {
                case DirectItemType.Media when Source.Media?.Images?.Length > 0:
                    return Source.Media.Images.GetPreviewImageUri();

                case DirectItemType.MediaShare when Source.MediaShare?.CarouselMedia?.Length > 0:
                    return Source.MediaShare.CarouselMedia[0].Images.GetPreviewImageUri();

                case DirectItemType.MediaShare when Source.MediaShare?.Images?.Length > 0:
                    return Source.MediaShare.Images.GetPreviewImageUri();

                case DirectItemType.RavenMedia when Source.RavenMedia?.Images?.Length > 0:
                    return Source.RavenMedia.Images.GetPreviewImageUri();

                case DirectItemType.RavenMedia when Source.VisualMedia?.Media?.Images?.Length > 0:
                    return Source.VisualMedia.Media.Images.GetPreviewImageUri();

                case DirectItemType.ReelShare when Source.ReelShareMedia?.Media?.Images?.Length > 0:
                    return Source.ReelShareMedia.Media.Images.GetPreviewImageUri();

                case DirectItemType.StoryShare when Source.StoryShareMedia?.Media?.Images?.Length > 0:
                    return Source.StoryShareMedia.Media.Images.GetPreviewImageUri();

                case DirectItemType.AnimatedMedia:
                    return Source.AnimatedMedia.Image.Url;

                case DirectItemType.Clip:
                    return Source.Clip?.Clip?.Images.GetPreviewImageUri();

                default:
                    return null;
            }
        }

        private InstaMedia GetVideo()
        {
            switch (Source.ItemType)
            {
                case DirectItemType.RavenMedia when Source.RavenMedia != null:
                    return Source.RavenMedia;

                case DirectItemType.RavenMedia when Source.VisualMedia != null:
                    return Source.VisualMedia.Media;

                case DirectItemType.Media when Source.Media != null:
                    return Source.Media;

                case DirectItemType.ReelShare when Source.ReelShareMedia != null:
                    return Source.ReelShareMedia.Media;

                case DirectItemType.StoryShare when Source.StoryShareMedia != null:
                    return Source.StoryShareMedia.Media;

                default:
                    return null;
            }
        }

        private Uri GetVideoUri()
        {
            switch (Source.ItemType)
            {
                case DirectItemType.Media when Source.Media?.Videos?.Length > 0:
                    return Source.Media.Videos[0].Url;

                case DirectItemType.MediaShare when Source.MediaShare?.Videos?.Length > 0:
                    return Source.MediaShare.Videos[0].Url;

                case DirectItemType.RavenMedia when Source.RavenMedia?.Videos?.Length > 0:
                    return Source.RavenMedia.Videos[0].Url;

                case DirectItemType.RavenMedia when Source.VisualMedia?.Media?.Videos?.Length > 0:
                    return Source.VisualMedia.Media.Videos[0].Url;

                case DirectItemType.ReelShare when Source.ReelShareMedia?.Media?.Videos?.Length > 0:
                    return Source.ReelShareMedia.Media.Videos[0].Url;

                case DirectItemType.StoryShare when Source.StoryShareMedia?.Media?.Videos?.Length > 0:
                    return Source.StoryShareMedia.Media.Videos[0].Url;

                case DirectItemType.VoiceMedia when Source.VoiceMedia?.Media?.Audio?.AudioSrc != null:
                    return Source.VoiceMedia.Media.Audio.AudioSrc;

                default:
                    return null;
            }
        }

        private HorizontalAlignment GetHorizontalAlignment()
        {
            if (Source.ItemType == DirectItemType.ActionLog)
            {
                return HorizontalAlignment.Center;
            }

            return FromMe ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }

        private bool GetItemReplyable()
        {
            switch (Source.ItemType)
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
                switch (Source.ItemType)
                {
                    case DirectItemType.ActionLog:
                        Description = Source.ActionLog.Description;
                        break;

                    case DirectItemType.AnimatedMedia:
                        Description = FromMe ? "You sent a GIF" : "Sent you a GIF";
                        break;

                    case DirectItemType.Hashtag:
                        Description = "#" + Source.HashtagMedia.Name;
                        break;

                    case DirectItemType.Like:
                        Description = Source.Like;
                        break;

                    case DirectItemType.Link:
                        Description = Source.Link.Text;
                        break;

                    case DirectItemType.Media:
                        if (Source.Media.MediaType == InstaMediaType.Image)
                            Description = FromMe ? "You sent a photo" : "Sent you a photo";
                        else
                            Description = FromMe ? "You sent a video" : "Sent you a video";
                        break;

                    case DirectItemType.MediaShare:
                        Description = FromMe ? "You shared a post" : "Shared a post";
                        break;

                    case DirectItemType.RavenMedia:
                        var mediaType = Source.RavenMedia?.MediaType ?? Source.VisualMedia.Media.MediaType;
                        if (mediaType == InstaMediaType.Image)
                            Description = FromMe ? "You sent a photo" : "Sent you a photo";
                        else
                            Description = FromMe ? "You sent a video" : "Sent you a video";
                        break;

                    case DirectItemType.ReelShare:
                        switch (Source.ReelShareMedia.Type)
                        {
                            case "reaction":
                                Description = FromMe
                                    ? $"You reacted to their story {Source.ReelShareMedia.Text}"
                                    : $"Reacted to your story {Source.ReelShareMedia.Text}";
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
                        Description = Source.Text;
                        break;

                    case DirectItemType.VoiceMedia:
                        Description = FromMe ? "You sent a voice clip" : "Sent you a voice clip";
                        break;

                    case DirectItemType.VideoCallEvent:
                        if (Source.VideoCallEvent?.Action == "video_call_started")
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
                        Description = Source.ItemType.ToString();
                        break;
                }
            }
            catch (Exception)
            {
                // pass
            }
        }

        private static LinkTextForDisplay DeconstructLinkShare(LinkShare link)
        {
            if (link?.LinkContext == null)
            {
                return default;
            }

            var text = link.Text;
            var startIndex = text.IndexOf(link.LinkContext.LinkUrl, StringComparison.Ordinal);
            if (startIndex < 0)
            {
                return default;
            }

            var urlLength = link.LinkContext.LinkUrl.Length;
            return new LinkTextForDisplay
            {
                Before = text.Substring(0, startIndex),
                LinkText = text.Substring(startIndex, urlLength),
                After = text.Substring(startIndex + urlLength),
            };
        }

        private static void CheckItemTextForEmoji(DirectItem item)
        {
            if (item.ItemType != DirectItemType.Text)
            {
                return;
            }

            if (item.Text.Length > 1 && Emoji.IsEmoji(item.Text))
            {
                item.Like = item.Text;
                item.ItemType = DirectItemType.Like;
            }
        }
    }
}
