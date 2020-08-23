using System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Indirect.Controls;
using Indirect.Wrapper;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect.Pages
{
    /// <summary>
    /// Present chat thread on its own page. Useful for multiple windows and My People support.
    /// </summary>
    public sealed partial class ThreadPage : Page, IImmersiveSupport
    {
        private InstaDirectInboxThreadWrapper _thread;

        public ThreadPage()
        {
            this.InitializeComponent();
            Window.Current.SetTitleBar(TitleBarElement);
            Window.Current.Activated += OnWindowFocusChange;
            Window.Current.SizeChanged += OnWindowSizeChanged;
            MediaPopup.Width = Window.Current.Bounds.Width;
            MediaPopup.Height = Window.Current.Bounds.Height - 32;
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            MediaPopup.Width = e.Size.Width;
            MediaPopup.Height = e.Size.Height - 32;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _thread = (InstaDirectInboxThreadWrapper) e?.Parameter ??
                      throw new ArgumentException("Did not receive chat thread to create page");
            if (_thread.IsContactPanel)
            {
                TitleBarElement.Visibility = Visibility.Collapsed;
                MainView.ThreadHeaderVisibility = Visibility.Collapsed;
            }
            ApplicationView.GetForCurrentView().Consolidated += ViewConsolidated;
            ApplicationView.GetForCurrentView().Title = _thread.Title + " - Thread";
            Bindings.Update();
        }

        private void OnWindowFocusChange(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                AppTitle.Opacity = 0.5;
            }
            else
            {
                AppTitle.Opacity = 1;
            }
        }

        private void ViewConsolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            ApplicationView.GetForCurrentView().Consolidated -= ViewConsolidated;
            ((App) Application.Current).ViewModel.SecondaryThreadViews.Remove(_thread);
        }

        private void CloseMediaPopup_OnClick(object sender, RoutedEventArgs e)
        {
            CloseImmersiveView();
        }

        public void OpenImmersiveView(object item)
        {
            MediaPopup.IsOpen = true;
            ImmersiveControl.Item = item;
            ImmersiveControl.Focus(FocusState.Programmatic);
        }

        public void CloseImmersiveView()
        {
            MediaPopup.IsOpen = false;
            ImmersiveControl.OnClose();
        }
    }
}
