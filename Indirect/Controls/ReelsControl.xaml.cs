using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Indirect.Wrapper;
using InstagramAPI.Classes.Story;
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

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
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
    }
}
