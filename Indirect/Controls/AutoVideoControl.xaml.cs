using System;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    public sealed partial class AutoVideoControl : UserControl
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(object),
            typeof(AutoVideoControl),
            new PropertyMetadata(null, OnSourceChange));
        public static readonly DependencyProperty TransportControlsProperty = DependencyProperty.Register(
            nameof(TransportControls),
            typeof(MediaTransportControls),
            typeof(AutoVideoControl),
            new PropertyMetadata(null, OnTransportControlsChanged));
        public static readonly DependencyProperty VideoWidthProperty = DependencyProperty.Register(
            nameof(VideoWidth),
            typeof(double),
            typeof(AutoVideoControl),
            new PropertyMetadata(0, OnVideoSizeChanged));
        public static readonly DependencyProperty VideoHeightProperty = DependencyProperty.Register(
            nameof(VideoHeight),
            typeof(double),
            typeof(AutoVideoControl),
            new PropertyMetadata(0, OnVideoSizeChanged));
        public static readonly DependencyProperty AreTransportControlsEnabledProperty = DependencyProperty.Register(
            nameof(AreTransportControlsEnabled),
            typeof(bool),
            typeof(AutoVideoControl),
            new PropertyMetadata(true, AreTransportControlsEnabledChanged));
        public static readonly DependencyProperty PosterSourceProperty = DependencyProperty.Register(
            nameof(PosterSource),
            typeof(object),
            typeof(AutoVideoControl),
            new PropertyMetadata(null, OnPosterSourceChanged));
        public static readonly DependencyProperty IsAudioPlayerProperty = DependencyProperty.Register(
            nameof(IsAudioPlayer),
            typeof(bool),
            typeof(AutoVideoControl),
            new PropertyMetadata(false, PropertyChangedCallback));
        public static readonly DependencyProperty AutoPlayProperty = DependencyProperty.Register(
            nameof(AutoPlay),
            typeof(bool),
            typeof(AutoVideoControl),
            new PropertyMetadata(false));
        public static readonly DependencyProperty AutoStopProperty = DependencyProperty.Register(
            nameof(AutoStop),
            typeof(bool),
            typeof(AutoVideoControl),
            new PropertyMetadata(true));
        public static readonly DependencyProperty IsLoopingEnabledProperty = DependencyProperty.Register(
            nameof(IsLoopingEnabled),
            typeof(bool),
            typeof(AutoVideoControl),
            new PropertyMetadata(false, OnIsLoopingEnabledChanged));

        private static void OnIsLoopingEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (AutoVideoControl) d;
            var value = (bool) e.NewValue;
            view.MediaPlayer.IsLoopingEnabled = value;
        }

        private static void OnPosterSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (AutoVideoControl) d;
            var source = e.NewValue;
            if (source == null) return;
            var imageSource = source as ImageSource;
            if (imageSource != null)
            {
                view.VideoPlayer.PosterSource = imageSource;
                return;
            }

            var uri = source as Uri;
            if (uri == null)
            {
                var url = source as string ?? source.ToString();
                if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri)) return;
            }
            
            view.VideoPlayer.PosterSource = new BitmapImage(uri);
        }

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (AutoVideoControl) d;
            view.VideoPlayer.Style = (bool) e.NewValue ? (Style) view.Resources["AudioOnlyMediaPlayerElement"] : null;
        }

        private static void AreTransportControlsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (AutoVideoControl) d;
            view.VideoPlayer.AreTransportControlsEnabled = (bool) e.NewValue;
        }


        private static void OnVideoSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (AutoVideoControl) d;
            var element = view.VideoPlayer;
            var maxHeight = view.MaxHeight;
            var maxWidth = view.MaxWidth;
            if (double.IsInfinity(maxWidth) && double.IsInfinity(maxHeight) && 
                view.ActualWidth > 0 && view.ActualHeight > 0)
            {
                maxHeight = view.ActualHeight;
                maxWidth = view.ActualWidth;
            }

            element.MaxHeight = maxHeight;
            element.MaxWidth = maxWidth;
            var height = view.VideoHeight;
            var width = view.VideoWidth;
            if (height < 1 || width < 1) return;
            if (maxWidth < 1 && maxHeight < 1)
            {
                element.Width = width;
                element.Height = height;
                return;
            }

            if (maxWidth < 1)
            {
                element.Width = maxHeight * width / height;
                return;
            }

            if (maxHeight < 1)
            {
                element.Height = maxWidth * height / width;
                return;
            }

            var elementMaxRatio = maxHeight / maxWidth;
            var ratio = height / width;
            if (ratio <= elementMaxRatio)
            {
                element.Width = width;
                var actualWidth = width <= maxWidth ? width : maxWidth;
                element.Height = actualWidth / width * height;
            }
            else
            {
                element.Height = height;
                var actualHeight = height <= maxHeight ? height : maxHeight;
                element.Width = actualHeight / height * width;
            }
        }

        private static void OnTransportControlsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (AutoVideoControl) d;
            view.VideoPlayer.TransportControls = (MediaTransportControls)e.NewValue;
        }

        private static void OnSourceChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (AutoVideoControl) d;
            var source = e.NewValue;
            if (source is IMediaPlaybackSource mediaSource)
            {
                view.VideoPlayer.Source = mediaSource;
                return;
            }

            if (source is IStorageFile fileSource)
            {
                view.VideoPlayer.Source = MediaSource.CreateFromStorageFile(fileSource);
            }

            var uri = source as Uri;
            if (uri == null)
            {
                var sUri = source as string;
                if (sUri == null) return;
                uri = new Uri(sUri);
            }

            mediaSource = MediaSource.CreateFromUri(uri);
            
            view.VideoPlayer.Source = mediaSource;
        }


        public object Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
        public object PosterSource
        {
            get => GetValue(PosterSourceProperty);
            set => SetValue(PosterSourceProperty, value);
        }
        public MediaTransportControls TransportControls
        {
            get => (MediaTransportControls) GetValue(TransportControlsProperty);
            set => SetValue(TransportControlsProperty, value);
        }
        public double VideoWidth
        {
            get => Convert.ToDouble(GetValue(VideoWidthProperty));
            set => SetValue(VideoWidthProperty, value);
        }
        public double VideoHeight
        {
            get => Convert.ToDouble(GetValue(VideoHeightProperty));
            set => SetValue(VideoHeightProperty, value);
        }
        public bool AreTransportControlsEnabled
        {
            get => (bool) GetValue(AreTransportControlsEnabledProperty);
            set => SetValue(AreTransportControlsEnabledProperty, value);
        }

        public bool IsAudioPlayer
        {
            get => (bool) GetValue(IsAudioPlayerProperty);
            set => SetValue(IsAudioPlayerProperty, value);
        }
        public bool AutoPlay
        {
            get => (bool)GetValue(AutoPlayProperty);
            set => SetValue(AutoPlayProperty, value);
        }
        public bool AutoStop
        {
            get => (bool) GetValue(AutoStopProperty);
            set => SetValue(AutoStopProperty, value);
        }
        public bool IsLoopingEnabled
        {
            get => (bool) GetValue(IsLoopingEnabledProperty);
            set => SetValue(IsLoopingEnabledProperty, value);
        }

        public MediaPlayer MediaPlayer => VideoPlayer.MediaPlayer;

        public AutoVideoControl()
        {
            this.InitializeComponent();
            VideoPlayer.TransportControls = new MediaTransportControls
            {
                IsFullWindowEnabled = false,
                IsFullWindowButtonVisible = false,
                IsSeekBarVisible = false,
                IsZoomButtonVisible = false,
                IsZoomEnabled = false,
                IsCompact = true,
            };
            VideoPlayer.MediaPlayer.Volume = 0.5;
        }

        private void AutoVideoControl_OnEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
        {
            var bringIntoViewDistanceX = args.BringIntoViewDistanceX;
            var bringIntoViewDistanceY = args.BringIntoViewDistanceY;

            var width = ActualWidth;
            var height = ActualHeight;

            if (bringIntoViewDistanceX >= width * 0.9 || bringIntoViewDistanceY >= height * 0.9)
            {
                if (AutoStop) Pause();
            }
            else if (AutoPlay)
            {
                Play();
            }
        }

        public void Pause() => VideoPlayer.MediaPlayer?.Pause();

        public void Play() => VideoPlayer.MediaPlayer?.Play();

        private void VideoPlayer_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            ((MediaPlayerElement) sender).TransportControls?.Hide();
        }

        private void AutoVideoControl_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            OnVideoSizeChanged(this, null);
        }

        private void AutoVideoControl_OnUnloaded(object sender, RoutedEventArgs e)
        {
            AutoPlay = false;
            Pause();
            Source = null;
        }
    }
}
