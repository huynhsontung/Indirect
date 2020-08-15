using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect.Controls
{
    public sealed partial class AboutDialog : ContentDialog
    {
        public AboutDialog()
        {
            this.InitializeComponent();
            var currentPackage = Package.Current;
            var version = currentPackage.Id.Version;
            VersionText.Text = "v" + version.Major + '.' + version.Minor + '.' + version.Build;
            Identity.Text = ((App)Application.Current).ViewModel.Device.DeviceName;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
