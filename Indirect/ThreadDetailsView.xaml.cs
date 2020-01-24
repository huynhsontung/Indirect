using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Indirect.Wrapper;
using InstaSharper.Enums;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect
{
    sealed partial class ThreadDetailsView : UserControl
    {
        public static readonly DependencyProperty ThreadProperty = DependencyProperty.Register(
            nameof(Thread),
            typeof(InstaDirectInboxThreadWrapper),
            typeof(ThreadDetailsView),
            new PropertyMetadata(null));
        // public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        //     nameof(ViewModel),
        //     typeof(ApiContainer),
        //     typeof(ThreadDetailsView),
        //     new PropertyMetadata(null));

        public InstaDirectInboxThreadWrapper Thread
        {
            get => (InstaDirectInboxThreadWrapper) GetValue(ThreadProperty);
            set => SetValue(ThreadProperty, value);
        }

        private ApiContainer _viewModel;
        public ApiContainer ViewModel
        {
            get
            {
                if (_viewModel == null && CoreApplication.Properties.ContainsKey(App.VIEW_MODEL_PROP_NAME))
                {
                    _viewModel = (ApiContainer) CoreApplication.Properties[App.VIEW_MODEL_PROP_NAME];
                }

                return _viewModel;
            }
        }

        private static void OnThreadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ThreadDetailsView) d;
            // view.Bindings.Update();
        }


        public ThreadDetailsView()
        {
            this.InitializeComponent();
        }

        private void RefreshThread_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateInboxAndSelectedThread();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var message = MessageTextBox.Text;
            MessageTextBox.Text = "";
            if(string.IsNullOrEmpty(message))
            {
                ViewModel.SendLike();
            }
            else
            {
                ViewModel.SendMessage(message);
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
            // picker.FileTypeFilter.Add(".mp4");   // todo: to be tested

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;
            if (file.ContentType.Contains("image"))
            {
                var properties = await file.GetBasicPropertiesAsync();
                if (properties.Size >= 5e7)
                {
                    DisplayFailDialog("Image too large.");
                    return;
                }

                var imageProps = await file.Properties.GetImagePropertiesAsync();
                var ratio = (double) imageProps.Width / imageProps.Height;
                if (ratio < 0.4 || ratio > 2.5)
                {
                    DisplayFailDialog("Image does not have valid aspect ratio.");
                    return;
                }
            }

            if (file.ContentType.Contains("video"))
            {
                var properties = await file.Properties.GetVideoPropertiesAsync();
                if (properties.Duration > TimeSpan.FromMinutes(1))
                {
                    DisplayFailDialog("Video too long. Please pick video shorter than 1 minute.");
                    return;
                }
            }
            FilePickerPreview.Source = file;
            FilePickerFlyout.ShowAt(AddFilesButton);
        }

        private async void DisplayFailDialog(string reason)
        {
            var dialog = new ContentDialog()
            {
                Title = "File not valid",
                Content = reason,
                CloseButtonText = "Close"
            };
            await dialog.ShowAsync();
        }

        private void FilePickerFlyout_OnClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
        {
            FilePickerPreview.PauseVideo();
        }

        private void SendFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            UploadProgress.Visibility = Visibility.Visible;
            var file = (StorageFile) FilePickerPreview.Source;
            ViewModel.SendFile(file, progress =>
            {
                if (progress.UploadState != InstaUploadState.Completed &&
                    progress.UploadState != InstaUploadState.Error) return;
                UploadProgress.Visibility = Visibility.Collapsed;
                if (progress.UploadState == InstaUploadState.Error)
                {
                    var dialog = new ContentDialog()
                    {
                        Title = "Upload failed",
                        CloseButtonText = "Close"
                    };
                    _ = dialog.ShowAsync();
                }
                else
                {
                    ViewModel.UpdateInboxAndSelectedThread();
                }
            });
            FilePickerFlyout.Hide();
        }

        private async void Details_OnProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
        {
            if (args.Key == VirtualKey.V && args.Modifiers == VirtualKeyModifiers.Control)
            {
                var dataPackage = Clipboard.GetContent();
                if (dataPackage.Contains(StandardDataFormats.Bitmap))
                {
                    var imageStream = await dataPackage.GetBitmapAsync();
                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(await imageStream.OpenReadAsync());
                    FilePickerPreview.Source = bitmapImage;
                    FilePickerFlyout.ShowAt(AddFilesButton);
                }
            }
        }

        private void MessageTextBox_OnProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
        {
            if (args.Key == VirtualKey.Enter && args.Modifiers == VirtualKeyModifiers.None)
            {
                args.Handled = true;
                if (!string.IsNullOrEmpty(MessageTextBox.Text))
                    SendButton_Click(sender, new RoutedEventArgs());
            }

        }
    }
}
