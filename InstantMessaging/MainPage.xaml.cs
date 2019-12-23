using InstantMessaging.Wrapper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using InstantMessaging.Converters;
using Microsoft.Toolkit.Uwp.UI.Controls;
using CoreWindowActivationState = Windows.UI.Core.CoreWindowActivationState;
using InstaDirectInboxThreadWrapper = InstantMessaging.Wrapper.InstaDirectInboxThreadWrapper;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace InstantMessaging
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
            InstaUserShortWrapper.PageReference = this;
            MainLayout.ViewStateChanged += OnViewStateChange;
            Window.Current.Activated += OnWindowFocusChange;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _viewModel = (ApiContainer)e.Parameter;
            if (_viewModel == null) throw new NullReferenceException("No _viewModel created");
            _viewModel.PageReference = this;
            await _viewModel.OnLoggedIn();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {

            var button = sender as Control;
            var messageBox = (button.Parent as Grid).Children[0] as TextBox;
            var message = messageBox.Text;
            await _viewModel.SendMessage(message);
            messageBox.Text = "";
        }

        private void MessageTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
            {
                SendButton_Click(sender, e);
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
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

        private void ItemContainer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var panel = (Panel) sender;
            var timestampTextBlock = panel.Children.Last();
            timestampTextBlock.Visibility = timestampTextBlock.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void RefreshThread_OnClick(object sender, RoutedEventArgs e)
        {
            await _viewModel.UpdateInboxAndSelectedThread();
        }

        private void MainLayout_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || e.AddedItems[0] == null)
            {
                DataContext = _viewModel.InboxThreads;
                return;
            }
            var inboxThread = (InstaDirectInboxThreadWrapper) e.AddedItems[0];
            DataContext = inboxThread.ObservableItems;
        }

        private void NewMessageButton_OnClick(object sender, RoutedEventArgs e)
        {
            SearchBox.Focus(FocusState.Programmatic);
        }

        private async void SearchBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            if (string.IsNullOrEmpty(sender.Text))
            {
                sender.ItemsSource = null;
            }
            else
            {
                // This will return a list of placeholder thread
                sender.ItemsSource = await _viewModel.Search(sender.Text);
            }
        }

        private void SearchBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var selectedItem = (InstaDirectInboxThreadWrapper) args.SelectedItem;
            sender.Text = selectedItem.Title;
        }

        private async void SearchBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var itemSource = (List<InstaDirectInboxThreadWrapper>) sender.ItemsSource;
            if (args.ChosenSuggestion != null)
            {
                await _viewModel.MakeProperInboxThread((InstaDirectInboxThreadWrapper) args.ChosenSuggestion);
            }
            else if (itemSource != null && itemSource.Count > 0)
            {
                await _viewModel.MakeProperInboxThread(itemSource[0]);
            }
            sender.Text = string.Empty;
        }
    }
}
