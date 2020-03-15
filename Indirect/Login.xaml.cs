using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using InstagramAPI;
using InstagramAPI.Classes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Login : Page
    {
        private ApiContainer _viewModel;
        public Login()
        {
            this.InitializeComponent();
            Window.Current.SetTitleBar(TitleBarElement);
            Window.Current.Activated += OnWindowFocusChange;
            Window.Current.SizeChanged += OnWindowSizeChanged;
            LoginWebview.Height = Window.Current.Bounds.Height * 0.8;
            WebviewPopup.VerticalOffset = -(LoginWebview.Height / 2);
            LoginWebview.NavigationStarting += async (view, args) =>
            {
                // Clearing challenge
                if (args.Uri.PathAndQuery == "/" || string.IsNullOrEmpty(args.Uri.PathAndQuery))
                {
                    WebviewPopup.IsOpen = false;
                }

                // Facebook OAuth Login
                if (args.Uri.PathAndQuery.Contains("accounts/signup", StringComparison.OrdinalIgnoreCase))
                {
                    // Uri looks like this: https://www.instagram.com/accounts/signup/?#access_token=...
                    WebviewPopup.IsOpen = false;
                    FbLoginButton.IsEnabled = false;
                    var query = args.Uri.Fragment.Substring(1); // turn fragment into query (remove the '#')
                    var urlParams = new WwwFormUrlDecoder(query);
                    var fbToken = urlParams.GetFirstValueByName("access_token");
                    if (string.IsNullOrEmpty(fbToken))
                    {
                        await ShowLoginErrorDialog("Failed to acquire access token");
                        FbLoginButton.IsEnabled = true;
                        return;
                    }
                    var result = await _viewModel.LoginWithFacebook(fbToken).ConfigureAwait(true);
                    if (!result.IsSucceeded)
                    {
                        await ShowLoginErrorDialog(result.Message);
                        FbLoginButton.IsEnabled = true;
                        return;
                    }
                    Frame.Navigate(typeof(MainPage));
                }
            };
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            LoginWebview.Height = e.Size.Height * 0.8;
            WebviewPopup.VerticalOffset = -(LoginWebview.Height / 2);
        }

        private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
                LoginButton_Click(sender, e);
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;
            var username = UsernameBox.Text;
            var password = PasswordBox.Password;
            if (username.Length <= 0 || password.Length <= 0)
            {
                LoginButton.IsEnabled = true;
                return;
            }
            var result = await _viewModel.Login(username, password);
            if (result.Status != ResultStatus.Succeeded || result.Value != LoginResult.Success)
            {
                if (result.Value == LoginResult.ChallengeRequired)
                {
                    if (Instagram.Instance.ChallengeInfo != null && !WebviewPopup.IsOpen)
                    {
                        LoginWebview.Navigate(Instagram.Instance.ChallengeInfo.Url);
                        WebviewPopup.IsOpen = true;
                    }

                }
                else
                {
                    await ShowLoginErrorDialog(result.Message);
                }

                LoginButton.IsEnabled = true;
                return;
            }
            Frame.Navigate(typeof(MainPage));
        }

        private async Task ShowLoginErrorDialog(string message)
        {
            var failDialog = new ContentDialog
            {
                Title = "Login failed",
                Content = $"Reason: {message}",
                DefaultButton = ContentDialogButton.Close,
                CloseButtonText = "Close"
            };
            var dialogResult = await failDialog.ShowAsync();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _viewModel = ApiContainer.Instance;
            this.Bindings.Update();
        }

        private void OnWindowFocusChange(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                AppTitleTextBlock.Opacity = 0.5;
            }
            else
            {

                AppTitleTextBlock.Opacity = 1;
            }
        }

        private void PopupCloseButton_Click(object sender, RoutedEventArgs e)
        {
            WebviewPopup.IsOpen = false;
        }

        private void FbLoginButton_OnClick(object sender, RoutedEventArgs e)
        {
            WebviewPopup.IsOpen = true;
            // https://developers.facebook.com/docs/facebook-login/manually-build-a-login-flow/#login
            LoginWebview.Navigate(new Uri("https://m.facebook.com/v6.0/dialog/oauth?client_id=124024574287414&scope=email&response_type=token&redirect_uri=https%3A%2F%2Fwww.instagram.com%2Faccounts%2Fsignup%2F"));
        }
    }
}
