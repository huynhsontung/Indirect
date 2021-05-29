using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Indirect.Controls;
using Indirect.Utilities;
using InstagramAPI;
using InstagramAPI.Utils;
using InstagramAPI.Classes.Core;

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
        private int _challengeRepeatCount;

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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _challengeRepeatCount = 0;
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
            this.Log($"Navigating to: {args.Uri}");
            // Clearing challenge
            if (args.Uri.PathAndQuery == "/" || string.IsNullOrEmpty(args.Uri.PathAndQuery))
            {
                if (await Debouncer.Delay("ClearingChallenge", 200) && await TryClearingChallenge(sender))
                    await LoginDispatch();
                return;
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
                        "Unexpected error occurred while logging in with Facebook. Please try again later or log in with Instagram account instead.");
                    DebugLogger.LogException(e, properties: new Dictionary<string, string>
                    {
                        {"Uri", args.Uri.ToString().StripSensitive()}
                    });
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
            if (ViewModel.InstaApi.TwoFactorInfo != null)
            {
                await TwoFactorAuthAsync();
                return;
            }

            await LoginDispatch();
        }

        private async Task LoginDispatch()
        {
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
                            if (ViewModel.InstaApi.ChallengeInfo != null && !WebviewPopup.IsOpen)
                            {
                                LoginWebview.Navigate(ViewModel.InstaApi.ChallengeInfo.Url);
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

        private async Task<bool> TryClearingChallenge(WebView challengeWebView)
        {
            const int retryCount = 4;
            _challengeRepeatCount++;
            if (_challengeRepeatCount < retryCount)
            {
                WebviewPopup.IsOpen = false;
                challengeWebView.Stop();
                return true;
            }

            if (_challengeRepeatCount == retryCount)
            {
                WebviewPopup.IsOpen = false;
                challengeWebView.Stop();
                challengeWebView.Navigate(new Uri("https://www.instagram.com/"));
                var manualLoginDialog = new ContentDialog
                {
                    Title = "Stuck at this screen?",
                    Content = "Instagram might misbehave and refuse to send you the security code.\n" +
                              "Please log in manually using their web interface, do all the verification required, then try logging in again with Indirect.",
                    CloseButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Close
                };
                try
                {
                    await manualLoginDialog.ShowAsync();
                }
                catch (Exception)
                {
                    // pass
                }

                WebviewPopup.IsOpen = true;
            }

            return false;
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
            if ((string.IsNullOrEmpty(ViewModel.InstaApi.Session.Username) ||
                 string.IsNullOrEmpty(ViewModel.InstaApi.Session.Password)) &&
                string.IsNullOrEmpty(ViewModel.InstaApi.Session.FacebookAccessToken) ||
                !ViewModel.InstaApi.IsUserAuthenticated)
            {
                await ShowLoginErrorDialog("Something went wrong. Please try again later.");
                DebugLogger.LogException(new Exception("Try to navigate to MainPage but user validation failed"));
            }
            else
            {
                Frame.Navigate(typeof(MainPage));
                await ViewModel.InstaApi.SaveToAppSettings().ConfigureAwait(false);
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
            if (ViewModel.InstaApi.TwoFactorInfo != null)
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
