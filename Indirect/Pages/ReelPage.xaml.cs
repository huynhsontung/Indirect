using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Indirect.Entities;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ReelPage : Page
    {
        public static readonly DependencyProperty ReelsProperty = DependencyProperty.Register(
            nameof(Reels),
            typeof(FlatReelsContainer),
            typeof(ReelPage),
            new PropertyMetadata(null));

        public FlatReelsContainer Reels
        {
            get => (FlatReelsContainer) GetValue(ReelsProperty);
            set => SetValue(ReelsProperty, value);
        }

        public ReelPage()
        {
            this.InitializeComponent();
            Window.Current.SetTitleBar(TitleBarElement);
            Window.Current.Activated += OnWindowFocusChange;

            SystemNavigationManager.GetForCurrentView().BackRequested += SystemNavigationManager_BackRequested;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Reels = e?.Parameter as FlatReelsContainer;
            if (Reels?.SecondaryView ?? false)
            {
                ApplicationView.GetForCurrentView().Consolidated += ViewConsolidated;
                ApplicationView.GetForCurrentView().Title = "Story";
                BackButton.Visibility = Visibility.Collapsed;
                BackButtonPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void ViewConsolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            Window.Current.Content = null;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SystemNavigationManager.GetForCurrentView().BackRequested -= SystemNavigationManager_BackRequested;
        }

        private void OnWindowFocusChange(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                BackButton.IsEnabled = false;
                AppTitleTextBlock.Opacity = 0.5;
            }
            else
            {
                BackButton.IsEnabled = true;
                AppTitleTextBlock.Opacity = 1;
            }
        }

        private void SystemNavigationManager_BackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = true;
            BackButton_OnClick(this, null);
        }

        private void BackButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }

        private void OnGoBackInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            BackButton_OnClick(this, null);
            args.Handled = true;
        }

        private void ReelPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Reels = null;
            Window.Current.Activated -= OnWindowFocusChange;
        }
    }
}
