using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Indirect.Entities;
using Indirect.Entities.Wrappers;
using Indirect.Utilities;
using InstagramAPI.Utils;
using Microsoft.Toolkit.Uwp.UI.Extensions;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    public sealed partial class ReelsControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(FlatReelsContainer),
            typeof(ReelsControl),
            new PropertyMetadata(null));

        public FlatReelsContainer Source
        {
            get => (FlatReelsContainer) GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private bool MorePreviousReels
        {
            get
            {
                var story = (ReelItemWrapper)StoryView.SelectedItem;
                if (story == null) return true;
                return Source.UserOrder.IndexOf(story.Parent.Source.User.Pk) != 0;
            }
        }

        private bool MoreNextReels
        {
            get
            {
                var story = (ReelItemWrapper)StoryView.SelectedItem;
                if (story == null) return true;
                return Source.UserOrder.LastIndexOf(story.Parent.Source.User.Pk) != Source.UserOrder.Count - 1;
            }
        }

        private Tuple<int, int> _reelLimit;

        public ReelsControl()
        {
            this.InitializeComponent();
        }

        private void ReelsControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            Source?.SelectItemToView();
        }

        private void ReelsControl_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Source = null;
        }

        private async Task PopReplyDeliveryStatus()
        {
            ReplyStatusInfoBar.IsOpen = true;
            if (await Debouncer.Delay(nameof(ReplyStatusInfoBar), 2000))
            {
                ReplyStatusInfoBar.IsOpen = false;
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var story = (ReelItemWrapper) StoryView.SelectedItem;
            var container = StoryView.ContainerFromItem(story) as FlipViewItem;
            var textBox = container.FindDescendant<TextBox>();
            if (string.IsNullOrEmpty(textBox?.Text) || story == null) return;
            var message = textBox.Text;
            textBox.Text = string.Empty;
            if (await story.Reply(message))
            {
                await PopReplyDeliveryStatus();
            }
        }

        private void StoryView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var storyView = (FlipView)sender;
            var previous = (ReelItemWrapper) e.RemovedItems.FirstOrDefault();
            var selected = (ReelItemWrapper) e.AddedItems.FirstOrDefault();
            var flipViewItem = storyView.ContainerFromItem(previous) as FlipViewItem;
            var element = flipViewItem?.ContentTemplateRoot as FrameworkElement;
            var autoVideo = element?.FindDescendant<AutoVideoControl>();
            autoVideo?.Pause();

            UpdateProgressBar(selected, previous);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MorePreviousReels)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MoreNextReels)));
        }

        private void UpdateProgressBar(ReelItemWrapper selected, ReelItemWrapper previous)
        {
            if (selected == null)
            {
                return;
            }

            var selectedReelId = selected.Parent.Source.Id;
            if (!selectedReelId.Equals(previous?.Parent.Source.Id))
            {
                var start = 0;
                var end = 0;
                for (int i = 0; i < Source.Items.Count; i++)
                {
                    if (selectedReelId.Equals(Source.Items[i].Parent.Source.Id))
                    {
                        start = i;
                        break;
                    }
                }

                for (int i = Source.Items.Count - 1; i >= 0; i--)
                {
                    if (selectedReelId.Equals(Source.Items[i].Parent.Source.Id))
                    {
                        end = i;
                        break;
                    }
                }

                _reelLimit = new Tuple<int, int>(start, end);
                var selectedIndex = StoryView.SelectedIndex;
                NewReelProgressIndicator.Count = end - start + 1;
                NewReelProgressIndicator.Selected = selectedIndex - start;
            }
            else if (_reelLimit != null)
            {
                var selectedIndex = StoryView.SelectedIndex;
                NewReelProgressIndicator.Selected = selectedIndex - _reelLimit.Item1;
            }
        }

        private void MessageTextBox_OnEnterPressed(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            SendButton_Click(sender, null);
        }

        private void PreviousReelButtonClick(object sender, RoutedEventArgs e)
        {
            var items = Source?.Items;
            var selectedIndex = StoryView.SelectedIndex;
            if (items == null || selectedIndex == -1) return;
            var userId = items[selectedIndex].Source.User.Pk;
            var previousReelIndex = -1;
            for (int i = selectedIndex; i >= 0; i--)
            {
                if (userId == items[i].Source.User.Pk) continue;
                previousReelIndex = i;
                userId = items[i].Source.User.Pk;
                break;
            }

            if (previousReelIndex == -1) return;
            var previousStories = items.Where(item => item.Source.User.Pk == userId).ToArray();
            var unseenStory = previousStories.FirstOrDefault(item => item.Source.TakenAt > item.Parent.Source.Seen);
            previousReelIndex = items.IndexOf(unseenStory ?? previousStories[0]);

            if (previousReelIndex != -1)
            {
                StoryView.SelectedIndex = previousReelIndex;
            }
        }

        private void NextReelButtonClick(object sender, RoutedEventArgs e)
        {
            var items = Source?.Items;
            var selectedIndex = StoryView.SelectedIndex;
            if (items == null || selectedIndex == -1) return;
            var userId = items[selectedIndex].Source.User.Pk;
            var nextReelIndex = -1;
            for (int i = selectedIndex; i < items.Count; i++)
            {
                if (nextReelIndex == -1)
                {
                    if (userId != items[i].Source.User.Pk)
                    {
                        nextReelIndex = i;
                        userId = items[i].Source.User.Pk;
                        if (items[i].Source.TakenAt > items[i].Parent.Source.Seen) break;
                    }
                }
                else
                {
                    // Go to unseen story
                    if (userId != items[i].Source.User.Pk) break;
                    if (items[i].Source.TakenAt > items[i].Parent.Source.Seen)
                    {
                        nextReelIndex = i;
                        break;
                    }
                }
            }

            if (nextReelIndex != -1)
            {
                StoryView.SelectedIndex = nextReelIndex;
            }
        }

        private async void UserInfo_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var userId = (StoryView.SelectedItem as ReelItemWrapper)?.Source.User.Pk ?? 0;
            if (userId == 0) return;
            if (UserInfoView.User?.Pk != userId)
            {
                var userInfoResult = await ((App)Application.Current).ViewModel.InstaApi.GetUserInfoAsync(userId);
                if (!userInfoResult.IsSucceeded) return;
                UserInfoView.User = userInfoResult.Value;
            }
            FlyoutBase.ShowAttachedFlyout(UserInfoGrid);
        }

        private async void DownloadMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var story = (ReelItemWrapper)StoryView.SelectedItem;
            if (story == null)
            {
                return;
            }

            await story.Download().ConfigureAwait(false);
        }

        private async void ReactEmojiButton_OnClick(object sender, RoutedEventArgs e)
        {
            var story = (ReelItemWrapper)StoryView.SelectedItem;
            var emoji = await EmojiPicker.ShowAsync((FrameworkElement) sender,
                new FlyoutShowOptions {Placement = FlyoutPlacementMode.TopEdgeAlignedRight});
            if (string.IsNullOrEmpty(emoji) || story == null)
            {
                return;
            }

            if (await story.Reply(emoji))
            {
                await PopReplyDeliveryStatus();
            }
        }

        private void StoryView_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var selected = (ReelItemWrapper)StoryView.SelectedItem;
            this.Log(e.Key);
            switch (e.Key)
            {
                case VirtualKey.GamepadRightTrigger when MoreNextReels:
                    e.Handled = true;
                    NextReelButtonClick(this, null);
                    break;
                case VirtualKey.GamepadLeftTrigger when MorePreviousReels:
                    e.Handled = true;
                    PreviousReelButtonClick(this, null);
                    break;
                case VirtualKey.Space:
                    e.Handled = true;
                    StoryView.ContainerFromItem(selected)?.FindDescendant<TextBox>()?.Focus(FocusState.Programmatic);
                    break;
                case VirtualKey.GamepadX:
                    e.Handled = true;
                    var button = StoryView.ContainerFromItem(selected)?.FindDescendantByName("ReactButton") as Control;
                    if (button?.Visibility == Visibility.Visible)
                    {
                        button.Focus(FocusState.Programmatic);
                    }
                    else
                    {
                        (StoryView.ContainerFromItem(selected)?.FindDescendantByName("ReplyButton") as Control)?.Focus(
                            FocusState.Programmatic);
                    }
                    break;
                case VirtualKey.GamepadY:
                    e.Handled = true;
                    UserInfo_OnTapped(this, null);
                    break;
            }
        }
    }
}
