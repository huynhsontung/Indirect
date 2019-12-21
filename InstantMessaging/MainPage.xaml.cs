using InstantMessaging.Wrapper;
using System;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using InstantMessaging.Converters;
using Microsoft.Toolkit.Uwp.UI.Controls;
using CoreWindowActivationState = Windows.UI.Core.CoreWindowActivationState;

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
            FromMeBoolToBrushConverter.CurrentPage = this;
            MainLayout.ViewStateChanged += OnViewStateChange;
            Window.Current.Activated += OnWindowFocusChange;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _viewModel = (ApiContainer)e.Parameter;
            if (_viewModel == null) throw new NullReferenceException("No _viewModel created");
            _viewModel.PageReference = this;
            await _viewModel.GetInboxAsync();
            await _viewModel.UpdateLoggedInUser();
            await _viewModel.StartPushClient();
        }

        //private async void MessageContent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (e.AddedItems[0] == null)
        //        return;
        //    await _viewModel.OnThreadChange(e.AddedItems[0] as InstaDirectInboxThreadWrapper);
        //}

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

    }
}
