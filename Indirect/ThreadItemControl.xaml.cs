using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Media.Core;
using Windows.Media.Streaming.Adaptive;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Indirect.Wrapper;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Enums;
using Microsoft.Toolkit.Uwp.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect
{
    internal sealed partial class ThreadItemControl : UserControl
    {
        public static readonly DependencyProperty ThreadProperty = DependencyProperty.Register(
            nameof(Thread),
            typeof(InstaDirectInboxThreadWrapper),
            typeof(ThreadItemControl),
            new PropertyMetadata(null, OnThreadChanged));

        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(
            nameof(Item),
            typeof(InstaDirectInboxItemWrapper),
            typeof(ThreadItemControl),
            new PropertyMetadata(null, OnItemSourceChanged));

        public InstaDirectInboxItemWrapper Item
        {
            get => (InstaDirectInboxItemWrapper) GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        public InstaDirectInboxThreadWrapper Thread
        {
            get => (InstaDirectInboxThreadWrapper) GetValue(ThreadProperty);
            set => SetValue(ThreadProperty, value);
        }

        private static void OnThreadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ThreadItemControl)d;
        }

        private static void OnItemSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ThreadItemControl) d;
            var item = (InstaDirectInboxItemWrapper) e.NewValue;
            view.ProcessItem();
            view.Bindings.Update();
            if (item.ItemType == InstaDirectThreadItemType.ActionLog)
                view.ItemContainer.Visibility = Visibility.Collapsed;
        }

        public ThreadItemControl()
        {
            this.InitializeComponent();
        }

        private void ProcessItem()
        {
            Item.TimeStamp = Item.TimeStamp.ToLocalTime();
            if (Item.ItemType == InstaDirectThreadItemType.Link)
                Item.Text = Item.LinkMedia.Text;
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

        private void ImageFrame_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Item.ItemType == InstaDirectThreadItemType.AnimatedMedia) return;
            var uri = Item.FullImageUri;
            if (uri == null) return;
            var immersive = new ImmersiveView(Item, InstaMediaType.Image);
            var result = immersive.ShowAsync();
        }

        private void VideoPopupButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var uri = Item.VideoUri;
            if (uri == null) return;
            var immersive = new ImmersiveView(Item, InstaMediaType.Video);
            var result = immersive.ShowAsync();
        }

        private void OpenMediaButton_OnClick(object sender, RoutedEventArgs e)
        {
            ImageFrame_Tapped(sender, new TappedRoutedEventArgs());
        }

        private void OpenWebLink(object sender, TappedRoutedEventArgs e)
        {
            if (Item.NavigateUri == null) return;
            _ = Windows.System.Launcher.LaunchUriAsync(Item.NavigateUri);
        }
    }
}
