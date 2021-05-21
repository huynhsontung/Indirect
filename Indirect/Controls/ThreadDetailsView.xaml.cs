using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.ViewManagement.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Indirect.Converters;
using Indirect.Entities.Wrappers;
using Indirect.Utilities;
using InstagramAPI.Classes;
using InstagramAPI.Classes.User;
using InstagramAPI.Utils;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect.Controls
{
    sealed partial class ThreadDetailsView : UserControl
    {
        public static readonly DependencyProperty ThreadProperty = DependencyProperty.Register(
            nameof(Thread),
            typeof(DirectThreadWrapper),
            typeof(ThreadDetailsView),
            new PropertyMetadata(null, OnThreadChanged));

        public static readonly DependencyProperty NewWindowButtonVisibilityProperty = DependencyProperty.Register(
            nameof(NewWindowButtonVisibility),
            typeof(Visibility),
            typeof(ThreadDetailsView),
            new PropertyMetadata(Visibility.Visible));

        public static readonly DependencyProperty ThreadHeaderVisibilityProperty = DependencyProperty.Register(
            nameof(ThreadHeaderVisibility),
            typeof(Visibility),
            typeof(ThreadDetailsView),
            new PropertyMetadata(Visibility.Visible));

        public DirectThreadWrapper Thread
        {
            get => (DirectThreadWrapper) GetValue(ThreadProperty);
            set => SetValue(ThreadProperty, value);
        }

        public Visibility NewWindowButtonVisibility
        {
            get => (Visibility) GetValue(NewWindowButtonVisibilityProperty);
            set => SetValue(NewWindowButtonVisibilityProperty, value);
        }

        public Visibility ThreadHeaderVisibility
        {
            get => (Visibility)GetValue(ThreadHeaderVisibilityProperty);
            set => SetValue(ThreadHeaderVisibilityProperty, value);
        }


        private bool _needUpdateCaret;   // For moving the caret to the end of text on thread change. This is a bad idea. ¯\_(ツ)_/¯

        private static void OnThreadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ThreadDetailsView)d;
            var thread = e.NewValue as DirectThreadWrapper;
            if (e.OldValue is DirectThreadWrapper oldThread) oldThread.PropertyChanged -= view.OnThreadPropertyChanged;
            if (thread == null) return;
            thread.PropertyChanged -= view.OnThreadPropertyChanged;   // Redundant. Just making sure it already unregistered.
            thread.PropertyChanged += view.OnThreadPropertyChanged;

            view.ViewProfileAppBarButton.Visibility = thread.Users?.Count == 1 ? Visibility.Visible : Visibility.Collapsed;
            view.MessageInputGrid.Visibility = thread.Pending ? Visibility.Collapsed : Visibility.Visible;
            view._needUpdateCaret = true;
            view.OnUserPresenceChanged();
        }

        private static MainViewModel ViewModel => ((App)Application.Current).ViewModel;

        public ThreadDetailsView()
        {
            this.InitializeComponent();
            ViewModel.PropertyChanged += OnUserPresenceChanged;
            GifPicker.ImageSelected += (sender, media) => GifPickerFlyout.Hide();
        }

        public void UnsubscribeHandlers()
        {
            ViewModel.PropertyChanged -= OnUserPresenceChanged;
            ProfilePictureView.UnsubscribeHandlers();
        }

        private async void OnUserPresenceChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName != nameof(MainViewModel.UserPresenceDictionary) && !string.IsNullOrEmpty(args.PropertyName)) return;
            try
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, OnUserPresenceChanged);
            }
            catch (InvalidComObjectException exception)
            {
                // This happens when ContactPanel is closed but this view still listens to event from viewmodel
                DebugLogger.LogException(exception, false);
            }
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

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (Thread == null) return;
            var message = MessageTextBox.Text;
            Thread.DraftMessage = string.Empty;
            if(string.IsNullOrEmpty(message))
            {
                await ViewModel.ChatService.SendLike(Thread);
            }
            else
            {
                message = message.Trim(' ', '\n', '\r');
                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

                if (Thread.ReplyingItem != null)
                {
                    var replyingItem = Thread.ReplyingItem;
                    Thread.ReplyingItem = null;
                    await ViewModel.ChatService.ReplyToItem(replyingItem, message);
                } 
                else
                {
                    var links = Helpers.ExtractLinks(message);
                    if (links.Count > 0)
                    {
                        await ViewModel.ChatService.SendLink(Thread, message, links);
                    }
                    else
                    {
                        var responseThread = await ViewModel.ChatService.SendTextMessage(Thread, message);
                        if (responseThread != null)
                        {
                            Thread.Update(responseThread);
                        }
                    }
                }
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
                _ = ViewModel.ChatService.SendFile(Thread, file, UploadAction);
            }

            if (FilePickerPreview.Source is IRandomAccessStreamWithContentType stream)
            {
                _ = ViewModel.ChatService.SendStream(Thread, stream, UploadAction);
            }
            FilePickerFlyout.Hide();
        }

        private async void MessageTextBox_OnCtrlV(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var dataPackage = Clipboard.GetContent();
            if (dataPackage.Contains(StandardDataFormats.Bitmap))
            {
                var imageStream = await dataPackage.GetBitmapAsync();
                FilePickerPreview.Source = await imageStream.OpenReadAsync();
                FilePickerFlyout.ShowAt(AddFilesButton);
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
            if (args.PropertyName == nameof(Thread.ReplyingItem) && Thread.ReplyingItem != null)
            {
                MessageTextBox.Focus(FocusState.Programmatic);
            }

            if (!Thread.IsSomeoneTyping || args.PropertyName != nameof(Thread.IsSomeoneTyping) &&
                !string.IsNullOrEmpty(args.PropertyName))
            {
                return;
            }

            var chatItemsStackPanel = (ItemsStackPanel) ItemsHolder.ItemsPanelRoot;
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

        private async void UserList_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var user = (BaseUser) e.ClickedItem;
            var pk = user.Pk;
            await ShowSingleUserInfoFlyout(pk, (FrameworkElement)sender);
        }

        private async void ShowUsersInfoFlyout(object sender, RoutedEventArgs e)
        {
            if (Thread?.Users == null || Thread.Users.Count == 0) return;
            if (Thread.Users.Count > 1)
            {
                UserListFlyout.ShowAt((FrameworkElement) sender);
                return;
            }

            var pk = Thread.Users[0].Pk;
            await ShowSingleUserInfoFlyout(pk, (FrameworkElement) sender);
        }

        private async Task ShowSingleUserInfoFlyout(long pk, FrameworkElement element)
        {
            if (!Thread.DetailedUserInfoDictionary.ContainsKey(pk))
            {
                var userInfoResult = await InstagramAPI.Instagram.Instance.GetUserInfoAsync(pk);
                if (!userInfoResult.IsSucceeded) return;
                Thread.DetailedUserInfoDictionary[pk] = userInfoResult.Value;
            }

            UserInfoView.User = Thread.DetailedUserInfoDictionary[pk];
            UserInfoFlyout.ShowAt(element);
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
                        await ViewModel.ChatService.SendFile(Thread, file, MultiUploadAction);
                    }
                }
            }

            if (e.DataView.Contains(StandardDataFormats.Bitmap))
            {
                var reference = await e.DataView.GetBitmapAsync();
                var bitmap = await reference.OpenReadAsync();
                UploadProgress.Visibility = Visibility.Visible;
                count++;
                await ViewModel.ChatService.SendStream(Thread, bitmap, MultiUploadAction);
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

        private async void OpenInNewWindow_OnClick(object sender, RoutedEventArgs e)
        {
            var viewmodel = ((App) Application.Current).ViewModel;
            await viewmodel.OpenThreadInNewWindow(Thread);
        }

        private void InsertEmojiButton_OnClick(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Focus(FocusState.Programmatic);
            CoreInputView.GetForCurrentView().TryShow(CoreInputViewKind.Emoji);
        }

        private void ClearReplyButton_OnClick(object sender, RoutedEventArgs e)
        {
            Thread.ReplyingItem = null;
        }
    }
}
