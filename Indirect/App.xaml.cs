using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Indirect.Pages;
using Indirect.Utilities;
using InstagramAPI;
using InstagramAPI.Utils;
using Microsoft.Toolkit.Uwp.UI;

namespace Indirect
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        internal MainViewModel ViewModel { get; } = new MainViewModel();

        private List<int> SecondaryViewIds { get; } = new List<int>();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Instagram.StartAppCenter();
            this.InitializeComponent();
            SetTheme();
            this.Suspending += OnSuspending;
            this.Resuming += OnResuming;
            this.EnteredBackground += OnEnteredBackground;
        }

        public async Task CloseAllSecondaryViews()
        {
            foreach (var secondaryViewId in SecondaryViewIds.ToArray())
            {
                await ApplicationViewSwitcher.SwitchAsync(ApplicationView.GetForCurrentView().Id, secondaryViewId,
                    ApplicationViewSwitchingOptions.ConsolidateViews);
            }
        }

        public async Task CreateAndShowNewView(Type targetPage, object parameter = null, CoreApplicationView view = null)
        {
            var newView = view ?? CoreApplication.CreateNewView();
            await newView.Dispatcher.QuickRunAsync(async () =>
            {
                var frame = new Frame();
                frame.Navigate(targetPage, parameter);
                Window.Current.Content = frame;
                // You have to activate the window in order to show it later.
                Window.Current.Activate();

                var newAppView = ApplicationView.GetForCurrentView();
                newAppView.SetPreferredMinSize(new Size(380, 300));
                var titleBar = newAppView.TitleBar;
                titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;
                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

                var newViewId = newAppView.Id;
                SecondaryViewIds.Add(newViewId);
                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
                newAppView.TryResizeView(new Size(380, 640));
                newAppView.Consolidated += SecondaryView_OnConsolidated;
            });
        }

        private void SetTheme()
        {
            var requestedTheme = _localSettings.Values["Theme"] as string;
            if (requestedTheme == null) return;
            switch (requestedTheme)
            {
                case "Dark":
                    RequestedTheme = ApplicationTheme.Dark;
                    break;

                case "Light":
                    RequestedTheme = ApplicationTheme.Light;
                    break;
            }
        }

        private void TryEnablePrelaunch()
        {
            Windows.ApplicationModel.Core.CoreApplication.EnablePrelaunch(true);
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            OnLaunchedOrActivated(args);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            OnLaunchedOrActivated(e);
        }

        private async void OnLaunchedOrActivated(IActivatedEventArgs e)
        {
            bool canEnablePrelaunch =
                Windows.Foundation.Metadata.ApiInformation.IsMethodPresent(
                    "Windows.ApplicationModel.Core.CoreApplication", "EnablePrelaunch");
            await ViewModel.Initialize();

            if (e is ContactPanelActivatedEventArgs cpEventArgs)
            {
                var contactFrame = new Frame();
                Window.Current.Content = contactFrame;
                contactFrame.Navigate(typeof(ContactPanelPage), cpEventArgs);
            }
            else
            {
                var rootFrame = Window.Current.Content as Frame;

                // Do not repeat app initialization when the Window already has content,
                // just ensure that the window is active
                if (rootFrame == null)
                {
                    ConfigureMainView();

                    // Create a Frame to act as the navigation context and navigate to the first page
                    rootFrame = new Frame();

                    rootFrame.NavigationFailed += OnNavigationFailed;

                    if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                    {
                        // Handle different ExecutionStates
                    }

                    // Place the frame in the current Window
                    Window.Current.Content = rootFrame;
                }

                if (e is LaunchActivatedEventArgs launchActivatedArgs && launchActivatedArgs.PrelaunchActivated)
                {
                    return;
                }

                if (e is ToastNotificationActivatedEventArgs toastActivated)
                {
                    var launchArgs = HttpUtility.ParseQueryString(toastActivated.Argument);
                    var threadId = launchArgs["threadId"];
                    var viewerId = toastActivated.Argument.Contains("viewerId") ? launchArgs["viewerId"] : null;
                    var targetSession =
                        ViewModel.AvailableSessions.Select(x => x.Session)
                            .FirstOrDefault(x => x.LoggedInUser.Pk.ToString() == viewerId);
                    if (!string.IsNullOrEmpty(viewerId) && targetSession != null)
                    {
                        await ViewModel.SwitchAccountAsync(targetSession);
                    }

                    ViewModel.OpenThreadWhenReady(threadId);
                }

                if (canEnablePrelaunch)
                {
                    TryEnablePrelaunch();
                }

                SyncLock.Acquire();
                ViewModel.StartedFromMainView = true;
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(ViewModel.IsUserAuthenticated ? typeof(MainPage) : typeof(LoginPage));
                }
            }

            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (e.SourcePageType == typeof(ReelPage))
            {
                return;
            }

            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            if (!ViewModel.IsUserAuthenticated) return;
            var deferral = e.SuspendingOperation.GetDeferral();
            try
            {
                ViewModel.ReelsFeed.StopReelsFeedUpdateLoop();
                ViewModel.SyncClient.Shutdown();    // Shutdown cleanly is not important here.
                await ViewModel.PushClient.TransferPushSocket();
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception, false);
            }
            finally
            {
                SyncLock.Release();
                deferral.Complete();
            }
        }

        private async void OnResuming(object sender, object e)
        {
            if (!ViewModel.IsUserAuthenticated) return;
            var seqId = ViewModel.Inbox.SeqId;
            var snapshotAt = ViewModel.Inbox.SnapshotAt;
            if (ViewModel.StartedFromMainView)
            {
                SyncLock.Acquire();
                await ViewModel.UpdateInboxAndSelectedThread();
                ViewModel.ReelsFeed.StartReelsFeedUpdateLoop();
            }

            if (seqId > 0)
            {
                await ViewModel.SyncClient.Start(seqId, snapshotAt, true);
            }
        }

        private async void OnEnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                await ViewModel.SaveDataAsync();
            }
            finally
            {
                deferral.Complete();
            }
        }

        private static void ConfigureMainView()
        {
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(380, 300));
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
        }

        private void SecondaryView_OnConsolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            sender.Consolidated -= SecondaryView_OnConsolidated;
            SecondaryViewIds.Remove(sender.Id);
        }
    }
}
