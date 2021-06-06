using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using InstagramAPI.Utils;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect.Controls
{
    public sealed partial class TwoFactorAuthDialog : ContentDialog
    {
        public TwoFactorAuthDialog()
        {
            this.InitializeComponent();
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
            var result = await ((App)App.Current).ViewModel.InstaApi.LoginWithTwoFactorAsync(CodeBox.Text);
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
