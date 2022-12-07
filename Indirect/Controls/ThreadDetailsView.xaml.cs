using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.ViewManagement.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Indirect.Converters;
using Indirect.Entities.Wrappers;
using Indirect.Pages;
using Indirect.Services;
using InstagramAPI.Classes;
using InstagramAPI.Classes.User;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.UI.Xaml.Controls;
using System.Numerics;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Hosting;
using Indirect.Utilities;
using CommunityToolkit.Mvvm.Messaging;
using Indirect.Entities.Messages;

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

        public DirectThreadWrapper Thread
        {
            get => (DirectThreadWrapper)GetValue(ThreadProperty);
            set => SetValue(ThreadProperty, value);
        }

        public bool IsNewWindow { get; set; }

        private long _firstUserId;
        private bool _needUpdateCaret;   // For moving the caret to the end of text on thread change. This is a bad idea. ¯\_(ツ)_/¯
        private readonly DispatcherQueue _dispatcherQueue;
        private SizeChangedEventHandler _sizeChangedHandler;

        private static void OnThreadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ThreadDetailsView)d;
            var thread = (DirectThreadWrapper)e.NewValue;
            if (thread == null) return;

            view.ViewProfileAppBarButton.Visibility = thread.Users?.Count == 1 ? Visibility.Visible : Visibility.Collapsed;
            view.MessageInputGrid.Visibility = thread.Source.Pending ? Visibility.Collapsed : Visibility.Visible;
            view._needUpdateCaret = true;
            view._firstUserId = thread.Users?.FirstOrDefault()?.Pk ?? default;
            view.OnUserPresenceChanged();
            view.ConditionallyShowTeachingTips();
        }

        private static MainViewModel ViewModel => ((App)Application.Current).ViewModel;

        public ThreadDetailsView()
        {
            this.InitializeComponent();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _sizeChangedHandler = new SizeChangedEventHandler(ItemContainer_SizeChanged);
            if (!DeviceFamilyHelpers.MultipleViewsSupport)
            {
                NewWindowButton.Visibility = Visibility.Collapsed;
            }
            
            GifPicker.ImageSelected += (sender, media) => GifPickerFlyout.Hide();
            TypingIndicator.RegisterPropertyChangedCallback(VisibilityProperty, TypingIndicator_OnVisibilityChanged);
            Loading += OnLoading;

            WeakReferenceMessenger.Default.Register<UserPresenceUpdatedMessage>(this, (r, m) =>
            {
                ThreadDetailsView view = (ThreadDetailsView)r;
                if (view._firstUserId != m.UserId) return;
                view._dispatcherQueue.TryEnqueue(view.OnUserPresenceChanged);
            });
        }

        private void OnLoading(FrameworkElement sender, object args)
        {
            if (IsNewWindow)
            {
                RootGrid.RowDefinitions[0].Height = new GridLength(0);
                ContentGrid.RowDefinitions[0].Height = new GridLength(82);
                ContentGrid.CornerRadius = new CornerRadius(0);
                ContentGrid.BorderThickness = new Thickness(0);
                BorderThicknessSetter.Value = new Thickness(0);
                NewWindowButton.Visibility = Visibility.Collapsed;
            }
        }

        private void TypingIndicator_OnVisibilityChanged(DependencyObject sender, DependencyProperty dp)
        {
            var chatItemsStackPanel = (ItemsStackPanel)ItemsHolder.ItemsPanelRoot;
            if (chatItemsStackPanel?.LastVisibleIndex == Thread.ObservableItems.Count - 1 ||
                chatItemsStackPanel?.LastVisibleIndex == Thread.ObservableItems.Count - 2)
            {
                if (Thread.IsSomeoneTyping)
                {
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        ItemsHolder.ScrollIntoView(ItemsHolder.Footer);
                    });
                }
            }
        }

        private async void ConditionallyShowTeachingTips()
        {
            if (!SettingsService.TryGetGlobal(nameof(SendButtonTeachingTip), out bool? b1) || (b1 ?? true))
            {
                await Task.Delay(1000);
                SendButtonTeachingTip.IsOpen = true;
                SendButtonTeachingTip.Closed += (sender, args) => SendEmojiTeachingTip.IsOpen = true;
                SettingsService.SetGlobal(nameof(SendButtonTeachingTip), false);
            }
            else if (!SettingsService.TryGetGlobal(nameof(SendEmojiTeachingTip), out bool? b2) || (b2 ?? true))
            {
                await Task.Delay(1000);
                SendEmojiTeachingTip.IsOpen = true;
                SettingsService.SetGlobal(nameof(SendEmojiTeachingTip), false);
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
            MessageTextBox.Text = string.Empty;
            MessageTextBox.Focus(FocusState.Programmatic);
            await ViewModel.ChatService.SendMessage(Thread, message);
        }

        private async void SendFilesButton_OnClick(object sender, RoutedEventArgs e)
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
                var ratio = (double)imageProps.Width / imageProps.Height;
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
            FilePickerFlyout.ShowAt(SendFilesButton);
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

        private void UploadAction(UploaderProgress progress)
        {
            if (progress.UploadState != InstaUploadState.Completed &&
                progress.UploadState != InstaUploadState.Error) return;
            UploadProgress.Visibility = Visibility.Collapsed;
            if (progress.UploadState == InstaUploadState.Error)
            {
                var frame = Window.Current?.Content as Frame;
                if (frame?.Content is MainPage page)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        page.ShowStatus("Upload failed", progress.Message, InfoBarSeverity.Error, 5);
                    });
                }
                else
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Upload failed",
                        Content = progress.Message,
                        CloseButtonText = "Close"
                    };
                    _ = dialog.ShowAsync();
                }

            }
            // Rely on sync client for update
        }

        private void SendFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            UploadProgress.Visibility = Visibility.Visible;

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
            try
            {
                var dataPackage = Clipboard.GetContent();
                if (dataPackage.Contains(StandardDataFormats.Bitmap))
                {
                    var imageStream = await dataPackage.GetBitmapAsync();
                    FilePickerPreview.Source = await imageStream.OpenReadAsync();
                    FilePickerFlyout.ShowAt(SendFilesButton);
                }
            }
            catch (Exception)
            {
                if (Window.Current.Content is MainPage mainPage)
                {
                    mainPage.ShowStatus("Cannot open clipboard", "Please check system settings and try again later", InfoBarSeverity.Error, 5);
                }
            }
        }

        private void MessageTextBox_OnEnterPressed(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            if (!string.IsNullOrEmpty(MessageTextBox.Text))
                SendButton_Click(sender, new RoutedEventArgs());
        }

        private async void UserList_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var user = (BaseUser)e.ClickedItem;
            var pk = user.Pk;
            await ShowSingleUserInfoFlyout(pk, (FrameworkElement)sender);
        }

        private async void ShowUsersInfoFlyout(object sender, RoutedEventArgs e)
        {
            if (Thread?.Users == null || Thread.Users.Count == 0) return;
            if (Thread.Users.Count > 1)
            {
                UserListFlyout.ShowAt((FrameworkElement)sender);
                return;
            }

            var pk = Thread.Users[0].Pk;
            await ShowSingleUserInfoFlyout(pk, (FrameworkElement)sender);
        }

        private async Task ShowSingleUserInfoFlyout(long pk, FrameworkElement element)
        {
            if (!Thread.DetailedUserInfoDictionary.ContainsKey(pk))
            {
                var userInfoResult = await ViewModel.InstaApi.GetUserInfoAsync(pk);
                if (!userInfoResult.IsSucceeded) return;
                Thread.DetailedUserInfoDictionary[pk] = userInfoResult.Value;
            }

            UserInfoView.User = Thread.DetailedUserInfoDictionary[pk];
            UserInfoFlyout.ShowAt(element);
        }

        private void MessageTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_needUpdateCaret) return;
            var tb = (TextBox)sender;
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
                    if (item is StorageFile file &&
                        (file.FileType.EndsWith("png", StringComparison.OrdinalIgnoreCase) ||
                         file.FileType.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase) ||
                         file.FileType.EndsWith("jpg", StringComparison.OrdinalIgnoreCase)))
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
            var viewmodel = ((App)Application.Current).ViewModel;
            await viewmodel.OpenThreadInNewWindow(Thread);
        }

        private void InsertEmojiButton_OnClick(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Focus(FocusState.Programmatic);
            CoreInputView.GetForCurrentView().TryShow(CoreInputViewKind.Emoji);
        }

        private void ClearReplyButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Thread != null)
            {
                Thread.ReplyingItem = null;
                MessageTextBox.Focus(FocusState.Programmatic);
            }
        }

        private async void SendButton_OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            args.Handled = true;
            var emoji = await EmojiPicker.ShowAsync((FrameworkElement)sender,
                new FlyoutShowOptions() { Placement = FlyoutPlacementMode.TopEdgeAlignedRight });
            if (string.IsNullOrEmpty(emoji))
            {
                return;
            }

            Thread.QuickReplyEmoji = emoji;
            MessageTextBox.Text = MessageTextBox.Text;
        }

        private void ItemsHolder_OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            if (FocusManager.GetFocusedElement() is ListViewItem item &&
                item.ContentTemplateRoot is ThreadItemControl itemControl &&
                itemControl.ContextFlyout != null &&
                itemControl.FindChild("MainContentControl") is FrameworkElement element)
            {
                args.Handled = true;
                itemControl.ContextFlyout.ShowAt(element,
                    new FlyoutShowOptions
                    {
                        Placement = itemControl.Item.FromMe
                            ? FlyoutPlacementMode.BottomEdgeAlignedRight
                            : FlyoutPlacementMode.BottomEdgeAlignedLeft
                    });
            }
        }

        private void ItemsHolder_OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Space when FocusManager.GetFocusedElement() is ListViewItem item &&
                                           item.ContentTemplateRoot is ThreadItemControl itemControl:
                    if (FocusManager.FindFirstFocusableElement(item) is Control control)
                    {
                        control.Focus(FocusState.Programmatic);
                    }
                    else
                    {
                        itemControl.OnItemClick();
                    }

                    e.Handled = true;
                    break;
            }
        }

        private void ThreadDetailsView_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.GamepadX:
                    e.Handled = true;
                    MessageTextBox.Focus(FocusState.Programmatic);
                    break;
                case VirtualKey.GamepadY:
                    e.Handled = true;
                    ShowUsersInfoFlyout(ViewProfileAppBarButton, null);
                    break;
            }
        }

        private async void CompactOverlayButton_OnClick(object sender, RoutedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();
            if (view.ViewMode == ApplicationViewMode.Default)
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.ViewSizePreference = ViewSizePreference.Custom;
                preferences.CustomSize = new Size(360, 300);
                if (await view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences))
                {
                    var result = VisualStateManager.GoToState(this, "ExitCompactOverlay", false);
                }
            }
            else
            {
                if (await view.TryEnterViewModeAsync(ApplicationViewMode.Default))
                {
                    var result = VisualStateManager.GoToState(this, "EnterCompactOverlay", false);
                }
            }
        }

        private async void AddAudioButton_OnClick(object sender, RoutedEventArgs e)
        {
            var audio = await SendAudioControl.ShowAsync((Button)sender, new FlyoutShowOptions());
            if (audio != null)
            {
                UploadProgress.Visibility = Visibility.Visible;
                await ViewModel.ChatService.SendVoiceClipAsync(Thread, audio, UploadAction);
                try
                {
                    await audio.AudioFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
                catch (Exception)
                {
                    // pass
                }
            }
        }

        private void ItemsHolder_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            args.RegisterUpdateCallback(2, (s, sizeChangedArgs) =>
            {
                sizeChangedArgs.ItemContainer.SizeChanged += _sizeChangedHandler;
            });
        }

        private void ItemContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var panel = ItemsHolder.ItemsPanelRoot as ItemsStackPanel;
            if (panel == null || e.PreviousSize.Height == e.NewSize.Height)
            {
                return;
            }

            var selector = sender as SelectorItem;

            var index = ItemsHolder.IndexFromContainer(selector);
            if (index < panel.LastVisibleIndex && e.PreviousSize.Width < 1 && e.PreviousSize.Height < 1)
            {
                return;
            }

            if (ItemsHolder.ItemFromContainer(selector) is DirectItemWrapper message && !message.IsInitialized)
            {
                if (e.PreviousSize.Width > 0 && e.PreviousSize.Height > 0)
                {
                    message.IsInitialized = true;
                }

                return;
            }

            if (index >= panel.FirstVisibleIndex && index <= panel.LastVisibleIndex)
            {
                var diff = (float)e.NewSize.Height - (float)e.PreviousSize.Height;
                if (Math.Abs(diff) < 2)
                {
                    return;
                }

                var batch = Window.Current.Compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

                var anim = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
                anim.InsertKeyFrame(0, new Vector3(0, (float)diff, 0));
                anim.InsertKeyFrame(1, new Vector3());
                //anim.Duration = TimeSpan.FromSeconds(5);

                for (int i = panel.FirstCacheIndex; i <= index; i++)
                {
                    var container = ItemsHolder.ContainerFromIndex(i) as SelectorItem;
                    if (container == null)
                    {
                        continue;
                    }

                    var child = VisualTreeHelper.GetChild(container, 0) as UIElement;

                    var visual = ElementCompositionPreview.GetElementVisual(child);
                    visual.StartAnimation("Offset", anim);
                }

                batch.End();
            }
        }
    }
}
