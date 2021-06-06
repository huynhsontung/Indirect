using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Contacts;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Indirect.Entities.Wrappers;
using Indirect.Services;
using InstagramAPI;
using InstagramAPI.Utils;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect.Pages
{
    public sealed partial class ContactPanelPage : Page
    {
        private DirectThreadWrapper _thread;

        private ContactPanel _contactPanel;

        private MainViewModel ViewModel { get; }

        public ContactPanelPage()
        {
            this.InitializeComponent();
            ViewModel = ((App)Application.Current).ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (_thread != null) return;
            var args = e?.Parameter as ContactPanelActivatedEventArgs;
            if (args == null) throw new ArgumentException("Did not receive ContactPanelActivatedEventArgs");
            _contactPanel = args.ContactPanel;
            _contactPanel.Closing += ContactPanelOnClosing;
            var contact = await ContactsService.GetFullContact(args.Contact.Id);
            _thread = await GetThread(contact);
            Bindings.Update();
            if (_thread != null)
            {
                ViewModel.SecondaryThreads.Add(_thread);
                await OptionallyStartSyncClient().ConfigureAwait(false);
            }
        }

        private async Task OptionallyStartSyncClient()
        {
            if (ViewModel.SyncClient.IsRunning) return;
            var seqId = ViewModel.Inbox.SeqId;
            var snapshotAt = ViewModel.Inbox.SnapshotAt;
            if (seqId == default || snapshotAt == default)
            {
                var result = await ViewModel.InstaApi.GetInboxInfoAsync();
                if (result.IsSucceeded)
                {
                    await ViewModel.SyncClient.Start(result.Value.SeqId, result.Value.SnapshotAt);
                }
            }
            else
            {
                await ViewModel.SyncClient.Start(seqId, snapshotAt);
            }
        }

        private void ContactPanelOnClosing(ContactPanel sender, ContactPanelClosingEventArgs args)
        {
            ViewModel.SecondaryThreads.Remove(_thread);
            MainView.UnsubscribeHandlers();
        }

        private async Task<DirectThreadWrapper> GetThread(Contact contact)
        {
            try
            {
                if (contact == null)
                {
                    ShowErrorMessage("Error getting contact. Please make sure Indirect has access to Contacts.");
                    return null;
                }
                if (!ViewModel.IsUserAuthenticated)
                {
                    ShowErrorMessage("Not logged in.");
                    return null;
                }
                var pk = contact.Phones
                    .SingleOrDefault(x => x.Number.Contains("@indirect", StringComparison.OrdinalIgnoreCase))?.Number
                    .Split("@").FirstOrDefault();
                if (string.IsNullOrEmpty(pk))
                {
                    ShowErrorMessage("Contact ID not available.");
                    return null;
                }

                var thread = await ViewModel.FetchThread(new[] { long.Parse(pk, NumberStyles.Integer) }, Dispatcher);
                if (thread == null)
                {
                    ShowErrorMessage("Cannot fetch chat thread.");
                }

                return thread;
            }
            catch (Exception e)
            {
                ShowErrorMessage(e.ToString());
                return null;
            }
        }

        private void ShowErrorMessage(string message)
        {
            ErrorTextBlock.Text = message;
        }
    }
}
