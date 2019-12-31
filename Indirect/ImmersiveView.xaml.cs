using System;
using Windows.Media.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Indirect.Wrapper;
using InstaSharper.Enums;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect
{
    sealed partial class ImmersiveView : ContentDialog
    {
        private InstaDirectInboxItemWrapper _item;

        public ImmersiveView(InstaDirectInboxItemWrapper item, InstaMediaType mediaType)
        {
            this.InitializeComponent();
            _item = item;
            if (mediaType == InstaMediaType.Image)
            {
                ScrollViewer.Visibility = Visibility.Visible;
                Grid.MinHeight = ((Frame)Window.Current.Content).ActualHeight * 0.8;
            }
            else
            {
                MediaPlayer.Visibility = Visibility.Visible;
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    async () => { MediaPlayer.Source = await VideoCache.Instance.GetFromCacheAsync(item.VideoUri); });
            }

        }

        private void ScrollViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var scrollviewer = (ScrollViewer) sender;
            // ImageView.Width = scrollviewer.ViewportWidth;
            ImageView.Height = scrollviewer.ViewportHeight;
        }
    }
}
