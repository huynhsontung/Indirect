using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Indirect.Controls;
using Indirect.Entities.Wrappers;
using Indirect.Utilities;
using InstagramAPI;
using InstagramAPI.Classes.User;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI;
using CoreWindowActivationState = Windows.UI.Core.CoreWindowActivationState;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Indirect.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static readonly DependencyProperty InboxProperty = DependencyProperty.Register(
            nameof(Inbox),
            typeof(InboxWrapper),
            typeof(MainPage),
            new PropertyMetadata(null));

        internal InboxWrapper Inbox
        {
            get => (InboxWrapper) GetValue(InboxProperty);
            set => SetValue(InboxProperty, value);
        }

        private MainViewModel ViewModel => ((App) Application.Current).ViewModel;
        private ObservableCollection<BaseUser> NewMessageCandidates { get; } = new ObservableCollection<BaseUser>();

        private readonly Windows.Storage.ApplicationDataContainer _localSettings =
            Windows.Storage.ApplicationData.Current.LocalSettings;


        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.SetTitleBar(TitleBarElement);
            MainLayout.ViewStateChanged += OnViewStateChange;
            Window.Current.Activated += OnWindowFocusChange;
            Inbox = ViewModel.Inbox;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e?.NavigationMode != NavigationMode.Back)
            {
                await ViewModel.OnLoggedIn();
            }
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var confirmDialog = new ContentDialog()
            {
                Title = "Are you sure?",
                Content = "Logging out will delete all session data, including cached images and videos.",
                CloseButtonText = "Close",
                PrimaryButtonText = "Logout", 
                DefaultButton = ContentDialogButton.Close
            };
            var confirmation = await confirmDialog.ShowAsync();
            if (confirmation != ContentDialogResult.Primary) return;
            ViewModel.Logout();
            Frame.Navigate(typeof(LoginPage));
        }
        
        private void DetailsBackButton_OnClick(object sender, RoutedEventArgs e) => ViewModel.SetSelectedThreadNull();

        private void OnWindowFocusChange(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                BackButton.IsEnabled = false;
                AppTitleTextBlock.Opacity = 0.5;
            }
            else
            {

                BackButton.IsEnabled = true;
                AppTitleTextBlock.Opacity = 1;
            }
        }

        private void OnViewStateChange(object sender, ListDetailsViewState state)
        {
            BackButton.Visibility = state == ListDetailsViewState.Details ? Visibility.Visible : Visibility.Collapsed;
            BackButtonPlaceholder.Visibility = BackButton.Visibility;
        }

        private void MainLayout_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || e.AddedItems[0] == null)
            {
                return;
            }
            var inboxThread = (DirectThreadWrapper) e.AddedItems[0];
            if (!string.IsNullOrEmpty(inboxThread.ThreadId)) 
                ToastNotificationManager.History.RemoveGroup(inboxThread.ThreadId);

            Debouncer.DelayExecute("OnThreadChanged", e.RemovedItems[0] == null ? 600 : 100, async cancelled =>
            {
                if (cancelled) return;
                var details = (TextBox) MainLayout.FindDescendant("MessageTextBox");
                details?.Focus(FocusState.Programmatic); // Focus to chat box after selecting a thread
                await inboxThread.MarkLatestItemSeen().ConfigureAwait(false);
            });
        }

        private void SearchBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            if (string.IsNullOrEmpty(sender.Text) || sender.Text.Length > 50)
            {
                return;
            }

            ViewModel.Search(sender.Text,
                updatedList => SearchBox.ItemsSource = updatedList);
        }

        private void SearchBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var selectedItem = (DirectThreadWrapper) args.SelectedItem;
            sender.Text = selectedItem.Title;
        }

        private void SearchBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                ViewModel.MakeProperInboxThread((DirectThreadWrapper) args.ChosenSuggestion);
            }
            else if (!string.IsNullOrEmpty(sender.Text))
            {
                ViewModel.Search(sender.Text, updatedList =>
                {
                    if (updatedList.Count == 0) return;
                    ViewModel.MakeProperInboxThread(updatedList[0]);
                });
            }
            sender.Text = string.Empty;
            sender.ItemsSource = null;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutDialog();
            _ = about.ShowAsync();
        }

        private void ThemeItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (MenuFlyoutItem) sender;
            switch (item.Text)
            {
                case "System":
                    _localSettings.Values["Theme"] = "System";
                    break;

                case "Dark":
                    _localSettings.Values["Theme"] = "Dark";
                    break;
                
                case "Light":
                    _localSettings.Values["Theme"] = "Light";
                    break;
            }

            var dialog = new ContentDialog
            {
                Title = "Saved",
                Content = "Please relaunch the app to see the result.",
                CloseButtonText = "Done",
                DefaultButton = ContentDialogButton.Close
            };

            _ = dialog.ShowAsync();
        }

        private async void Profile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ViewModel?.LoggedInUser?.Username)) return;
            var username = ViewModel.LoggedInUser.Username;
            var uri = new Uri("https://www.instagram.com/" + username);
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        #region NewMessage

        private void NewMessageSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            if (string.IsNullOrEmpty(sender.Text) || sender.Text.Length > 50)
            {
                return;
            }

            ViewModel.SearchWithoutThreads(sender.Text,
                updatedList => NewMessageSuggestBox.ItemsSource = updatedList);
        }

        private void NewMessageSuggestBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var selectedItem = (BaseUser) args.SelectedItem;
            sender.Text = selectedItem.Username;
        }

        private void NewMessageSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                var selectedRecipient = (BaseUser) args.ChosenSuggestion;
                if (NewMessageCandidates.All(x => selectedRecipient.Username != x.Username))
                    NewMessageCandidates.Add(selectedRecipient);
            }
            else if (!string.IsNullOrEmpty(sender.Text))
            {
                ViewModel.SearchWithoutThreads(sender.Text, updatedList =>
                {
                    if (updatedList.Count == 0) return;
                    NewMessageCandidates.Add(updatedList[0]);
                });
            }
            sender.Text = string.Empty;
            sender.ItemsSource = null;
        }

        private void NewMessageClearAll_OnClick(object sender, RoutedEventArgs e)
        {
            NewMessageCandidates.Clear();
        }

        private async void ChatButton_OnClick(object sender, RoutedEventArgs e)
        {
            NewThreadFlyout.Hide();
            if (NewMessageCandidates.Count == 0 || NewMessageCandidates.Count > 32) return;
            var userIds = NewMessageCandidates.Select(x => x.Pk);
            await ViewModel.CreateAndOpenThread(userIds);
            NewMessageCandidates.Clear();
        }

        private void ClearSingleCandidateButton_OnClick(object sender, RoutedEventArgs e)
        {
            var target = (BaseUser) (sender as FrameworkElement)?.DataContext;
            if (target == null) return;
            NewMessageCandidates.Remove(target);
        }

        private void Candidate_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            (sender as FrameworkElement).FindDescendant("ClearSingleCandidateButton").Visibility = Visibility.Visible;
        }

        private void Candidate_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            (sender as FrameworkElement).FindDescendant("ClearSingleCandidateButton").Visibility = Visibility.Collapsed;
        }

        private void ClearSingleCandidateSwipe_OnInvoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            var target = (BaseUser) args.SwipeControl.DataContext;
            if (target == null) return;
            NewMessageCandidates.Remove(target);
        }

        private void NewMessageSuggestBox_OnProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
        {
            if (args.Key == VirtualKey.Escape && args.Modifiers == VirtualKeyModifiers.None)
            {
                args.Handled = true;
                NewThreadFlyout.Hide();
            }
                
        }

        #endregion

        private void TogglePendingInbox_OnClick(object sender, RoutedEventArgs e)
        {
            Inbox = Inbox == ViewModel.Inbox ? ViewModel.PendingInbox : ViewModel.Inbox;
        }

        private async void ReelsFeed_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var reelsFeed = (ListView) sender;
            int selected;
            lock (sender)
            {
                if (reelsFeed.SelectedIndex == -1) return;
                selected = reelsFeed.SelectedIndex;
                reelsFeed.SelectedIndex = -1;
            }
            var reelsWrapper = await ViewModel.ReelsFeed.PrepareFlatReelsContainer(selected);
            this.Frame.Navigate(typeof(ReelPage), reelsWrapper);
        }

        private async void StoriesSectionTitle_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            await ViewModel.ReelsFeed.UpdateReelsFeed(ReelsTrayFetchReason.PullToRefresh);
        }

        private void TestButton_OnClick(object sender, RoutedEventArgs e)
        {
            //await ContactsService.DeleteAllAppContacts();
        }

        private async void MasterMenuButton_OnImageExFailed(object sender, ImageExFailedEventArgs e)
        {
            await ViewModel.UpdateLoggedInUser();
        }
    }
}
