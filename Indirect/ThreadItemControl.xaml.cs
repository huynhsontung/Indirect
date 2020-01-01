using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Media.Core;
using Windows.Media.Streaming.Adaptive;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Indirect.Wrapper;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Enums;
using Microsoft.Toolkit.Uwp.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect
{
    internal sealed partial class ThreadItemControl : UserControl
    {
        public static readonly DependencyProperty ThreadProperty = DependencyProperty.Register(
            nameof(Thread),
            typeof(InstaDirectInboxThreadWrapper),
            typeof(ThreadItemControl),
            new PropertyMetadata(null, OnThreadChanged));

        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(
            nameof(Item),
            typeof(InstaDirectInboxItemWrapper),
            typeof(ThreadItemControl),
            new PropertyMetadata(null, OnItemSourceChanged));

        public InstaDirectInboxItemWrapper Item
        {
            get => (InstaDirectInboxItemWrapper) GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        public InstaDirectInboxThreadWrapper Thread
        {
            get => (InstaDirectInboxThreadWrapper) GetValue(ThreadProperty);
            set => SetValue(ThreadProperty, value);
        }

        private static void OnThreadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ThreadItemControl)d;
        }

        private static void OnItemSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ThreadItemControl) d;
            view.HandleItemSourceChange(e);
        }

        private Uri _navigateUri;

        public ThreadItemControl()
        {
            this.InitializeComponent();
            MediaFrame.TransportControls.IsFullWindowEnabled = false;
            MediaFrame.TransportControls.IsFullWindowButtonVisible = false;
            MediaFrame.TransportControls.IsZoomButtonVisible = false;
            MediaFrame.TransportControls.ShowAndHideAutomatically = true;
        }

        private void HandleItemSourceChange(DependencyPropertyChangedEventArgs e)
        {
            ProcessItem();
            this.Bindings.Update();
            MessageContentWithBorder.Visibility = Visibility.Collapsed;
            switch (Item.ItemType)
            {
                case InstaDirectThreadItemType.Text when string.IsNullOrEmpty(_navigateUri?.ToString()):
                    MessageContentWithBorder.Visibility = Visibility.Visible;
                    break;

                case InstaDirectThreadItemType.Text:
                    HyperlinkContent.Visibility = Visibility.Visible;
                    break;

                case InstaDirectThreadItemType.Link:
                    HyperlinkContent.Visibility = Visibility.Visible;
                    HyperlinkPreview.Visibility = Visibility.Visible;
                    break;

                case InstaDirectThreadItemType.Like:
                    MessageContentNoBorder.Visibility = Visibility.Visible;
                    break;

                case InstaDirectThreadItemType.MediaShare:
                    // todo: Handle all MediaShare cases
                    MediaShareView.Visibility = Visibility.Visible;
                    break;

                case InstaDirectThreadItemType.RavenMedia when Item.VisualMedia.ViewMode != InstaViewMode.Permanent:
                    OpenMediaButton.Visibility = Visibility.Visible;
                    break;

                case InstaDirectThreadItemType.Media when Item.Media.MediaType == InstaMediaType.Image:
                case InstaDirectThreadItemType.RavenMedia when
                    Item.RavenMedia?.MediaType == InstaMediaType.Image || Item.VisualMedia?.Media.MediaType == InstaMediaType.Image:
                    // ConformElementSize(ImageFrame, Source.PreviewImageUri.DecodePixelWidth, Source.PreviewImageUri.DecodePixelHeight);
                    ImageFrame.Visibility = Visibility.Visible;
                    break;

                case InstaDirectThreadItemType.Media when Item.Media.MediaType == InstaMediaType.Video:
                case InstaDirectThreadItemType.RavenMedia when
                    Item.RavenMedia?.MediaType == InstaMediaType.Video || Item.VisualMedia.Media.MediaType == InstaMediaType.Video:
                    var videoWidth = Item.RavenMedia?.Width ?? Item.VisualMedia.Media.Width;
                    var videoHeight = Item.RavenMedia?.Height ?? Item.VisualMedia.Media.Height;
                    _ = SetVideoSourceAsync();
                    ConformElementSize(MediaFrame, videoWidth, videoHeight);
                    VideoView.Visibility = Visibility.Visible;
                    break;
                // case InstaDirectThreadItemType.ReelShare:
                //     break;
                // case InstaDirectThreadItemType.Placeholder:
                //     break;
                // case InstaDirectThreadItemType.StoryShare:
                //     break;

                case InstaDirectThreadItemType.ActionLog:
                    ItemContainer.Visibility = Visibility.Collapsed;
                    break;

                // case InstaDirectThreadItemType.Profile:
                //     break;
                // case InstaDirectThreadItemType.Location:
                //     break;
                // case InstaDirectThreadItemType.FelixShare:
                //     break;
                // case InstaDirectThreadItemType.VoiceMedia:
                //     break;
                case InstaDirectThreadItemType.AnimatedMedia:
                    ImageFrame.Visibility = Visibility.Visible;
                    break;
                // case InstaDirectThreadItemType.LiveViewerInvite:
                //     break;
                default:
                    NotAvailableMessage.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void ProcessItem()
        {
            Item.TimeStamp = Item.TimeStamp.ToLocalTime();
            switch (Item.ItemType)
            {
                case InstaDirectThreadItemType.Text when Item.Text[0] == '#' && !Item.Text.Contains(' '):
                    _navigateUri = new Uri("https://www.instagram.com/explore/tags/" + Item.Text.Substring(1));
                    break;

                case InstaDirectThreadItemType.Link:
                    _navigateUri = new Uri(Item.LinkMedia.LinkContext.LinkUrl);
                    break;

                case InstaDirectThreadItemType.MediaShare:
                    _navigateUri = new Uri("https://www.instagram.com/p/" + Item.MediaShare.Code);
                    break;
            }
        }

        private async Task SetVideoSourceAsync()
        {
            var previewSource = await ImageCache.Instance.GetFromCacheAsync(Item.PreviewImageUri);
            var videoSource = await VideoCache.Instance.GetFromCacheAsync(Item.VideoUri);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MediaFrame.Source = videoSource;
                MediaFrame.PosterSource = previewSource;
            });
        }

        private static void ConformElementSize(FrameworkElement element, double width, double height)
        {
            var elementMaxRatio = element.MaxHeight / element.MaxWidth;
            var ratio = height / width;
            if (ratio <= elementMaxRatio)
            {
                element.Width = width;
                var actualWidth = width <= element.MaxWidth ? width : element.MaxWidth;
                element.Height = actualWidth / width * height;
            }
            else
            {
                element.Height = height;
                var actualHeight = height <= element.MaxHeight ? height : element.MaxHeight;
                element.Width = actualHeight / height * width;
            }
        }

        private void ItemContainer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var panel = (Panel)sender;
            var timestampTextBlock = panel.Children.Last();
            timestampTextBlock.Visibility = timestampTextBlock.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private void MediaFrame_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            MediaFrame.TransportControls.Hide();
            VideoPopupButton.Visibility = Visibility.Collapsed;
        }

        private void ImageFrame_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Item.ItemType == InstaDirectThreadItemType.AnimatedMedia) return;
            var uri = Item.FullImageUri;
            if (uri == null) return;
            var immersive = new ImmersiveView(Item, InstaMediaType.Image);
            var result = immersive.ShowAsync();
        }

        private void VideoPopupButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var uri = Item.VideoUri;
            if (uri == null) return;
            var immersive = new ImmersiveView(Item, InstaMediaType.Video);
            var result = immersive.ShowAsync();
        }

        private void MediaFrame_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            VideoPopupButton.Visibility = Visibility.Visible;
        }

        private void OpenMediaButton_OnClick(object sender, RoutedEventArgs e)
        {
            ImageFrame_Tapped(sender, new TappedRoutedEventArgs());
        }

        private void OpenWebLink(object sender, TappedRoutedEventArgs e)
        {
            _ = Windows.System.Launcher.LaunchUriAsync(_navigateUri);
        }
    }
}
