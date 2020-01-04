using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Indirect.Utilities;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect
{
    public sealed partial class PhotoVideoControl : UserControl
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(object),
            typeof(PhotoVideoControl),
            new PropertyMetadata(null, SourceChanged));


        public object Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (PhotoVideoControl) d;
            view.OnSourceChanged();
        }

        private async void OnSourceChanged()
        {
            ImageFrame.Visibility = Visibility.Visible;
            VideoFrame.Visibility = Visibility.Collapsed;
            if (!(Source is IStorageFile storageFile))
            {
                var uri = Source as Uri;
                if (uri == null)
                {
                    var uriString = Source as string;
                    if (!string.IsNullOrEmpty(uriString)) uri = new Uri(uriString);
                    else return;
                }
                _imageSource = uri;
                if (uri.IsFile && uri.Segments.Last().Contains(".mp4"))
                {
                    ImageFrame.Visibility = Visibility.Collapsed;
                    VideoFrame.Visibility = Visibility.Visible;

                    if (Helpers.IsHttpUri(uri))
                    {
                        _videoSource = await VideoCache.Instance.GetFromCacheAsync(uri);
                    }
                    else
                    {
                        _videoSource = MediaSource.CreateFromUri(uri);
                    }
                }
            }
            else
            {
                if (storageFile.FileType == ".mp4" || storageFile.FileType == ".MP4")
                {
                    ImageFrame.Visibility = Visibility.Collapsed;
                    VideoFrame.Visibility = Visibility.Visible;
                    _videoSource = MediaSource.CreateFromStorageFile(storageFile);
                }
                else
                {
                    _imageSource = new BitmapImage();
                    using (var fileStream = await storageFile.OpenAsync(FileAccessMode.Read))
                    {
                        await ((BitmapImage) _imageSource).SetSourceAsync(fileStream);
                    }
                }
            }

            this.Bindings.Update();
        }

        private object _imageSource;
        private MediaSource _videoSource;

        public PhotoVideoControl()
        {
            this.InitializeComponent();
        }


        private void VideoFrame_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (VideoFrame.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                VideoFrame.MediaPlayer.Pause();
            }
            else
            {
                VideoFrame.MediaPlayer.Play();
            }
        }
    }
}
