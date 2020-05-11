using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Indirect.Wrapper;

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
            var autoVideo = flipViewItem?.ContentTemplateRoot as AutoVideoControl;
            autoVideo?.Pause();
        }
    }
}
