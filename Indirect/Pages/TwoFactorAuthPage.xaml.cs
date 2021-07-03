using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using InstagramAPI;
using InstagramAPI.Classes.Core;
using InstagramAPI.Utils;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TwoFactorAuthPage : Page
    {
        private readonly UserSessionData _session;

        public TwoFactorAuthPage(UserSessionData session)
        {
            this.InitializeComponent();
            _session = session;
        }

        public static async Task<ContentDialogResult> ShowAsync(UserSessionData session)
        {
            var page = new TwoFactorAuthPage(session);
            var dialog = new ContentDialog
            {
                Title = "Two-factor authentication required",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                PrimaryButtonText = "Confirm",
                Content = page
            };
            dialog.Opened += page.OnDialogOpened;
            dialog.PrimaryButtonClick += page.OnDialogPrimaryButtonClick;
            return await dialog.ShowAsync();
        }

        private async void OnDialogPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (CodeBox.Text.Length < 6)
            {
                args.Cancel = true;
                ErrorMessage.Text = "Please enter a valid security code";
                return;
            }

            var deferral = args.GetDeferral();
            try
            {
                sender.IsPrimaryButtonEnabled = false;
                var result = await Instagram.LoginWithTwoFactorAsync(CodeBox.Text, _session);
                if (!result.IsSucceeded)
                {
                    if (result.Value != LoginResult.ChallengeRequired)
                    {
                        args.Cancel = true;
                        ErrorMessage.Text = result.Message;
                    }

                    if (result.Exception != null)
                    {
                        DebugLogger.LogException(result.Exception);
                    }
                }
            }
            finally
            {
                sender.IsPrimaryButtonEnabled = true;
                deferral.Complete();
            }
        }

        private void OnDialogOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            ErrorMessage.Text = string.Empty;
        }

        private void CodeBox_OnTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            if (string.IsNullOrEmpty(sender.Text) || !args.IsContentChanging) return;
            sender.Text = new string(sender.Text.Where(c => '0' <= c && c <= '9').ToArray());
            sender.SelectionStart = sender.Text.Length;
        }
    }
}
