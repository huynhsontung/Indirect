using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Indirect.Converters;
using Indirect.Wrapper;
using InstagramAPI;
using InstagramAPI.Classes;
using InstagramAPI.Utils;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    sealed partial class ThreadDetailsView : UserControl
    {
        public static readonly DependencyProperty ThreadProperty = DependencyProperty.Register(
            nameof(Thread),
            typeof(InstaDirectInboxThreadWrapper),
            typeof(ThreadDetailsView),
            new PropertyMetadata(null, OnThreadChanged));

        public InstaDirectInboxThreadWrapper Thread
        {
            get => (InstaDirectInboxThreadWrapper) GetValue(ThreadProperty);
            set => SetValue(ThreadProperty, value);
        }

        private bool _needUpdateCaret;   // For moving the caret to the end of text on thread change. This is a bad idea. ¯\_(ツ)_/¯

        private static void OnThreadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ThreadDetailsView)d;
            var thread = e.NewValue as InstaDirectInboxThreadWrapper;
            if (e.OldValue is InstaDirectInboxThreadWrapper oldThread) oldThread.PropertyChanged -= view.OnThreadPropertyChanged;
            if (thread == null) return;
            thread.PropertyChanged -= view.OnThreadPropertyChanged;   // Redundant. Just making sure it already unregistered.
            thread.PropertyChanged += view.OnThreadPropertyChanged;

            view.ViewProfileAppBarButton.Visibility = thread.Users?.Count == 1 ? Visibility.Visible : Visibility.Collapsed;
            view.MessageInputGrid.Visibility = thread.Pending ? Visibility.Collapsed : Visibility.Visible;
            view.RefreshButton.Visibility = thread.Pending ? Visibility.Collapsed : Visibility.Visible;
            view._needUpdateCaret = true;
            view.OnUserPresenceChanged();
        }

        private static ApiContainer ViewModel => ((App)Application.Current).ViewModel;

        public ThreadDetailsView()
        {
            this.InitializeComponent();
            ViewModel.PropertyChanged += async (sender, args) =>
            {
                if (args.PropertyName != nameof(ApiContainer.UserPresenceDictionary) && !string.IsNullOrEmpty(args.PropertyName)) return;
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, OnUserPresenceChanged);
            };
            GifPicker.ImageSelected += (sender, media) => GifPickerFlyout.Hide();
        }

        private void OnUserPresenceChanged()
        {
            if (Thread == null) return;
            if (Thread.Users.Count > 1)
            {
                LastActiveText.Visibility = Visibility.Collapsed;
                return;
            }
            if (ViewModel.UserPresenceDictionary.TryGetValue(Thread.Users[0].Pk, out var value))
            {
                LastActiveText.Visibility = Visibility.Visible;
                if (value.IsActive)
                {
                    LastActiveText.Text = "Active now";
                }
                else if (value.LastActivityAtMs != null)
                {
                    var converter = new RelativeTimeConverter();
                    LastActiveText.Text =
                        $"Active {converter.Convert(value.LastActivityAtMs, typeof(DateTimeOffset), null, "en-us")}";
                }
                else
                {
                    LastActiveText.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                LastActiveText.Visibility = Visibility.Collapsed;
            }
        }

        private void RefreshThread_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateInboxAndSelectedThread();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (Thread == null) return;
            var message = MessageTextBox.Text;
            Thread.DraftMessage = string.Empty;
            if(string.IsNullOrEmpty(message))
            {
                _ = ViewModel.SendLike();
            }
            else
            {
                _ = ViewModel.SendMessage(message);
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
            picker.FileTypeFilter.Add(".png");
            // picker.FileTypeFilter.Add(".mp4");   // todo: to be tested

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;
            if (file.ContentType.Contains("image", StringComparison.OrdinalIgnoreCase))
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

            if (file.ContentType.Contains("video", StringComparison.OrdinalIgnoreCase))
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

        private static async void DisplayFailDialog(string reason)
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

            void UploadAction(UploaderProgress progress)
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
                // Rely on sync client for update
            }

            if (FilePickerPreview.Source is StorageFile file)
            {
                _ = ViewModel.SendFile(file, UploadAction);
            }

            if (FilePickerPreview.Source is IRandomAccessStreamWithContentType stream)
            {
                _ = ViewModel.SendStream(stream, UploadAction);
            }
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
                    FilePickerPreview.Source = await imageStream.OpenReadAsync();
                    FilePickerFlyout.ShowAt(AddFilesButton);
                }
            }
        }

        private void MessageTextBox_OnEnterPressed(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            if (!string.IsNullOrEmpty(MessageTextBox.Text))
                SendButton_Click(sender, new RoutedEventArgs());
        }

        private async void OnThreadPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName != nameof(Thread.IsSomeoneTyping) &&
                args.PropertyName != nameof(Thread.ShowSeenIndicator) &&
                !string.IsNullOrEmpty(args.PropertyName)) return;
            if (!Thread.IsSomeoneTyping && !Thread.ShowSeenIndicator) return;
            var chatItemsStackPanel = (ItemsStackPanel) ItemsHolder.ItemsPanelRoot;
            Debug.WriteLine($"LastVisibleIndex: {chatItemsStackPanel?.LastVisibleIndex}");
            Debug.WriteLine($"FirstVisibleIndex: {chatItemsStackPanel?.FirstVisibleIndex}");
            Debug.WriteLine($"Last index in ObservableItems: {Thread.ObservableItems.Count - 1}");
            if (chatItemsStackPanel?.LastVisibleIndex == Thread.ObservableItems.Count - 1 ||
                chatItemsStackPanel?.LastVisibleIndex == Thread.ObservableItems.Count - 2)
            {
                await Task.Delay(100);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        ItemsHolder.ScrollIntoView(ItemsHolder.Footer);
                    });
            }
        }

        private async void ShowUserInfoFlyout(object sender, RoutedEventArgs e)
        {
            if (Thread?.Users == null || Thread.Users.Count == 0) return;
            if (Thread.Users.Count > 1 || string.IsNullOrEmpty(Thread.Users[0].Username)) return;
            if (Thread.DetailedUserInfo == null)
            {
                var userInfoResult = await InstagramAPI.Instagram.Instance.GetUserInfoAsync(Thread.Users[0].Pk);
                if (!userInfoResult.IsSucceeded) return;
                Thread.DetailedUserInfo = userInfoResult.Value;
            }

            UserInfoView.User = Thread.DetailedUserInfo;
            UserInfoFlyout.ShowAt((FrameworkElement) sender);
        }

        private void MessageTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_needUpdateCaret) return;
            var tb = (TextBox) sender;
            tb.SelectionStart = tb.Text.Length;
            _needUpdateCaret = false;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            if (e.DragUIOverride == null) return;
            if (e.DataView.Contains(StandardDataFormats.StorageItems) || 
                e.DataView.Contains(StandardDataFormats.Bitmap))
            {
                e.DragUIOverride.Caption = "Send";
            }
            else
            {
                e.DragUIOverride.IsCaptionVisible = false;
            }
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            var count = 0;
            void MultiUploadAction(UploaderProgress progress)
            {
                if (progress.UploadState != InstaUploadState.Completed &&
                    progress.UploadState != InstaUploadState.Error) return;
                count--;
                if (count <= 0) UploadProgress.Visibility = Visibility.Collapsed;
            }

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    var file = (StorageFile) item;
                    if (file.FileType.EndsWith("png", StringComparison.OrdinalIgnoreCase) ||
                        file.FileType.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase) ||
                        file.FileType.EndsWith("jpg", StringComparison.OrdinalIgnoreCase))
                    {
                        UploadProgress.Visibility = Visibility.Visible;
                        count++;
                        await ViewModel.SendFile(file, MultiUploadAction);
                    }
                }
            }

            if (e.DataView.Contains(StandardDataFormats.Bitmap))
            {
                var reference = await e.DataView.GetBitmapAsync();
                var bitmap = await reference.OpenReadAsync();
                UploadProgress.Visibility = Visibility.Visible;
                count++;
                await ViewModel.SendStream(bitmap, MultiUploadAction);
            }

            if (e.DataView.Contains(StandardDataFormats.WebLink))
            {
                var link = await e.DataView.GetWebLinkAsync();
                MessageTextBox.Text = link.ToString();
            }

            if (e.DataView.Contains(StandardDataFormats.Text))
            {
                var text = await e.DataView.GetTextAsync();
                MessageTextBox.Text = text;
            }
        }

        private void SeenIndicator_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (Thread.UsersSeenLatestMessage.Count > 3)
            {
                FlyoutBase.ShowAttachedFlyout(SeenIndicator);
            }
        }

        private async void OpenInNewWindow_OnClick(object sender, RoutedEventArgs e)
        {
            var viewmodel = ((App) Application.Current).ViewModel;
            await viewmodel.OpenThreadInNewWindow(Thread);
        }
    }
}
