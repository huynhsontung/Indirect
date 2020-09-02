using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation.Metadata;
using Windows.Storage.Streams;
using InstagramAPI;
using InstagramAPI.Classes.User;

namespace Indirect.Services
{
    internal static class ContactsService
    {
        private const string APP_ID = "18496Starpine.Indirect_rm8wvch11q4my!App";

        private static async Task<ContactList> GetContactList()
        {
            ContactStore store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
            if (null == store) return null;

            ContactList contactList;
            IReadOnlyList<ContactList> contactLists = await store.FindContactListsAsync();
            if (0 == contactLists.Count)
            {
                contactList = await store.CreateContactListAsync("Indirect");
            }
            else
            {
                contactList = contactLists[0];
            }
            
            return contactList;
        }

        private static async Task<ContactAnnotationList> GetContactAnnotationList()
        {
            ContactAnnotationStore annotationStore = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);
            if (null == annotationStore) return null;

            ContactAnnotationList annotationList;
            IReadOnlyList<ContactAnnotationList> annotationLists = await annotationStore.FindAnnotationListsAsync();
            if (0 == annotationLists.Count)
            {
                annotationList = await annotationStore.CreateAnnotationListAsync();
            }
            else
            {
                annotationList = annotationLists[0];
            }

            return annotationList;
        }

        public static async Task<bool> TryFetchContactStores()
        {
            try
            {
                var contactStore = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
                var annotationStore = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);
                if (contactStore == null || annotationStore == null) return false;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public static async Task<Contact> GetFullContact(string contactId)
        {
            var store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
            if (null == store) return null;
            try
            {
                var contact = await store.GetContactAsync(contactId);
                return contact;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task DeleteAllAppContacts()
        {
            var store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
            if (store == null) return;
            IReadOnlyList<ContactList> contactLists = await store.FindContactListsAsync();
            if (0 == contactLists.Count) return;
            var contactList = contactLists[0];
            await contactList.DeleteAsync();
        }

        public static async Task SaveUsersAsContact(ICollection<BaseUser> users)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
            {
                if (users == null || users.Count == 0) return;
                var contactList = await GetContactList();
                var annotationList = await GetContactAnnotationList();
                if (contactList == null || annotationList == null) return;

                var toBeAdded = new List<BaseUser>();
                var currentUser = Instagram.Instance.Session.LoggedInUser;
                foreach (var user in users)
                {
                    if (user.Pk == currentUser.Pk) continue;
                    var existingContact = await contactList.GetContactFromRemoteIdAsync(user.Pk + "@Indirect");
                    if (existingContact == null)
                    {
                        toBeAdded.Add(user);
                    }
                }

                if (toBeAdded.Count == 0) return;

                var contacts = toBeAdded.Select(x => new Contact
                {
                    FirstName = x.Username,
                    RemoteId = x.Pk + "@Indirect",
                    Phones =
                    {
                        new ContactPhone
                        {
                            Number = x.Pk + "@Indirect",
                            Kind = ContactPhoneKind.Other,
                            Description = "Indirect's internal ID, do not change."
                        }
                    },
                    SourceDisplayPicture = RandomAccessStreamReference.CreateFromUri(x.ProfilePictureUrl)
                });

                foreach (var contact in contacts)
                {
                    await contactList.SaveContactAsync(contact);
                    var annotation = new ContactAnnotation
                    {
                        ContactId = contact.Id,
                        RemoteId = contact.RemoteId,
                        SupportedOperations = ContactAnnotationOperations.ContactProfile,
                    };
                    annotation.ProviderProperties.Add("ContactPanelAppID", APP_ID);
                    var result = await annotationList.TrySaveAnnotationAsync(annotation);
                }
            }
        }
    }
}
