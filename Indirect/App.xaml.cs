using System;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Indirect.Utilities;
using InstagramAPI;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp.UI;
using UnhandledExceptionEventArgs = Windows.UI.Xaml.UnhandledExceptionEventArgs;

namespace Indirect
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private readonly Windows.Storage.ApplicationDataContainer _localSettings =
            Windows.Storage.ApplicationData.Current.LocalSettings;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
#if !DEBUG
            AppCenter.Start(APPCENTER_SECRET, typeof(Analytics), typeof(Crashes));
#endif
            this.InitializeComponent();
            SetTheme();
            this.Suspending += OnSuspending;
            this.Resuming += OnResuming;
            this.EnteredBackground += OnEnteredBackground;
            ImageCache.Instance.CacheDuration = TimeSpan.FromDays(7);
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

        private void OnLaunchedOrActivated(IActivatedEventArgs e)
        {
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(380,300));
            Frame rootFrame = Window.Current.Content as Frame;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
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

            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(Instagram.IsUserAuthenticatedPersistent ? typeof(MainPage) : typeof(Login));
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
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
            var viewModel = ApiContainer.Instance;
            if (!viewModel.IsUserAuthenticated) return;
            var deferral = e.SuspendingOperation.GetDeferral();
            try
            {
                viewModel.StopReelsFeedUpdateLoop();
                viewModel.SyncClient.Shutdown();    // Shutdown cleanly is not important here.
                await viewModel.PushClient.TransferPushSocket();    // Has to wait for Dotnetty to shutdown
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void OnResuming(object sender, object e)
        {
            var viewModel = ApiContainer.Instance;
            viewModel.PushClient.Start();
            await viewModel.SyncClient.Start(viewModel.Inbox.SeqId, viewModel.Inbox.SnapshotAt);
            viewModel.UpdateInboxAndSelectedThread();
            viewModel.StartReelsFeedUpdateLoop();
        }

        private void OnEnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            Instagram.Instance.SaveToAppSettings();
        }
    }
}
