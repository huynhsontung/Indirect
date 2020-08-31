using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Indirect.Entities.Wrappers;
using Indirect.Services;
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
            typeof(DirectItemWrapper),
            typeof(ThreadItemControl),
            new PropertyMetadata(null, OnItemSourceChanged));

        public DirectItemWrapper Item
        {
            get => (DirectItemWrapper) GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        private static void OnItemSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ThreadItemControl) d;
            var item = (DirectItemWrapper) e.NewValue;
            view.ProcessItem();
            view.LikeItemMenuOption.IsEnabled = !item.Parent.Pending;
            if (item.ItemType == DirectItemType.ActionLog)
                view.ItemContainer.Visibility = Visibility.Collapsed;
            if (item.ItemType == DirectItemType.Text)
                view.MenuCopyOption.Visibility = Visibility.Visible;
            view.Bindings.Update();
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

        private string SeenTextConverter(Dictionary<long, LastSeen> lastSeenAt)
        {
            var seenList = lastSeenAt.Where(x => 
                    x.Value.ItemId == Item.ItemId &&    // Match item id
                    x.Key != Item.Parent.ViewerId &&    // Not from viewer
                    x.Key != Item.Sender.Pk             // Not from sender
                ).Select(y => y.Key).ToArray();
            if (seenList.Length == 0) return string.Empty;
            if (Item.Parent.Users.Count == 1)
            {
                if (Item.FromMe && Item.Parent.LastPermanentItem.ItemId != Item.ItemId) return string.Empty;
                return "Seen";
            }
            if (Item.Parent.Users.Count <= seenList.Length) return "Seen by everyone";
            var seenUsers = seenList.Select(x => Item.Parent.Users.FirstOrDefault(y => x == y.Pk)?.Username).ToArray();
            if (seenUsers.Length <= 3)
            {
                return "Seen by " + string.Join(", ", seenUsers);
            }

            return $"Seen by {seenUsers[0]}, {seenUsers[1]}, {seenUsers[2]} and {seenUsers.Length - 3} others";
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
            if (Item.ObservableReactions.MeLiked)
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

        private void ReactionsBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Item.ObservableReactions.Senders.Count == 0) return;
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }
    }
}
