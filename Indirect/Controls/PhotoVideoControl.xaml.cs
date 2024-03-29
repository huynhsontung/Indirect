﻿using System;
using System.ComponentModel;
using System.Linq;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    public sealed partial class PhotoVideoControl : UserControl, INotifyPropertyChanged
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
            try
            {
                if (Source is IRandomAccessStreamWithContentType stream) // screenshot
                {
                    var image = new BitmapImage();
                    await image.SetSourceAsync(stream);
                    _source = image;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_source)));
                    return;
                }

                if (!(Source is IStorageFile storageFile))
                {
                    var uri = Source as Uri;
                    if (uri == null)
                    {
                        var uriString = Source as string;
                        if (!string.IsNullOrEmpty(uriString)) uri = new Uri(uriString);
                        else
                        {
                            _source = Source;
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_source)));
                            return;
                        }
                    }

                    _source = uri;

                    if (uri.IsFile && uri.Segments.Last().Contains(".mp4", StringComparison.OrdinalIgnoreCase))
                    {
                        _source = MediaSource.CreateFromUri(uri);
                    }
                }
                else
                {
                    if (storageFile.ContentType.Contains("video", StringComparison.OrdinalIgnoreCase))
                    {
                        _source = MediaSource.CreateFromStorageFile(storageFile);
                    }
                    else
                    {
                        _source = new BitmapImage();
                        using (var fileStream = await storageFile.OpenAsync(FileAccessMode.Read))
                        {
                            await ((BitmapImage) _source).SetSourceAsync(fileStream);
                        }
                    }
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_source)));
            }
            catch (Exception)
            {
                // pass
            }
        }

        private object _source;

        public PhotoVideoControl()
        {
            this.InitializeComponent();
        }


        private void VideoFrame_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var videoFrame = (AutoVideoControl) sender;
            if (videoFrame.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                videoFrame.MediaPlayer.Pause();
            }
            else
            {
                videoFrame.MediaPlayer.Play();
            }
        }

        public void PauseVideo()
        {
            if (ContentControl.ContentTemplateRoot is AutoVideoControl videoView)
            {
                videoView.MediaPlayer?.Pause();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    class PhotoVideoTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ImageView { get; set; }
        public DataTemplate VideoView { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return item is IMediaSource ? VideoView : ImageView;
        }
    }
}
