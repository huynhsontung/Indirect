using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Indirect.Wrapper;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Indirect
{
    sealed partial class ThreadDetailsView : UserControl
    {
        public static readonly DependencyProperty ThreadProperty = DependencyProperty.Register(
            nameof(Thread),
            typeof(InstaDirectInboxThreadWrapper),
            typeof(ThreadDetailsView),
            new PropertyMetadata(null, OnThreadChanged));
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ApiContainer),
            typeof(ThreadDetailsView),
            new PropertyMetadata(null));

        public InstaDirectInboxThreadWrapper Thread
        {
            get => (InstaDirectInboxThreadWrapper) GetValue(ThreadProperty);
            set => SetValue(ThreadProperty, value);
        }

        public ApiContainer ViewModel
        {
            get => (ApiContainer) GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        private static void OnThreadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ThreadDetailsView) d;
            view.HandleThreadSourceChanged();
        }


        public ThreadDetailsView()
        {
            this.InitializeComponent();
        }


        private void HandleThreadSourceChanged()
        {
            DataContext = Thread;
            this.Bindings.Update();

        }

        private void RefreshThread_OnClick(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.UpdateInboxAndSelectedThread();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Control;
            var messageBox = (TextBox)(button.Parent as Grid).Children[0];
            var message = messageBox.Text;
            messageBox.Text = "";
            _ = ViewModel.SendMessage(message);
        }

        private void MessageTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SendButton_Click(sender, e);
            }
        }
    }
}
