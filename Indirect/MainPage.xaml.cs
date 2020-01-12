using System;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI.Controls;
using CoreWindowActivationState = Windows.UI.Core.CoreWindowActivationState;
using InstaDirectInboxThreadWrapper = Indirect.Wrapper.InstaDirectInboxThreadWrapper;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Indirect
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ApiContainer _viewModel;

        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.SetTitleBar(TitleBarElement);
            MainLayout.ViewStateChanged += OnViewStateChange;
            Window.Current.Activated += OnWindowFocusChange;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _viewModel = (ApiContainer)e.Parameter;
            if (_viewModel == null) throw new NullReferenceException("No _viewModel created");
            MainLayout.DataContext = _viewModel;
            _viewModel.OnLoggedIn();
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var confirmDialog = new ContentDialog()
            {
                Title = "Log out?",
                CloseButtonText = "Close",
                PrimaryButtonText = "Logout", 
                DefaultButton = ContentDialogButton.Close
            };
            var confirmation = await confirmDialog.ShowAsync();
            if (confirmation != ContentDialogResult.Primary) return;
            await _viewModel.Logout();
            Frame.Navigate(typeof(Login), _viewModel);
        }
        
        private void DetailsBackButton_OnClick(object sender, RoutedEventArgs e) => _viewModel.SetSelectedThreadNull();

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

        private void OnViewStateChange(object sender, MasterDetailsViewState state)
        {
            BackButton.Visibility = state == MasterDetailsViewState.Details ? Visibility.Visible : Visibility.Collapsed;
            BackButtonPlaceholder.Visibility = BackButton.Visibility;
        }

        private void RefreshThread_OnClick(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdateInboxAndSelectedThread();
        }

        private void MainLayout_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || e.AddedItems[0] == null)
            {
                return;
            }
            var inboxThread = (InstaDirectInboxThreadWrapper) e.AddedItems[0];
            DataContext = inboxThread.ObservableItems;
            _viewModel.MarkLatestItemSeen(inboxThread);
        }

        private void NewMessageButton_OnClick(object sender, RoutedEventArgs e)
        {
            SearchBox.Focus(FocusState.Programmatic);
        }

        private void SearchBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            if (string.IsNullOrEmpty(sender.Text) || sender.Text.Length > 50)
            {
                return;
            }

            _viewModel.Search(sender.Text,
                updatedList => SearchBox.ItemsSource = updatedList);
        }

        private void SearchBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var selectedItem = (InstaDirectInboxThreadWrapper) args.SelectedItem;
            sender.Text = selectedItem.Title;
        }

        private void SearchBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                _viewModel.MakeProperInboxThread((InstaDirectInboxThreadWrapper) args.ChosenSuggestion);
            }
            else if (!string.IsNullOrEmpty(sender.Text))
            {
                _viewModel.Search(sender.Text, updatedList =>
                {
                    if (updatedList.Count == 0) return;
                    _viewModel.MakeProperInboxThread(updatedList[0]);
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

        private void Magic_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            _viewModel.StartSyncClient();
#endif
        }
    }
}
