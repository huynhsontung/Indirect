using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Indirect.Controls;
using Indirect.Services;
using InstagramAPI;
using InstagramAPI.Classes;
using InstagramAPI.Utils;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        private MainViewModel ViewModel => ((App)Application.Current).ViewModel;
        private bool _loading;
        public LoginPage()
        {
            this.InitializeComponent();
            Window.Current.SetTitleBar(TitleBarElement);
            Window.Current.Activated += OnWindowFocusChange;
            Window.Current.SizeChanged += OnWindowSizeChanged;
            LoginWebview.Height = Window.Current.Bounds.Height * 0.8;
            WebviewPopup.VerticalOffset = -(LoginWebview.Height / 2);
            LoginWebview.NavigationStarting += LoginWebviewOnNavigationStarting;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e?.NavigationMode != NavigationMode.Back)
            {
                await ContactsService.TryFetchContactStores();  // for store certification
            }
        }

        private void DisableButtons()
        {
            _loading = true;
            LoginButton.IsEnabled = false;
            FbLoginButton.IsEnabled = false;
        }

        private void EnableButtons()
        {
            _loading = false;
            LoginButton.IsEnabled = true;
            FbLoginButton.IsEnabled = true;
        }

        private async void LoginWebviewOnNavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
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
                
                if (_loading) return;
                DisableButtons();
                try
                {
                    var query = args.Uri.Fragment.Substring(1); // turn fragment into query (remove the '#')
                    var urlParams = new WwwFormUrlDecoder(query);
                    var fbToken = urlParams.GetFirstValueByName("access_token");
                    if (string.IsNullOrEmpty(fbToken))
                    {
                        await ShowLoginErrorDialog("Failed to acquire access token");
                        return;
                    }

                    var result = await ViewModel.LoginWithFacebook(fbToken).ConfigureAwait(true);
                    if (!result.IsSucceeded)
                    {
                        if (result.Value == LoginResult.TwoFactorRequired)
                        {
                            await TwoFactorAuthAsync();
                        }
                        else
                        {
                            await ShowLoginErrorDialog(result.Message);
                        }
                        return;
                    }

                    await TryNavigateToMainPage();
                }
                catch (Exception e)
                {
                    await ShowLoginErrorDialog(
                        "Unexpected error occured while logging in with Facebook. Please try again later or log in with Instagram account instead.");
                    DebugLogger.LogException(e);
                }
                finally
                {
                    EnableButtons();
                }
            }
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
            if (Instagram.Instance.TwoFactorInfo != null)
            {
                await TwoFactorAuthAsync();
                return;
            }

            if (_loading) return;
            DisableButtons();
            try
            {
                var username = UsernameBox.Text;
                var password = PasswordBox.Password;
                if (username.Length <= 0 || password.Length <= 0)
                {
                    return;
                }

                var result = await ViewModel.Login(username, password);
                if (result.Status != ResultStatus.Succeeded || result.Value != LoginResult.Success)
                {
                    switch (result.Value)
                    {
                        case LoginResult.ChallengeRequired:
                            if (Instagram.Instance.ChallengeInfo != null && !WebviewPopup.IsOpen)
                            {
                                LoginWebview.Navigate(Instagram.Instance.ChallengeInfo.Url);
                                WebviewPopup.IsOpen = true;
                            }

                            break;
                        case LoginResult.TwoFactorRequired:
                            await TwoFactorAuthAsync();
                            break;
                        default:
                            await ShowLoginErrorDialog(result.Message);
                            break;
                    }

                    return;
                }

                await TryNavigateToMainPage();
            }
            finally
            {
                EnableButtons();
            }
        }

        private async Task TwoFactorAuthAsync()
        {
            var tfaDialog = new TwoFactorAuthDialog();
            var dialogResult = await tfaDialog.ShowAsync();
            if (dialogResult == ContentDialogResult.Primary)
                await TryNavigateToMainPage();
        }

        private async Task TryNavigateToMainPage()
        {
            var instance = Instagram.Instance;
            if ((string.IsNullOrEmpty(instance.Session.Username) || string.IsNullOrEmpty(instance.Session.Password)) &&
                string.IsNullOrEmpty(instance.Session.FacebookAccessToken) || !instance.IsUserAuthenticated)
            {
                await ShowLoginErrorDialog("Something went wrong. Please try again later.");
                DebugLogger.LogException(new Exception("Try to navigate to MainPage but user validation failed"));
            }
            else
            {
                Frame.Navigate(typeof(MainPage));
            }
        }

        private static async Task ShowLoginErrorDialog(string message)
        {
            var failDialog = new ContentDialog
            {
                Title = "Login failed",
                Content = $"Reason: {message}",
                DefaultButton = ContentDialogButton.Close,
                CloseButtonText = "Close"
            };
            try
            {
                await failDialog.ShowAsync();
            }
            catch (Exception)
            {
                // pass
            }
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

        private async void FbLoginButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Instagram.Instance.TwoFactorInfo != null)
            {
                await TwoFactorAuthAsync();
                return;
            }
            WebviewPopup.IsOpen = true;
            // https://developers.facebook.com/docs/facebook-login/manually-build-a-login-flow/#login
            LoginWebview.Navigate(new Uri("https://m.facebook.com/v6.0/dialog/oauth?client_id=124024574287414&scope=email&response_type=token&redirect_uri=https%3A%2F%2Fwww.instagram.com%2Faccounts%2Fsignup%2F"));
        }
    }
}
