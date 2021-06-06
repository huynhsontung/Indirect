using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Indirect.Entities;
using Indirect.Entities.Wrappers;
using Indirect.Pages;
using Indirect.Services;
using Indirect.Utilities;
using InstagramAPI;
using InstagramAPI.Classes.Android;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.User;
using InstagramAPI.Push;
using InstagramAPI.Sync;
using InstagramAPI.Utils;
using Microsoft.Toolkit.Uwp.Helpers;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Classes.Core;

namespace Indirect
{
    internal partial class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private DateTimeOffset _lastUpdated = DateTimeOffset.Now;
        private DirectThreadWrapper _selectedThread;
        private FileStream _lockFile;
        private string _threadToBeOpened;

        public event PropertyChangedEventHandler PropertyChanged;

        public Instagram InstaApi { get; private set; }
        public bool BackgroundSyncLocked => _lockFile != null;
        public bool StartedFromMainView { get; set; }
        public PushClient PushClient => InstaApi.PushClient;
        public SyncClient SyncClient => InstaApi.SyncClient;
        public Dictionary<long, UserPresenceValue> UserPresenceDictionary { get; } = new Dictionary<long, UserPresenceValue>();
        public InboxWrapper PendingInbox { get; }
        public InboxWrapper Inbox { get; }
        public List<DirectThreadWrapper> SecondaryThreads { get; } = new List<DirectThreadWrapper>();
        public BaseUser LoggedInUser => InstaApi.Session.LoggedInUser;
        public PersistentDictionary<string> ThreadInfoPersistentDictionary { get; } = new PersistentDictionary<string>("ThreadInfoPersistentDictionary");
        public Dictionary<long, BaseUser> CentralUserRegistry { get; } = new Dictionary<long, BaseUser>();
        public DirectThreadWrapper SelectedThread
        {
            get => _selectedThread;
            set
            {
                _selectedThread = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedThread)));
            }
        }

        public AndroidDevice Device => InstaApi?.Device;
        public bool IsUserAuthenticated => InstaApi.IsUserAuthenticated;
        public ReelsFeed ReelsFeed { get; } = new ReelsFeed();
        public ChatService ChatService { get; }

        public MainViewModel()
        {
            Inbox = new InboxWrapper(this);
            //PendingInbox = new InboxWrapper(this, true);
            ChatService = new ChatService(this);
            ThreadInfoPersistentDictionary.LoadFromAppSettings();

            Inbox.FirstUpdated += OnInboxFirstUpdated;
        }

        public async Task Initialize()
        {
            var session = await SessionManager.TryLoadLastSessionAsync() ?? new UserSessionData();
            InstaApi = new Instagram(session);
            RegisterSyncClientHandlers(InstaApi.SyncClient);
            InstaApi.PushClient.MessageReceived += (sender, args) =>
            {
                this.Log("Background notification: " + args.Json);
            };
        }

        public async Task OnLoggedIn()
        {
            if (!IsUserAuthenticated) throw new Exception("User is not logged in.");
            await Inbox.ClearInbox();
            GetUserPresence();
            PushClient.Start();
            await ReelsFeed.UpdateReelsFeed();
            ReelsFeed.StartReelsFeedUpdateLoop();

            // Disabled due to store certification failed
            //await Task.Delay(10000).ConfigureAwait(false);
            //await ContactsService.SaveUsersAsContact(CentralUserRegistry.Values).ConfigureAwait(false);
        }

        public void SetSelectedThreadNull()
        {
            SelectedThread = null;
        }

        public void OpenThreadWhenReady(string threadId)
        {
            if (Inbox.Threads.Count > 0)
            {
                SelectedThread = Inbox.Threads.FirstOrDefault(x => x.Source.ThreadId == threadId);
            }
            else
            {
                _threadToBeOpened = threadId;
            }
        }

        public Task<Result<LoginResult>> Login(string username, string password) => InstaApi.LoginAsync(username, password);

        public Task<Result<LoginResult>> LoginWithFacebook(string fbAccessToken) =>
            InstaApi.LoginWithFacebookAsync(fbAccessToken);

        public async Task Logout()
        {
            await InstaApi.Logout();
            //await ContactsService.DeleteAllAppContacts();
            ThreadInfoPersistentDictionary.RemoveFromAppSettings();
            // TODO: Close all secondary views
            // _settings.Values.Clear();
        }

        public async Task UpdateLoggedInUser()
        {
            var succeeded = await InstaApi.UpdateLoggedInUser();
            if (!succeeded) return;
            await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoggedInUser)));
            });
        }

        public async Task UpdateThread(DirectThreadWrapper thread)
        {
            if (thread == null)
            {
                return;
            }
            
            var result = await InstaApi.GetThreadAsync(thread.Source.ThreadId, PaginationParameters.MaxPagesToLoad(1));
            if (result.IsSucceeded)
            {
                await thread.Dispatcher.AwaitableRunAsync(() => { thread.Update(result.Value); });
            }
        }

        public async Task UpdateInboxAndSelectedThread()
        {
            _lastUpdated = DateTime.Now;
            await Inbox.UpdateInbox();
            if (SelectedThread == null) return;
            var selectedThread = SelectedThread;
            if (Inbox.Threads.Contains(selectedThread))
            {
                await UpdateThread(selectedThread);
                await selectedThread.MarkLatestItemSeen();
            }
            else
            {
                var preferSelectedThread = Inbox.Threads.FirstOrDefault(x =>
                    x != null && x.Source.ThreadId == selectedThread.Source.ThreadId);
                if (preferSelectedThread != null)
                {
                    SelectedThread = preferSelectedThread;
                }
            }
        }

        private static Task<bool> SearchReady() => Debouncer.Delay("ThreadSearch", 500);

        public async void Search(string query, Action<List<DirectThreadWrapper>> updateAction)
        {
            if (query.Length > 50) return;
            if (!await SearchReady()) return;

            var result = await InstaApi.GetRankedRecipientsByUsernameAsync(query);
            if (!result.IsSucceeded) return;
            var recipients = result.Value;
            var threadsFromUser = recipients.Users.Select(x => new DirectThreadWrapper(this, x)).ToList();
            var threadsFromRankedThread = recipients.Threads.Select(x => new DirectThreadWrapper(this, x)).ToList();
            var list = new List<DirectThreadWrapper>(threadsFromRankedThread.Count + threadsFromUser.Count);
            list.AddRange(threadsFromRankedThread);
            list.AddRange(threadsFromUser);
            var decoratedList = list.Select(x =>
            {
                var directThread = x.Source;
                if (directThread.LastPermanentItem == null)
                {
                    directThread.LastPermanentItem = new DirectItem();
                }

                directThread.LastPermanentItem.Text = x.Users.Count == 1 ? x.Users?[0].FullName : $"{x.Users.Count} participants";
                return x;
            }).ToList();
            updateAction?.Invoke(decoratedList);
        }

        public async void SearchWithoutThreads(string query, Action<List<BaseUser>> updateAction)
        {
            if (query.Length > 50) return;
            if (!await SearchReady()) return;

            var result = await InstaApi.GetRankedRecipientsByUsernameAsync(query, false);
            if (!result.IsSucceeded) return;
            var recipients = result.Value.Users;
            if (recipients?.Count > 0)
                updateAction?.Invoke(recipients);
        }

        public async Task OpenThreadInNewWindow(DirectThreadWrapper thread)
        {
            var newView = CoreApplication.CreateNewView();
            var cloneThread = await thread.CloneThreadForSecondaryView(newView.Dispatcher);
            if (cloneThread == null) return;
            SecondaryThreads.Add(cloneThread);
            await ((App) App.Current).CreateAndShowNewView(typeof(ThreadPage), cloneThread, newView);
        }

        public async Task CreateAndOpenThread(IEnumerable<long> userIds)
        {
            var result = await InstaApi.CreateGroupThreadAsync(userIds);
            if (!result.IsSucceeded) return;
            var thread = result.Value;
            var existingThread = Inbox.Threads.FirstOrDefault(x => x.Source.ThreadId == thread.ThreadId);
            SelectedThread = existingThread ?? new DirectThreadWrapper(this, thread);
        }

        public async Task<DirectThreadWrapper> FetchThread(IEnumerable<long> userIds, CoreDispatcher dispatcher)
        {
            var result = await InstaApi.GetThreadByParticipantsAsync(userIds);
            return !result.IsSucceeded
                ? null
                : new DirectThreadWrapper(this, result.Value, dispatcher);
        }

        public async void MakeProperInboxThread(DirectThreadWrapper placeholderThread)
        {
            DirectThreadWrapper thread;
            if (string.IsNullOrEmpty(placeholderThread.Source.ThreadId))
            {
                var userIds = placeholderThread.Users.Select(x => x.Pk);
                var result = await InstaApi.GetThreadByParticipantsAsync(userIds);
                if (!result.IsSucceeded) return;
                thread = result.Value != null && result.Value.Users.Count > 0 ? 
                    new DirectThreadWrapper(this, result.Value) : new DirectThreadWrapper(this, placeholderThread.Users?[0]);
            }
            else
            {
                thread = placeholderThread;
            }

            foreach (var existingThread in Inbox.Threads)
            {
                if (!thread.Equals(existingThread)) continue;
                thread = existingThread;
                break;
            }

            if (thread.Source.LastPermanentItem == null)
            {
                thread.Source.LastPermanentItem = new DirectItem() {Description = thread.Users?[0].FullName};
            }

            SelectedThread = thread;
        }

        private async void GetUserPresence()
        {
            try
            {
                var presenceResult = await InstaApi.GetPresence();
                if (!presenceResult.IsSucceeded) return;
                foreach (var userPresenceValue in presenceResult.Value.UserPresence)
                {
                    UserPresenceDictionary[userPresenceValue.Key] = userPresenceValue.Value;
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
            }
        }

        public static Task HandleException(string message = null, Exception e = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "An unexpected error has occured. Indirect doesn't know how to proceed next and may crash. " +
                          "If this happens frequently, please submit an issue on Indirect's Github page.\n\n" +
                          "https://github.com/huynhsontung/Indirect";
            }

            return CoreApplication.MainView.CoreWindow.Dispatcher.QuickRunAsync(async () =>
            {
                try
                {
                    var dialog = new ContentDialog()
                    {
                        Title = "An error occured",
                        Content = new ScrollViewer()
                        {
                            Content = new TextBlock()
                            {
                                Text = message,
                                TextWrapping = TextWrapping.Wrap,
                                IsTextSelectionEnabled = true
                            },
                            HorizontalScrollMode = ScrollMode.Disabled,
                            VerticalScrollMode = ScrollMode.Auto,
                            MaxWidth = 400
                        },
                        CloseButtonText = "Close",
                        DefaultButton = ContentDialogButton.Close
                    };
                    await dialog.ShowAsync();
                }
                catch (Exception innerException)
                {
                    Debug.WriteLine(innerException);
                }

                // Intentionally crash the app
                if (e != null) throw e;
            });
        }

        public void Dispose()
        {
            ReleaseSyncLock();
        }

        public async Task SaveDataAsync()
        {
            ThreadInfoPersistentDictionary.SaveToAppSettings();
            await SessionManager.SaveSessionAsync(InstaApi);
        }

        internal async Task<bool> TryAcquireSyncLock()
        {
            if (BackgroundSyncLocked) return true;
            var storageFolder = ApplicationData.Current.LocalFolder;
            var storageItem = await storageFolder.CreateFileAsync("SyncLock.mutex", CreationCollisionOption.OpenIfExists);
            try
            {
                _lockFile = new FileStream(storageItem.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        internal void ReleaseSyncLock()
        {
            try
            {
                _lockFile?.Dispose();
                _lockFile = null;
            }
            catch (Exception)
            {
                // pass
            }
        }
    }
}