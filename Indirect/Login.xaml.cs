using System;
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
            ChallengeWebview.Height = Window.Current.Bounds.Height * 0.8;
            ChallengePopup.VerticalOffset = -(ChallengeWebview.Height / 2);
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            ChallengeWebview.Height = e.Size.Height * 0.8;
            ChallengePopup.VerticalOffset = -(ChallengeWebview.Height / 2);
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
            if (username.Length <= 0 || password.Length <= 0) return;
            var result = await _viewModel.Login(username, password);
            if (result.Status != ResultStatus.Succeeded || result.Value != LoginResult.Success)
            {
                if (result.Value == LoginResult.ChallengeRequired)
                {
                    if (Instagram.Instance.ChallengeInfo != null && !ChallengePopup.IsOpen)
                    {
                        ChallengeWebview.Navigate(Instagram.Instance.ChallengeInfo.Url);
                        ChallengeWebview.NavigationStarting += (view, args) =>
                        {
                            if (args.Uri.PathAndQuery == "/" || string.IsNullOrEmpty(args.Uri.PathAndQuery))
                            {
                                ChallengePopup.IsOpen = false;
                            }
                        };
                        ChallengePopup.IsOpen = true;
                    }

                }
                else
                {
                    var failDialog = new ContentDialog
                    {
                        Title = "Login failed",
                        Content = $"Reason: {result.Message}",
                        DefaultButton = ContentDialogButton.Close,
                        CloseButtonText = "Close"
                    };
                    var dialogResult = await failDialog.ShowAsync();
                }

                LoginButton.IsEnabled = true;
                return;
            }
            Frame.Navigate(typeof(MainPage), _viewModel);
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
            ChallengePopup.IsOpen = false;
        }
    }
}
