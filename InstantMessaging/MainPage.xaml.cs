using InstantMessaging.Wrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace InstantMessaging
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public ApiContainer ViewModel { get; set; }
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel = (ApiContainer)e.Parameter;
            await ViewModel.GetInboxAsync();
            Frame.BackStack.RemoveAt(Frame.BackStack.Count - 1);
        }

        private async void MessageContent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] == null)
                return;
            await ViewModel.GetInboxThread(e.AddedItems[0] as InstaDirectInboxThreadWrapper);           
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {

            var button = sender as Control;
            var messageBox = (button.Parent as Grid).Children[0] as TextBox;
            var message = messageBox.Text;
            await ViewModel.SendMessage(message);
            messageBox.Text = "";
        }

        private void MessageTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
            {
                SendButton_Click(sender, e);
            }
        }
    }
}
