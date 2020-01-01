using System;
using System.Collections.Generic;
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

namespace Indirect
{
    sealed partial class ThreadDetailsView : UserControl
    {
        public static readonly DependencyProperty ThreadProperty = DependencyProperty.Register(
            nameof(Thread),
            typeof(InstaDirectInboxThreadWrapper),
            typeof(ThreadDetailsView),
            new PropertyMetadata(null, OnThreadChanged));
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ApiContainer),
            typeof(ThreadDetailsView),
            new PropertyMetadata(null));

        public InstaDirectInboxThreadWrapper Thread
        {
            get => (InstaDirectInboxThreadWrapper) GetValue(ThreadProperty);
            set => SetValue(ThreadProperty, value);
        }

        public ApiContainer ViewModel
        {
            get => (ApiContainer) GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        private static void OnThreadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ThreadDetailsView) d;
            view.HandleThreadChanged();
        }


        public ThreadDetailsView()
        {
            this.InitializeComponent();
            // GotFocus += (sender, args) =>
            // {
            //     MessageTextBox.Focus(FocusState.Programmatic);
            // };
        }


        private void HandleThreadChanged()
        {
            this.Bindings.Update();
        }

        private void RefreshThread_OnClick(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.UpdateInboxAndSelectedThread();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var message = MessageTextBox.Text;
            MessageTextBox.Text = "";
            _ = string.IsNullOrEmpty(message) ? ViewModel.SendLike() : ViewModel.SendMessage(message);
        }

        private void MessageTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && !string.IsNullOrEmpty(MessageTextBox.Text))
            {
                SendButton_Click(sender, e);
            }
        }

        private async void AddFilesButton_OnClick(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder
            };
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".mp4");

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;
            FilePickerPreview.Source = file;
            FilePickerFlyout.ShowAt(AddFilesButton);
        }
    }
}
