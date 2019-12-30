using Windows.Media.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect
{
    public sealed partial class ImmersiveView : ContentDialog
    {
        private BitmapImage _image;
        private MediaSource _video;

        public ImmersiveView(BitmapImage image)
        {
            this.InitializeComponent();
            ScrollViewer.Visibility = Visibility.Visible;
            Grid.MinHeight = ((Frame)Window.Current.Content).ActualHeight * 0.8;
            _image = image;

        }

        public ImmersiveView(MediaSource video)
        {
            this.InitializeComponent();
            MediaPlayer.Visibility = Visibility.Visible;
            _video = video;
        }

        private void ScrollViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var scrollviewer = (ScrollViewer) sender;
            // ImageView.Width = scrollviewer.ViewportWidth;
            ImageView.Height = scrollviewer.ViewportHeight;
        }
    }
}
