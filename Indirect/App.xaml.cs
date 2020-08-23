﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Indirect.Pages;
using InstagramAPI;
using InstagramAPI.Utils;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp.UI;

namespace Indirect
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        internal ApiContainer ViewModel { get; } = ApiContainer.Instance;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
#if !DEBUG
            AppCenter.Start(Secrets.APPCENTER_SECRET, typeof(Analytics), typeof(Crashes));
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

        private async void OnLaunchedOrActivated(IActivatedEventArgs e)
        {
            await ViewModel.TryAcquireSyncLock();
            
            if (e.Kind != ActivationKind.ContactPanel)
            {
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(380, 300));
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
                    rootFrame.Navigate(Instagram.IsUserAuthenticatedPersistent ? typeof(MainPage) : typeof(LoginPage));
                }
            }
            else
            {
                var cpEventArgs = (ContactPanelActivatedEventArgs) e;
                await HandleActivatedFromContactPanel(cpEventArgs);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        private async Task HandleActivatedFromContactPanel(ContactPanelActivatedEventArgs e)
        {
            var rootFrame = new Frame();

            Window.Current.Content = rootFrame;

            if (!Instagram.IsUserAuthenticatedPersistent)
            {
                rootFrame.Navigate(typeof(NotAvailablePage), "Not logged in");
            }
            else
            {
                var contact = await ContactsIntegration.GetFullContact(e.Contact.Id);
                var pk = contact.Phones.SingleOrDefault(x => x.Number.ToLower().Contains("@indirect"))?.Number
                    .Split("@").FirstOrDefault();
                if (string.IsNullOrEmpty(pk))
                {
                    rootFrame.Navigate(typeof(NotAvailablePage), "Contact ID not available");
                    return;
                }

                var thread = await ViewModel.FetchThread(new[] {long.Parse(pk)}, rootFrame.Dispatcher);
                if (thread == null)
                {
                    rootFrame.Navigate(typeof(NotAvailablePage), "Cannot fetch chat thread");
                    return;
                }

                thread.IsContactPanel = true;
                rootFrame.Navigate(typeof(ThreadPage), thread);
            }
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
            if (!ViewModel.IsUserAuthenticated) return;
            var deferral = e.SuspendingOperation.GetDeferral();
            try
            {
                ViewModel.ReelsFeed.StopReelsFeedUpdateLoop();
                ViewModel.SyncClient.Shutdown();    // Shutdown cleanly is not important here.
                await ViewModel.PushClient.TransferPushSocket();
                ViewModel.ReleaseSyncLock();
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception, false);
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void OnResuming(object sender, object e)
        {
            await ViewModel.TryAcquireSyncLock();
            ViewModel.PushClient.Start();
            await ViewModel.SyncClient.Start(ViewModel.Inbox.SeqId, ViewModel.Inbox.SnapshotAt);
            await ViewModel.UpdateInboxAndSelectedThread();
            ViewModel.ReelsFeed.StartReelsFeedUpdateLoop();
        }

        private void OnEnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            Instagram.Instance.SaveToAppSettings();
        }

        public static async Task<bool> CreateAndShowNewView(Type targetPage, object parameter = null, CoreApplicationView view = null)
        {
            var newView = view ?? CoreApplication.CreateNewView();
            int newViewId = 0;
            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(380, 300));
                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;
                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

                var frame = new Frame();
                frame.Navigate(targetPage, parameter);
                Window.Current.Content = frame;
                // You have to activate the window in order to show it later.
                Window.Current.Activate();

                newViewId = ApplicationView.GetForCurrentView().Id;
            });
            return await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
        }
    }
}
