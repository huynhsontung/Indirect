using System;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Indirect.Entities.Wrappers;
using Indirect.Utilities;
using Microsoft.Toolkit.Uwp.UI.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    internal sealed partial class ImmersiveControl : UserControl
    {
        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(
            nameof(Item),
            typeof(object),
            typeof(ImmersiveControl),
            new PropertyMetadata(null));

        public object Item
        {
            get => GetValue(ItemProperty);
            private set => SetValue(ItemProperty, value);
        }

        public bool IsOpen => MediaPopup.IsOpen;

        private Control _focusedElement;

        public ImmersiveControl()
        {
            this.InitializeComponent();

            Window.Current.SizeChanged += OnWindowSizeChanged;
            MediaPopup.Width = Window.Current.Bounds.Width;
            MediaPopup.Height = Window.Current.Bounds.Height - 32;
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            MediaPopup.Width = e.Size.Width;
            MediaPopup.Height = e.Size.Height > 32 ? e.Size.Height - 32 : e.Size.Height;
        }

        private void ScrollViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var scrollviewer = (ScrollViewer)sender;
            var imageView = scrollviewer.Content as ImageEx;
            if (imageView == null) return;
            if (Item is DirectItemWrapper item && Item != null)
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

        private void CloseMediaPopup_OnClick(object sender, RoutedEventArgs e) => Close();

        private async void DownloadMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var item = Item as DirectItemWrapper;
            var url = item?.VideoUri != null ? item.VideoUri : item?.FullImageUri;
            if (url == null)
            {
                return;
            }

            await MediaHelpers.DownloadMedia(url).ConfigureAwait(false);
        }

        public void Open(object item)
        {
            _focusedElement = FocusManager.GetFocusedElement() as Control;
            MediaPopup.IsOpen = true;
            Item = item;
        }

        public void Close()
        {
            MediaPopup.IsOpen = false;
            Item = null;

            try
            {
                _focusedElement?.Focus(FocusState.Programmatic);
            }
            catch (Exception)
            {
                // pass
            }
        }

        private void ScrollViewer_OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var scrollviewer = (ScrollViewer) sender;
            switch (e.Key)
            {
                case VirtualKey.GamepadRightTrigger:
                    e.Handled = true;
                    var zoomInFactor = scrollviewer.ZoomFactor + 0.2f;
                    scrollviewer.ChangeView(null, null,
                        zoomInFactor >= scrollviewer.MaxZoomFactor ? scrollviewer.MaxZoomFactor : zoomInFactor);
                    break;
                case VirtualKey.GamepadLeftTrigger:
                    e.Handled = true;
                    var zoomOutFactor = scrollviewer.ZoomFactor - 0.2f;
                    scrollviewer.ChangeView(null, null,
                        zoomOutFactor < scrollviewer.MinZoomFactor ? scrollviewer.MinZoomFactor : zoomOutFactor);
                    break;
            }
        }

        private void MainControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (MediaPopup.IsOpen)
            {
                var contentRoot = MainControl.ContentTemplateRoot as Control;
                contentRoot?.Focus(FocusState.Programmatic);
            }
        }
    }
}
