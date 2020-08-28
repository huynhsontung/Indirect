using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Indirect.Entities.Wrappers;
using Indirect.Services;
using InstagramAPI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect.Pages
{
    public sealed partial class ContactPanelPage : Page
    {
        private DirectThreadWrapper _thread;

        private ContactPanel _contactPanel;

        private static MainViewModel ViewModel => ((App)Application.Current).ViewModel;

        public ContactPanelPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e?.Parameter as ContactPanelActivatedEventArgs;
            if (args == null) throw new ArgumentException("Did not receive ContactPanelActivatedEventArgs");
            _contactPanel = args.ContactPanel;
            _contactPanel.Closing += ContactPanelOnClosing;
            var contact = await ContactsService.GetFullContact(args.Contact.Id);
            _thread = await GetThread(contact);
            if (_thread != null)
                ViewModel.SecondaryThreadViews.Add(_thread);
            Bindings.Update();
        }

        private void ContactPanelOnClosing(ContactPanel sender, ContactPanelClosingEventArgs args)
        {
            ViewModel.SecondaryThreadViews.Remove(_thread);
            MainView.UnsubscribeHandlers();
        }

        private async Task<DirectThreadWrapper> GetThread(Contact contact)
        {
            if (!Instagram.IsUserAuthenticatedPersistent)
            {
                ShowErrorMessage("Not logged in");
                return null;
            }
            var pk = contact.Phones
                .SingleOrDefault(x => x.Number.Contains("@indirect", StringComparison.OrdinalIgnoreCase))?.Number
                .Split("@").FirstOrDefault();
            if (string.IsNullOrEmpty(pk))
            {
                ShowErrorMessage("Contact ID not available");
                return null;
            }

            var thread = await ViewModel.FetchThread(new[] { long.Parse(pk, NumberStyles.Integer) }, Dispatcher);
            if (thread == null)
            {
                ShowErrorMessage("Cannot fetch chat thread");
            }

            return thread;
        }

        private void ShowErrorMessage(string message)
        {
            ErrorTextBlock.Text = message;
        }
    }
}
