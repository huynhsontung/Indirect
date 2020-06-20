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
            typeof(ReelsWrapper),
            typeof(ReelsControl),
            new PropertyMetadata(null));

        public ReelsWrapper Source
        {
            get => (ReelsWrapper) GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private bool MorePreviousReels
        {
            get
            {
                var story = (StoryItemWrapper)StoryView.SelectedItem;
                if (story == null) return true;
                return Source.UserOrder.IndexOf(story.Parent.Owner.Id) != 0;
            }
        }

        private bool MoreNextReels
        {
            get
            {
                var story = (StoryItemWrapper)StoryView.SelectedItem;
                if (story == null) return true;
                return Source.UserOrder.LastIndexOf(story.Parent.Owner.Id) != Source.UserOrder.Count - 1;
            }
        }

        private Tuple<int, int> _reelLimit;

        public ReelsControl()
        {
            this.InitializeComponent();
        }

        private void StoryViewOnLoadedAndOnSelectionChanged()
        {
            if (StoryView.SelectedIndex == -1) return;
            var flipViewItem = StoryView.ContainerFromIndex(StoryView.SelectedIndex) as FlipViewItem;
            var grid = flipViewItem?.ContentTemplateRoot as Grid;
            var autoVideo = grid?.FindDescendant<AutoVideoControl>();
            if (autoVideo == null) return;
            autoVideo.MediaPlayer.Volume = 0.5;
        }

        private void StoryView_OnLoaded(object sender, RoutedEventArgs e)
        {
            var storyView = (FlipView) sender;
            Source?.OnLoaded(storyView);
            //StoryViewOnLoadedAndOnSelectionChanged();
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
            var story = (StoryItemWrapper) StoryView.SelectedItem;
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
            var previous = (StoryItemWrapper) e.RemovedItems.FirstOrDefault();
            var selected = (StoryItemWrapper) e.AddedItems.FirstOrDefault();
            var flipViewItem = storyView.ContainerFromItem(previous) as FlipViewItem;
            var element = flipViewItem?.ContentTemplateRoot as FrameworkElement;
            var autoVideo = element?.FindDescendant<AutoVideoControl>();
            autoVideo?.Pause();

            UpdateProgressBar(selected, previous);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MorePreviousReels)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MoreNextReels)));
        }

        private void UpdateProgressBar(StoryItemWrapper selected, StoryItemWrapper previous)
        {
            if (selected == null)
            {
                ReelsProgressBar.Value = 0;
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
                ReelsProgressBar.Value = StoryView.SelectedIndex == end ? 100 : 1d / (end - start + 1) * 100;
            }
            else
            {
                var selectedIndex = StoryView.SelectedIndex;
                if (_reelLimit.Item2 < selectedIndex) ReelsProgressBar.Value = 0;
                else
                {
                    ReelsProgressBar.Value = (selectedIndex - _reelLimit.Item1 + 1d) /
                        (_reelLimit.Item2 - _reelLimit.Item1 + 1d) * 100;
                }
            }

            if (ReelsProgressBar.Value > 99) ReelsProgressBar.Value = 100;
        }

        private void MessageTextBox_OnEnterPressed(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            SendButton_Click(sender, null);
        }

        private void RedirectToThread(object sender, TappedRoutedEventArgs e)
        {
            if (!(Window.Current.Content is Frame frame)) return;
            if (!(frame.Content is MainPage mainPage)) return;
            var owner = (StoryView.SelectedItem as StoryItemWrapper)?.Owner;
            if (owner != null && !string.IsNullOrEmpty(owner.Username))
                ApiContainer.Instance.SearchWithoutThreads(owner.Username, async userList =>
                {
                    ApiContainer.Instance.NewMessageCandidates.Clear();
                    ApiContainer.Instance.NewMessageCandidates.Add(userList[0]);
                    mainPage.CloseImmersiveView();
                    await ApiContainer.Instance.CreateThread();
                });

        }

        private void PreviousReelButtonClick(object sender, RoutedEventArgs e)
        {
            var items = Source?.Items;
            var selectedIndex = StoryView.SelectedIndex;
            if (items == null || selectedIndex == -1) return;
            var userId = items[selectedIndex].Parent.Owner.Id;
            var previousReelIndex = -1;
            for (int i = selectedIndex; i >= 0; i--)
            {
                if (userId == items[i].Parent.Owner.Id) continue;
                previousReelIndex = i;
                userId = items[i].Parent.Owner.Id;
                break;
            }

            // Getting to the start of the reel
            for (int i = previousReelIndex; i >= 0; i--)
            {
                if (userId == items[i].Parent.Owner.Id && i == 0)
                {
                    previousReelIndex = 0;
                    break;
                }
                if (userId == items[i].Parent.Owner.Id) continue;
                previousReelIndex = i + 1;
                break;
            }

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
            var userId = items[selectedIndex].Parent.Owner.Id;
            var nextReelIndex = -1;
            for (int i = selectedIndex; i < items.Count; i++)
            {
                if (userId == items[i].Parent.Owner.Id) continue;
                nextReelIndex = i;
                break;
            }

            if (nextReelIndex != -1)
            {
                StoryView.SelectedIndex = nextReelIndex;
            }
        }
    }
}
