using System;
using Windows.Media.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Indirect.Wrapper;
using InstaSharper.Enums;
using Microsoft.Toolkit.Uwp.UI.Controls;
using ScrollViewer = Windows.UI.Xaml.Controls.ScrollViewer;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect
{
    sealed partial class ImmersiveView : ContentDialog
    {
        private readonly InstaDirectInboxItemWrapper _item;

        public ImmersiveView(InstaDirectInboxItemWrapper item, InstaMediaType mediaType)
        {
            this.InitializeComponent();
            _item = item;
            if (mediaType == InstaMediaType.Image)
            {
                ContentControl.MinHeight = ((Frame)Window.Current.Content).ActualHeight * 0.8;
                ContentControl.ContentTemplate = (DataTemplate) Resources["ImageView"];
            }
            else
            {
                ContentControl.ContentTemplate = (DataTemplate) Resources["VideoView"];
            }

        }

        private void ScrollViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var scrollviewer = (ScrollViewer) sender;
            var imageView = scrollviewer.Content as ImageEx;
            if (imageView == null) return;
            imageView.Height = scrollviewer.ViewportHeight;
            if (_item.FullImageWidth > scrollviewer.ViewportWidth) imageView.Width = scrollviewer.ViewportWidth;
        }

        private void ImmersiveView_OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            var videoView = ContentControl.ContentTemplateRoot as AutoVideoControl;
            if (videoView == null) return;
            videoView.MediaPlayer.Pause();
        }
    }
}
