using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using InstagramAPI;
using InstagramAPI.Classes.Core;
using InstagramAPI.Utils;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect.Controls
{
    public sealed partial class TwoFactorAuthDialog : ContentDialog
    {
        private readonly UserSessionData _session;

        public TwoFactorAuthDialog(UserSessionData session)
        {
            this.InitializeComponent();
            _session = session;
        }

        private async void ConfirmSecurityCode(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (CodeBox.Text.Length < 6)
            {
                args.Cancel = true;
                ErrorMessage.Text = "Please enter a valid security code";
                return;
            }
            var deferral = args.GetDeferral();
            this.IsPrimaryButtonEnabled = false;
            var result = await Instagram.LoginWithTwoFactorAsync(CodeBox.Text, _session);
            if (!result.IsSucceeded)
            {
                args.Cancel = true;
                ErrorMessage.Text = result.Message;

                if (result.Exception != null)
                {
                    DebugLogger.LogException(result.Exception);
                }
            }

            this.IsPrimaryButtonEnabled = true;
            deferral.Complete();
        }

        private async void TwoFactorAuthDialog_OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            ErrorMessage.Text = string.Empty;
            await Task.Delay(200);
            CodeBox.Focus(FocusState.Programmatic);
        }

        private void CodeBox_OnTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            if (string.IsNullOrEmpty(sender.Text) || !args.IsContentChanging) return;
            sender.Text = new string(sender.Text.Where(c => '0' <= c && c <= '9').ToArray());
            sender.SelectionStart = sender.Text.Length;
        }
    }
}
