using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Indirect.Wrapper;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Media;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    internal sealed partial class ImmersiveControl : UserControl
    {
        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(
            nameof(Item),
            typeof(object),
            typeof(ImmersiveControl),
            new PropertyMetadata(null, OnItemChanged));

        public object Item
        {
            get => GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ImmersiveControl)d;

            if (e.NewValue is ReelsWrapper reel && e.NewValue != null)
            {
                view.PrepareReelView();
            }

            var item = e.NewValue as InstaDirectInboxItemWrapper;
            if (item == null) return;
            switch (item.ItemType)
            {
                case DirectItemType.Media when item.Media.MediaType == InstaMediaType.Image:
                case DirectItemType.RavenMedia when 
                    item.RavenMedia?.MediaType == InstaMediaType.Image || item.VisualMedia?.Media.MediaType == InstaMediaType.Image:
                    view.PrepareImageView();
                    break;

                case DirectItemType.Media when item.Media.MediaType == InstaMediaType.Video:
                case DirectItemType.RavenMedia when
                    item.RavenMedia?.MediaType == InstaMediaType.Video || item.VisualMedia?.Media.MediaType == InstaMediaType.Video:
                    view.PrepareVideoView();
                    break;

                case DirectItemType.ReelShare:
                    if (item.ReelShareMedia.Media.MediaType == ReelMediaType.Image)
                        view.PrepareImageView();
                    else
                        view.PrepareVideoView();
                    break;

                case DirectItemType.StoryShare when item.StoryShareMedia.Media != null:
                    if (item.StoryShareMedia.Media.MediaType == ReelMediaType.Image)
                        view.PrepareImageView();
                    else
                        view.PrepareVideoView();
                    break;

                default:
                    view.MainControl.ContentTemplate = null;
                    break;
            }
        }

        public ImmersiveControl()
        {
            this.InitializeComponent();
        }

        private void PrepareImageView()
        {
            MainControl.ContentTemplate = (DataTemplate)Resources["ImageView"];
            var scrollviewer = this.FindDescendant<ScrollViewer>();
            if (scrollviewer == null) return;
            scrollviewer.Width = ActualWidth;
            scrollviewer.Height = ActualHeight;
            ScrollViewer_OnSizeChanged(scrollviewer, null);
        }

        private void PrepareVideoView()
        {
            MainControl.ContentTemplate = (DataTemplate)Resources["VideoView"];
        }

        private void PrepareReelView()
        {
            MainControl.ContentTemplate = (DataTemplate)Resources["ReelView"];
        }

        private void ScrollViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var scrollviewer = (ScrollViewer)sender;
            var imageView = scrollviewer.Content as ImageEx;
            if (imageView == null) return;
            if (Item is InstaDirectInboxItemWrapper item && Item != null)
            {
                if (item.FullImageHeight > scrollviewer.ViewportHeight)
                {
                    imageView.MaxHeight = scrollviewer.ViewportHeight;
                }
                if (item.FullImageWidth > scrollviewer.ViewportWidth)
                {
                    imageView.MaxWidth = scrollviewer.ViewportWidth;
                }
            }
        }

        public void OnClose()
        {
            var videoView = MainControl.ContentTemplateRoot as AutoVideoControl;
            videoView?.MediaPlayer.Pause();

            var reelView = MainControl.ContentTemplateRoot as ReelsControl;
            reelView?.OnClose();
        }

        private void MainControl_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var scrollviewer = this.FindDescendant<ScrollViewer>();
            if (scrollviewer == null) return;
            scrollviewer.Width = e.NewSize.Width;
            scrollviewer.Height = e.NewSize.Height;
        }

        private void ScrollViewer_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var scrollviewer = (ScrollViewer) sender;
            if (scrollviewer.ZoomFactor > 1)
            {
                scrollviewer.ChangeView(null, null, 1);
            }
        }

        private void ScrollViewer_OnLoaded(object sender, RoutedEventArgs e)
        {
            var scrollviewer = (ScrollViewer) sender;
            scrollviewer.ChangeView(null, null, 1, true);
        }
    }
}
