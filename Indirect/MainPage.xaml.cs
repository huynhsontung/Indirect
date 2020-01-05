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
        private StorageFolder _temporaryFolder = ApplicationData.Current.TemporaryFolder;
        private ApiContainer _viewModel;
        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.SetTitleBar(TitleBarElement);
            MainLayout.ViewStateChanged += OnViewStateChange;
            Window.Current.Activated += OnWindowFocusChange;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _viewModel = (ApiContainer)e.Parameter;
            if (_viewModel == null) throw new NullReferenceException("No _viewModel created");
            _viewModel.PageReference = this;
            MainLayout.DataContext = _viewModel;
            await _viewModel.OnLoggedIn();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Control;
            var messageBox = (TextBox) (button.Parent as Grid).Children[0];
            var message = messageBox.Text;
            messageBox.Text = "";
            _viewModel.SendMessage(message);
        }

        private void MessageTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
            {
                SendButton_Click(sender, e);
            }
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await _viewModel.Logout();
            if (result.Value)
            {
                Frame.Navigate(typeof(Login), _viewModel);
            }
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
            _ = _viewModel.MarkLatestItemSeen(inboxThread);
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
            // Run logic on separate thread and update with UpdateSuggestionListCallback()
            _ = _viewModel.Search(sender.Text);
            
        }

        public void UpdateSuggestionListCallback(object updatedList)
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { SearchBox.ItemsSource = updatedList; });
        }

        private void SearchBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var selectedItem = (InstaDirectInboxThreadWrapper) args.SelectedItem;
            sender.Text = selectedItem.Title;
        }

        private async void SearchBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                await _viewModel.MakeProperInboxThread((InstaDirectInboxThreadWrapper) args.ChosenSuggestion);
            }
            else if (!string.IsNullOrEmpty(sender.Text))
            {
                var items = await _viewModel.Search(sender.Text);
                if (items.Count == 0) return;
                await _viewModel.MakeProperInboxThread(items[0]);
            }
            sender.Text = string.Empty;
            sender.ItemsSource = null;
        }
    }
}
