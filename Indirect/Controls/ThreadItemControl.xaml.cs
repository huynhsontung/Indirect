using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Indirect.Wrapper;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Media;
using Microsoft.Toolkit.Uwp.UI.Extensions;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    internal sealed partial class ThreadItemControl : UserControl
    {
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

        private static void OnItemSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ThreadItemControl) d;
            var item = (InstaDirectInboxItemWrapper) e.NewValue;
            view.ProcessItem();
            view.LikeItemMenuOption.IsEnabled = !item.Parent.Pending;
            if (item.ItemType == DirectItemType.ActionLog)
                view.ItemContainer.Visibility = Visibility.Collapsed;
            if (item.ItemType == DirectItemType.Text)
                view.MenuCopyOption.Visibility = Visibility.Visible;
        }

        public ThreadItemControl()
        {
            this.InitializeComponent();
        }

        private void ProcessItem()
        {
            Item.Timestamp = Item.Timestamp.ToLocalTime();
            if (Item.ItemType == DirectItemType.Link)
                Item.Text = Item.Link.Text;
        }


        private void ImageFrame_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Item.ItemType == DirectItemType.AnimatedMedia) return;
            var uri = Item.FullImageUri;
            if (uri == null) return;
            var frame = Window.Current.Content as Frame;
            var page = frame?.Content as Page;
            var immersiveControl = page?.FindChild<ImmersiveControl>();
            immersiveControl?.Open(Item);
        }

        private void VideoPopupButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var uri = Item.VideoUri;
            if (uri == null) return;
            var frame = Window.Current.Content as Frame;
            var page = frame?.Content as Page;
            var immersiveControl = page?.FindChild<ImmersiveControl>();
            immersiveControl?.Open(Item);
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

        private void ReelShareImage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Item.ReelShareMedia?.Media.MediaType == InstaMediaType.Image ||
                Item.StoryShareMedia?.Media.MediaType == InstaMediaType.Image)
            {
                ImageFrame_Tapped(sender, e);
            }
            else
            {
                VideoPopupButton_OnTapped(sender, e);
            }
        }

        private void Item_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => LikeUnlikeItem();

        private void LikeUnlike_Click(object sender, RoutedEventArgs e) => LikeUnlikeItem();

        private bool _timeout;
        private async void LikeUnlikeItem()
        {
            if (_timeout || Item.Parent.Pending) return;
            if (Item.Reactions?.MeLiked ?? false)
            {
                Item.UnlikeItem();
            }
            else
            {
                Item.LikeItem();
            }
            _timeout = true;
            await Task.Delay(TimeSpan.FromSeconds(2));
            _timeout = false;
        }

        private void MenuCopyOption_Click(object sender, RoutedEventArgs e)
        {
            var border = MainContentControl.ContentTemplateRoot as Border;
            var textBlock = border?.Child as TextBlock;
            if (textBlock == null) return;
            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(textBlock.Text);
            Clipboard.SetContent(dataPackage);
        }

        private void ConfigTooltip_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var tooltip = new ToolTip();
            tooltip.Content = $"{Item.Timestamp:f}";
            tooltip.PlacementRect = new Rect(0,12, e.NewSize.Width, e.NewSize.Height);
            ToolTipService.SetToolTip((DependencyObject) sender, tooltip);
        }

        private async void UnsendMessage(object sender, RoutedEventArgs e)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Unsend message?",
                Content = "Unsending will remove the message for everyone",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Unsend",
                DefaultButton = ContentDialogButton.Primary,
            };
            var confirmation = await confirmDialog.ShowAsync();
            if (confirmation == ContentDialogResult.Primary)
            {
                await Item.Unsend();
            }
        }

        private async void StoryShareOwnerLink_OnClick(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            var uri = new Uri($"https://www.instagram.com/{Item.StoryShareMedia.OwnerUsername}/");
            await Launcher.LaunchUriAsync(uri);
        }
    }
}
