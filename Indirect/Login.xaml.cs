using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using InstaSharper.Enums;

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
        }

        private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
                Button_Click(sender, e);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;
            var username = UsernameBox.Text;
            var password = PasswordBox.Password;
            if (username.Length <= 0 || password.Length <= 0) return;
            var result = await _viewModel.Login(username, password);
            if (!result.Succeeded || result.Value != InstaLoginResult.Success)
            {
                ContentDialog failDialog;
                if (result.Value == InstaLoginResult.ChallengeRequired)
                {
                    failDialog = new ContentDialog
                    {
                        Title = "Login failed",
                        Content = new TextBlock
                        {
                            Text = "Challenge required. Please login using the official Instagram website first then try again.",
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 250
                        },
                        DefaultButton = ContentDialogButton.Close,
                        CloseButtonText = "Close"
                    };
                }
                else
                {
                    failDialog = new ContentDialog
                    {
                        Title = "Login failed",
                        Content = $"Reason: {result.Info.Message}",
                        DefaultButton = ContentDialogButton.Close,
                        CloseButtonText = "Close"
                    };
                }

                var dialogResult = await failDialog.ShowAsync();
                LoginButton.IsEnabled = true;
                return;
            }
            Frame.Navigate(typeof(MainPage), _viewModel);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _viewModel = e.Parameter as ApiContainer;
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
    }
}
