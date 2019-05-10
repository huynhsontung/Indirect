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
            var result = await ViewModel.GetInboxThread(((InstaSharper.Classes.Models.InstaDirectInboxThread)e.AddedItems[0]).ThreadId);
            if (result.Succeeded)
            {
                //int index = ViewModel.InboxThreads.IndexOf((InstaSharper.Classes.Models.InstaDirectInboxThread)e.AddedItems[0]);
                //ViewModel.InboxThreads[index] = result.Value;

                ((InstaSharper.Classes.Models.InstaDirectInboxThread)e.AddedItems[0]).Items = result.Value.Items;
                ViewModel.InboxThreadItems.Clear();
                foreach(var item in result.Value.Items)
                {
                    ViewModel.InboxThreadItems.Add(item);
                }
            }
        }
    }

    public class BoolToAlignmentConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            bool? b = (bool?)value;
            if (b ?? false)
            {
                return "Right";
            }
            return "Left";

        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
