using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
            var currentPackage = Package.Current;
            var version = currentPackage.Id.Version;
            VersionText.Text = "v" + version.Major + '.' + version.Minor + '.' + version.Build;
            Identity.Text = ((App)Application.Current).ViewModel.Device.HardwareModel;
        }
    }
}
