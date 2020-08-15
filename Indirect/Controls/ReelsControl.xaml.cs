using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Indirect.Wrapper;
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
                return Source.UserOrder.IndexOf(story.Parent.User.Pk) != 0;
            }
        }

        private bool MoreNextReels
        {
            get
            {
                var story = (ReelItemWrapper)StoryView.SelectedItem;
                if (story == null) return true;
                return Source.UserOrder.LastIndexOf(story.Parent.User.Pk) != Source.UserOrder.Count - 1;
            }
        }

        private Tuple<int, int> _reelLimit;

        public ReelsControl()
        {
            this.InitializeComponent();
        }

        private void StoryView_OnLoaded(object sender, RoutedEventArgs e)
        {
            var storyView = (FlipView) sender;
            Source?.OnLoaded(storyView);
        }

        public void OnClose()
        {
            var flipViewItem = StoryView.ContainerFromIndex(StoryView.SelectedIndex) as FlipViewItem;
            var grid = flipViewItem?.ContentTemplateRoot as Grid;
            var autoVideo = grid.FindDescendant<AutoVideoControl>();
            autoVideo?.Pause();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var story = (ReelItemWrapper) StoryView.SelectedItem;
            var container = StoryView.ContainerFromItem(story) as FlipViewItem;
            var textBox = container.FindDescendant<TextBox>();
            if (string.IsNullOrEmpty(textBox?.Text) || story == null) return;
            var message = textBox.Text;
            textBox.Text = string.Empty;
            await story.Reply(message);
        }

        private void StoryView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var storyView = (FlipView)sender;
            Source?.OnSelectionChanged(storyView.SelectedIndex);
            //StoryViewOnLoadedAndOnSelectionChanged();
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
            else if (selected.Parent.Id != previous?.Parent.Id)
            {
                var start = 0;
                var end = 0;
                for (int i = 0; i < Source.Items.Count; i++)
                {
                    if (selected.Parent.Id == Source.Items[i].Parent.Id)
                    {
                        start = i;
                        break;
                    }
                }

                for (int i = Source.Items.Count - 1; i >= 0; i--)
                {
                    if (selected.Parent.Id == Source.Items[i].Parent.Id)
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
            else
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
            var userId = items[selectedIndex].Parent.User.Pk;
            var previousReelIndex = -1;
            for (int i = selectedIndex; i >= 0; i--)
            {
                if (userId == items[i].Parent.User.Pk) continue;
                previousReelIndex = i;
                userId = items[i].Parent.User.Pk;
                break;
            }

            if (previousReelIndex == -1) return;
            var previousStories = items.Where(x => x.Parent.User.Pk == userId).ToArray();
            var unseenStory = previousStories.FirstOrDefault(x => x.TakenAt > x.Parent.Seen);
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
            var userId = items[selectedIndex].Parent.User.Pk;
            var nextReelIndex = -1;
            for (int i = selectedIndex; i < items.Count; i++)
            {
                if (nextReelIndex == -1)
                {
                    if (userId == items[i].Parent.User.Pk) continue;
                    nextReelIndex = i;
                    userId = items[i].Parent.User.Pk;
                }
                else
                {
                    // Go to unseen story
                    if (userId != items[i].Parent.User.Pk) break;
                    if (items[i].TakenAt > items[i].Parent.Seen)
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
            var userId = (StoryView.SelectedItem as ReelItemWrapper)?.User.Pk ?? 0;
            if (userId == 0) return;
            if (UserInfoView.User?.Pk != userId)
            {
                var userInfoResult = await InstagramAPI.Instagram.Instance.GetUserInfoAsync(userId);
                if (!userInfoResult.IsSucceeded) return;
                UserInfoView.User = userInfoResult.Value;
            }
            FlyoutBase.ShowAttachedFlyout(UserInfoGrid);
        }
    }
}
