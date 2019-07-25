using InstantMessaging.Wrapper;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using InstantMessaging.Converters;
using Microsoft.Toolkit.Uwp.UI.Controls;

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
            FromMeBoolToBrushConverter.CurrentPage = this;
            InstaUserShortWrapper.PageReference = this;
            MainLayout.ViewStateChanged += OnViewStateChange;
            
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _viewModel = (ApiContainer)e.Parameter;
            if (_viewModel == null) throw new NullReferenceException("No _viewModel created");
            await _viewModel.GetInboxAsync();
//            await _viewModel.StartPushClient();
        }

        private async void MessageContent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] == null)
                return;
            await _viewModel.OnThreadChange(e.AddedItems[0] as InstaDirectInboxThreadWrapper);           
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

        private void OnViewStateChange(object sender, MasterDetailsViewState state)
        {
            BackButton.Visibility = state == MasterDetailsViewState.Details ? Visibility.Visible : Visibility.Collapsed;
            BackButtonPlaceholder.Visibility = BackButton.Visibility;
        }
    }
}
