using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Streaming.Adaptive;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Indirect.Wrapper;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Enums;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect
{
    internal sealed partial class ThreadItemControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private InstaDirectInboxItemWrapper _source;

        public InstaDirectInboxItemWrapper Source
        {
            get => _source;
            set
            {
                _source = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Source)));
            }
        }

        public ThreadItemControl()
        {
            this.InitializeComponent();
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Source)) return;
            this.Bindings.Update();
            OpenMediaButton.Visibility = Visibility.Collapsed;
            MessageContentWithBorder.Visibility = Visibility.Collapsed;
            MessageContentNoBorder.Visibility = Visibility.Collapsed;
            ImageFrame.Visibility = Visibility.Collapsed;
            NotAvailableMessage.Visibility = Visibility.Collapsed;
            MediaFrame.Visibility = Visibility.Collapsed;
            switch (Source.ItemType)
            {
                case InstaDirectThreadItemType.Text:
                case InstaDirectThreadItemType.Link:
                case InstaDirectThreadItemType.Hashtag:
                    MessageContentWithBorder.Visibility = Visibility.Visible;
                    break;

                case InstaDirectThreadItemType.Like:
                    MessageContentNoBorder.Visibility = Visibility.Visible;
                    break;

                // case InstaDirectThreadItemType.MediaShare:
                //     break;
                case InstaDirectThreadItemType.RavenMedia when Source.VisualMedia.ViewMode != InstaViewMode.Permanent:
                    OpenMediaButton.Visibility = Visibility.Visible;
                    break;

                case InstaDirectThreadItemType.Media when Source.Media.MediaType == InstaMediaType.Image:
                case InstaDirectThreadItemType.RavenMedia when
                    Source.RavenMedia?.MediaType == InstaMediaType.Image || Source.VisualMedia?.Media.MediaType == InstaMediaType.Image:
                    ConformElementSize(ImageFrame, Source.PreviewImage.DecodePixelWidth, Source.PreviewImage.DecodePixelHeight);
                    ImageFrame.Visibility = Visibility.Visible;
                    break;

                case InstaDirectThreadItemType.Media when Source.Media.MediaType == InstaMediaType.Video:
                case InstaDirectThreadItemType.RavenMedia when
                    Source.RavenMedia?.MediaType == InstaMediaType.Video || Source.VisualMedia.Media.MediaType == InstaMediaType.Video:
                    var videoWidth = Source.RavenMedia?.Width ?? Source.VisualMedia.Media.Width;
                    var videoHeight = Source.RavenMedia?.Height ?? Source.VisualMedia.Media.Height;
                    ConformElementSize(MediaFrame, videoWidth, videoHeight);
                    MediaFrame.TransportControls.IsFullWindowEnabled = false;
                    MediaFrame.TransportControls.IsFullWindowButtonVisible = false;
                    MediaFrame.TransportControls.IsZoomButtonVisible = false;
                    MediaFrame.TransportControls.ShowAndHideAutomatically = true;
                    MediaFrame.Visibility = Visibility.Visible;
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

        private async Task SetVideoSourceAsync()
        {
            var uri = new Uri(Source.RavenMedia?.Videos.FirstOrDefault()?.Url ??
                              Source.VisualMedia?.Media.Videos.First().Url);
            var videoSource = MediaSource.CreateFromUri(uri);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                MediaFrame.Source = videoSource;
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
            ((MediaPlayerElement) sender).TransportControls.Hide();
        }

        private void ImageFrame_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var fullImage = Source.FullImage;
            if (fullImage == null) return;
            var immersive = new ImmersiveView(fullImage);
            var result = immersive.ShowAsync();
        }

        private void MediaFrame_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var video = Source.MediaSource;
            if (video == null) return;
            var immersive = new ImmersiveView(video);
            var result = immersive.ShowAsync();
        }
    }
}
