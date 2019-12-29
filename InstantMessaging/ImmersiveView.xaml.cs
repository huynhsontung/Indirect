using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using InstaSharper.Enums;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace InstantMessaging
{
    public sealed partial class ImmersiveView : ContentDialog
    {
        private BitmapImage _image;
        private MediaSource _video;

        public ImmersiveView(BitmapImage image)
        {
            this.InitializeComponent();
            ScrollViewer.Visibility = Visibility.Visible;
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
