using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Indirect.Entities;
using Indirect.Entities.Wrappers;
using Indirect.Pages;
using Indirect.Services;
using InstagramAPI;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Android;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.User;
using InstagramAPI.Push;
using InstagramAPI.Sync;
using InstagramAPI.Utils;
using Microsoft.Toolkit.Uwp.UI;

namespace Indirect
{
    internal partial class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private static MainViewModel _instance;
        public static MainViewModel Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new MainViewModel();
                return _instance;
            }
        }


        private DateTimeOffset _lastUpdated = DateTimeOffset.Now;
        private CancellationTokenSource _searchCancellationToken;
        private DirectThreadWrapper _selectedThread;
        private FileStream _lockFile;
        private string _threadToBeOpened;

        public event PropertyChangedEventHandler PropertyChanged;

        public Instagram InstaApi { get; } = Instagram.Instance;
        public bool BackgroundSyncLocked => _lockFile != null;
        public bool StartedFromMainView { get; set; }
        public PushClient PushClient => InstaApi.PushClient;
        public SyncClient SyncClient => InstaApi.SyncClient;
        public Dictionary<long, UserPresenceValue> UserPresenceDictionary { get; } = new Dictionary<long, UserPresenceValue>();
        public InboxWrapper PendingInbox { get; }
        public InboxWrapper Inbox { get; }
        public List<DirectThreadWrapper> SecondaryThreadViews { get; } = new List<DirectThreadWrapper>();
        public CurrentUser LoggedInUser { get; private set; }
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

        private MainViewModel()
        {
            Inbox = new InboxWrapper(this);
            //PendingInbox = new InboxWrapper(this, true);
            ThreadInfoPersistentDictionary.LoadFromAppSettings();
            SubscribeHandlers();
        }

        public async Task OnLoggedIn()
        {
            if (!InstaApi.IsUserAuthenticated) throw new Exception("User is not logged in.");
            await UpdateLoggedInUser();
            GetUserPresence();
            PushClient.Start();
            await ReelsFeed.UpdateReelsFeed();
            ReelsFeed.StartReelsFeedUpdateLoop();

            // Post launch
            await Task.Delay(10000).ConfigureAwait(false);
            await ContactsService.SaveUsersAsContact(CentralUserRegistry.Values).ConfigureAwait(false);
        }

        public void SetSelectedThreadNull()
        {
            SelectedThread = null;
        }

        public void OpenThreadWhenReady(string threadId)
        {
            if (Inbox.Threads.Count > 0)
            {
                SelectedThread = Inbox.Threads.FirstOrDefault(x => x.ThreadId == threadId);
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
            InstaApi.Logout();
            //await ImageCache.Instance.ClearAsync();
            await ContactsService.DeleteAllAppContacts();
            ThreadInfoPersistentDictionary.RemoveFromAppSettings();
            // _settings.Values.Clear();
        }

        private async Task UpdateLoggedInUser()
        {
            var loggedInUser = await InstaApi.GetCurrentUserAsync();
            LoggedInUser = loggedInUser.Value;
            CentralUserRegistry[LoggedInUser.Pk] = LoggedInUser;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoggedInUser)));
        }

        public async Task UpdateSelectedThread()
        {
            if (SelectedThread == null)
                return;
            try
            {
                var result = await InstaApi.GetThreadAsync(SelectedThread.ThreadId, PaginationParameters.MaxPagesToLoad(1));
                if (result.IsSucceeded)
                    await SelectedThread.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () => { SelectedThread.Update(result.Value); });
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
            }
        }

        public async Task UpdateInboxAndSelectedThread()
        {
            _lastUpdated = DateTime.Now;
            await Inbox.UpdateInbox();
            if (SelectedThread == null) return;
            if (Inbox.Threads.Contains(SelectedThread))
            {
                await UpdateSelectedThread();
                await SelectedThread.MarkLatestItemSeen();
            }
            else
            {
                var preferSelectedThread = Inbox.Threads.FirstOrDefault(x => x.ThreadId == SelectedThread.ThreadId);
                if (preferSelectedThread != null)
                {
                    SelectedThread = preferSelectedThread;
                }
            }
        }

        private async Task<bool> SearchReady()
        {
            _searchCancellationToken?.Cancel();
            _searchCancellationToken?.Dispose();
            _searchCancellationToken = new CancellationTokenSource();
            var cancellationToken = _searchCancellationToken.Token;
            try
            {
                await Task.Delay(500, cancellationToken); // Delay so we don't search something mid typing
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            if (cancellationToken.IsCancellationRequested) return false;
            return true;
        }

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
                if (x.LastPermanentItem == null) x.LastPermanentItem = new DirectItem();
                x.LastPermanentItem.Text = x.Users.Count == 1 ? x.Users?[0].FullName : $"{x.Users.Count} participants";
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
            SecondaryThreadViews.Add(cloneThread);
            await App.CreateAndShowNewView(typeof(ThreadPage), cloneThread, newView);
        }

        public async Task CreateAndOpenThread(IEnumerable<long> userIds)
        {
            var result = await InstaApi.CreateGroupThreadAsync(userIds);
            if (!result.IsSucceeded) return;
            var thread = result.Value;
            var existingThread = Inbox.Threads.FirstOrDefault(x => x.ThreadId == thread.ThreadId);
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
            if (string.IsNullOrEmpty(placeholderThread.ThreadId))
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

            if (thread.LastPermanentItem == null)
            {
                thread.LastPermanentItem = new DirectItem() {Description = thread.Users?[0].FullName};
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

        public static IAsyncAction HandleException(string message = null, Exception e = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "An unexpected error has occured. Indirect doesn't know how to proceed next and may crash. " +
                          "If this happens frequently, please submit an issue on Indirect's Github page.\n\n" +
                          "https://github.com/huynhsontung/Indirect";
            }

            return CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
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
            _searchCancellationToken?.Dispose();
            ReleaseSyncLock();
        }

        public void SaveToAppSettings()
        {
            InstaApi.SaveToAppSettings();
            ThreadInfoPersistentDictionary.SaveToAppSettings();
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