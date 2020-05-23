﻿using System;
using System.Diagnostics;
using System.Linq;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Indirect.Wrapper;
using Microsoft.Toolkit.Uwp.UI.Extensions;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    public sealed partial class ReelsControl : UserControl
    {
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

        private Tuple<int, int> _reelLimit;

        public ReelsControl()
        {
            this.InitializeComponent();
        }

        private void StoryView_OnLoaded(object sender, RoutedEventArgs e)
        {
            var storyView = (FlipView) sender;
            Source?.AttachSelector(storyView);
        }

        public void OnClose()
        {
            Source?.DetachSelector();
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
            if (string.IsNullOrEmpty(textBox?.Text)) return;
            var message = textBox.Text;
            textBox.Text = string.Empty;
            await ApiContainer.Instance.ReelsFeed.ReplyToStory(story, message);
        }

        private void StoryView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var storyView = (FlipView)sender;
            var previous = (StoryItemWrapper) e.RemovedItems.FirstOrDefault();
            var selected = (StoryItemWrapper) e.AddedItems.FirstOrDefault();
            var flipViewItem = storyView.ContainerFromItem(previous) as FlipViewItem;
            var element = flipViewItem?.ContentTemplateRoot as FrameworkElement;
            var autoVideo = element?.FindDescendant<AutoVideoControl>();
            autoVideo?.Pause();

            if (selected == null)
            {
                ReelsProgressBar.Value = 0;
            }
            else if (selected.Parent != previous?.Parent)
            {
                var start = 0;
                var end = 0;
                for (int i = 0; i < Source.Items.Count; i++)
                {
                    if (selected.Parent == Source.Items[i].Parent)
                    {
                        start = i;
                        break;
                    }
                }

                for (int i = Source.Items.Count - 1; i >= 0; i--)
                {
                    if (selected.Parent == Source.Items[i].Parent)
                    {
                        end = i;
                        break;
                    }
                }

                _reelLimit = new Tuple<int, int>(start, end);
                ReelsProgressBar.Value = 1d / (end - start + 1) * 100;
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

        private void MessageTextBox_OnKeyboardInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            SendButton_Click(sender, null);
        }
    }
}