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
            var message = story?.DraftMessage;
            if (string.IsNullOrEmpty(message)) return;
            story.DraftMessage = string.Empty;
            await ApiContainer.Instance.ReelsFeed.ReplyToStory(story, message);
        }

        private void MessageTextBox_OnProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
        {
            var messageTextBox = (TextBox) sender;
            if (args.Key == VirtualKey.Enter && args.Modifiers == VirtualKeyModifiers.None)
            {
                args.Handled = true;
                if (!string.IsNullOrEmpty(messageTextBox.Text))
                    SendButton_Click(sender, null);
            }
        }

        private void StoryView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var storyView = (FlipView)sender;
            var previous = e.RemovedItems.FirstOrDefault();
            var selected = e.AddedItems.FirstOrDefault();
            var flipViewItem = storyView.ContainerFromItem(previous) as FlipViewItem;
            var element = flipViewItem?.ContentTemplateRoot as FrameworkElement;
            var autoVideo = element?.FindDescendant<AutoVideoControl>();
            autoVideo?.Pause();

            //flipViewItem = storyView.ContainerFromItem(selected) as FlipViewItem;
            //element = flipViewItem?.ContentTemplateRoot as FrameworkElement;
            //autoVideo = element?.FindDescendant<AutoVideoControl>();
            //autoVideo?.Play();
        }
    }
}
