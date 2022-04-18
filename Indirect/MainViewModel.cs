using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Core;
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
using InstagramAPI.Utils;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Classes.Core;
using InstagramAPI.Realtime;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Indirect
{
    [INotifyPropertyChanged]
    internal partial class MainViewModel
    {
        private string _threadToBeOpened;
        private readonly DispatcherQueue _mainWindowDispatcherQueue;

        private string ThreadInfoKey => $"{nameof(ThreadInfoDictionary)}_{LoggedInUser?.Pk}";
        private Dictionary<string, DirectThreadInfo> ThreadInfoDictionary { get; set; }

        public Instagram InstaApi { get; private set; }
        public bool StartedFromMainView { get; set; }
        private PushClient PushClient => InstaApi.PushClient;
        private RealtimeClient RealtimeClient => InstaApi.RealtimeClient;
        public Dictionary<long, UserPresenceValue> UserPresenceDictionary { get; } = new Dictionary<long, UserPresenceValue>();
        public InboxWrapper PendingInbox { get; }
        public InboxWrapper Inbox { get; }
        public List<DirectThreadWrapper> SecondaryThreads { get; } = new List<DirectThreadWrapper>();
        public UserSessionContainer[] AvailableSessions { get; private set; } = Array.Empty<UserSessionContainer>();
        public UserSessionData ActiveSession => InstaApi.Session;
        public BaseUser LoggedInUser => InstaApi.Session.LoggedInUser;
        public AndroidDevice Device => InstaApi?.Device;
        public bool IsUserAuthenticated => InstaApi?.IsUserAuthenticated ?? false;
        public ReelsFeed ReelsFeed { get; } = new ReelsFeed();
        public ChatService ChatService { get; }
        public SettingsService Settings { get; }

        [ObservableProperty]
        private bool _showStoryInNewWindow;

        public MainViewModel(DispatcherQueue mainWindowDispatcherQueue)
        {
            _mainWindowDispatcherQueue = mainWindowDispatcherQueue;
            Inbox = new InboxWrapper(this);
            //PendingInbox = new InboxWrapper(this, true);
            ChatService = new ChatService(this);
            Settings = new SettingsService(this);

            ShowStoryInNewWindow = true;
            if (SettingsService.TryGetGlobal("ShowStoryInNewWindow", out bool? result))
            {
                ShowStoryInNewWindow = result ?? true;
            }

            PropertyChanged += OnPropertyChanged;
            Inbox.FirstUpdated += OnInboxFirstUpdated;
            Inbox.Threads.CollectionChanged += InboxThreads_OnCollectionChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShowStoryInNewWindow))
            {
                SettingsService.SetGlobal("ShowStoryInNewWindow", ShowStoryInNewWindow);
            }
        }

        public async Task Initialize()
        {
            if (InstaApi != null)
            {
                return;
            }

            var session = await SessionManager.TryLoadLastSessionAsync() ?? new UserSessionData();
            InstaApi = new Instagram(session);

            AvailableSessions = await SessionManager.GetAvailableSessionsAsync(InstaApi.Session);
            ThreadInfoDictionary =
                await CacheManager.ReadCacheAsync<Dictionary<string, DirectThreadInfo>>(ThreadInfoKey) ??
                new Dictionary<string, DirectThreadInfo>();
        }

        public async Task OnLoggedIn()
        {
            if (!IsUserAuthenticated) throw new Exception("User is not logged in.");
            SyncLock.Acquire(ActiveSession.SessionName);
            ReelsFeed.StopReelsFeedUpdateLoop(true);
            var tasks = new List<Task>
            {
                InstaApi.RunPostLoginFlow(),
                Inbox.ClearInbox(),
                GetUserPresence(),
                ReelsFeed.UpdateReelsFeedAsync(),
                PushClient.StartFromMainView()
            };

            await Task.WhenAll(tasks).ConfigureAwait(false);
            ReelsFeed.StartReelsFeedUpdateLoop();
            // Disabled due to store certification failed
            //await Task.Delay(10000).ConfigureAwait(false);
            //await ContactsService.SaveUsersAsContact(CentralUserRegistry.Values).ConfigureAwait(false);
        }

        public async Task SwitchAccountAsync(UserSessionData session)
        {
            ReelsFeed.StopReelsFeedUpdateLoop();
            ShutdownRealtimeClient();
            await PushClient.TransferPushSocket();
            ThreadInfoDictionary.Clear();

            InstaApi = new Instagram(session);
            AvailableSessions = await SessionManager.GetAvailableSessionsAsync(InstaApi.Session);
            SyncLock.Acquire(session.SessionName);
        }

        public void SetSelectedThreadNull()
        {
            Inbox.SelectedThread = null;
        }

        public void OpenThreadWhenReady(string threadId)
        {
            if (Inbox.Threads.Count > 0)
            {
                Inbox.SelectedThread = Inbox.Threads.FirstOrDefault(x => x.Source.ThreadId == threadId);
            }
            else
            {
                _threadToBeOpened = threadId;
            }
        }

        public async Task<bool> Logout()
        {
            ReelsFeed.StopReelsFeedUpdateLoop(true);
            await CacheManager.RemoveCacheAsync(ThreadInfoKey);
            await SessionManager.TryRemoveSessionAsync(InstaApi.Session);
            ThreadInfoDictionary.Clear();

            ShutdownRealtimeClient();
            PushClient.Shutdown();
            PushClient.DisposeBackgroundSocket();

            SyncLock.Release();

            if (AvailableSessions.Length > 0)
            {
                await SwitchAccountAsync(AvailableSessions[0].Session);
                return false;
            }
            else
            {
                //await ContactsService.DeleteAllAppContacts();
                InstaApi = new Instagram(InstaApi.Session);
                return true;
            }
        }

        public async Task UpdateLoggedInUser()
        {
            var user = await InstaApi.UpdateLoggedInUser();
            if (user == null) return;
            _mainWindowDispatcherQueue.TryEnqueue(() =>
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
            
            var result = await InstaApi.GetThreadAsync(thread.Source.ThreadId, Inbox.SeqId, PaginationParameters.MaxPagesToLoad(1));
            if (result.IsSucceeded)
            {
                await thread.Dispatcher.QuickRunAsync(() => { thread.Update(result.Value); });
            }
        }

        public async Task UpdateInboxAndSelectedThread()
        {
            await Inbox.UpdateInbox();
            if (Inbox.SelectedThread == null) return;
            var selectedThread = Inbox.SelectedThread;
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
                    Inbox.SelectedThread = preferSelectedThread;
                }
            }
        }

        private static Task<bool> SearchReady() => Debouncer.Delay("ThreadSearch", 150);

        public async void Search(string query, Action<List<DirectThreadWrapper>> updateAction)
        {
            if (query.Length > 50) return;
            if (!await SearchReady()) return;

            var result = await InstaApi.GetRankedRecipientsByUsernameAsync(query);
            if (!result.IsSucceeded) return;
            var recipients = result.Value;
            var threadsFromUser = recipients.Users.Select(x => new DirectThreadWrapper(x, this)).ToList();
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
            await newView.Dispatcher.QuickRunAsync(async () =>
            {
                var cloneThread = await thread.CloneThread();
                if (cloneThread == null) return;
                var wrapper = new DirectThreadWrapper(this, cloneThread);
                SecondaryThreads.Add(wrapper);
                await ((App) App.Current).CreateAndShowNewView(typeof(ThreadPage), wrapper, newView);
            });
        }

        public async Task CreateAndOpenThread(IEnumerable<long> userIds)
        {
            var result = await InstaApi.CreateGroupThreadAsync(userIds);
            if (!result.IsSucceeded) return;
            var thread = result.Value;
            var existingThread = Inbox.Threads.FirstOrDefault(x => x.Source.ThreadId == thread.ThreadId);
            Inbox.SelectedThread = existingThread ?? new DirectThreadWrapper(this, thread);
        }

        public async Task<DirectThreadWrapper> FetchThread(IEnumerable<long> userIds, CoreDispatcher dispatcher)
        {
            var result = await InstaApi.GetThreadByParticipantsAsync(userIds);
            return !result.IsSucceeded
                ? null
                : new DirectThreadWrapper(this, result.Value);
        }

        public async void MakeProperInboxThread(DirectThreadWrapper placeholderThread)
        {
            DirectThreadWrapper thread;
            if (string.IsNullOrEmpty(placeholderThread.Source.ThreadId))
            {
                var userIds = placeholderThread.Users.Select(x => x.Pk);
                var result = await InstaApi.GetThreadByParticipantsAsync(userIds);
                if (result.IsSucceeded)
                {
                    thread = result.Value != null && result.Value.Users.Count > 0 ? 
                        new DirectThreadWrapper(this, result.Value) : new DirectThreadWrapper(placeholderThread.Users?[0], this);
                }
                else
                {
                    thread = placeholderThread;
                }
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

            Inbox.SelectedThread = thread;
        }

        private void InboxThreads_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null || ThreadInfoDictionary == null)
            {
                return;
            }

            foreach (var item in e.NewItems)
            {
                var thread = (DirectThreadWrapper)item;
                if (string.IsNullOrEmpty(thread.ThreadId))
                {
                    continue;
                }

                ThreadInfoDictionary[thread.ThreadId] = new DirectThreadInfo(thread.Source);
            }
        }

        private async Task GetUserPresence()
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

        public async Task SaveDataAsync()
        {
            if (!IsUserAuthenticated)
            {
                return;
            }

            await SessionManager.SaveSessionAsync(InstaApi);
            await CacheManager.WriteCacheAsync(ThreadInfoKey, ThreadInfoDictionary);

            var composite = new Windows.Storage.ApplicationDataCompositeValue();
            composite[LoggedInUser.Pk.ToString()] = LoggedInUser.Username;

            foreach (var sessionContainer in AvailableSessions)
            {
                var user = sessionContainer.Session.LoggedInUser;
                composite[user.Pk.ToString()] = user.Username;
            }

            SettingsService.SetGlobal("LoggedInUsers", composite);
        }

        public async Task OnSuspending()
        {
            try
            {
                ReelsFeed.StopReelsFeedUpdateLoop();
                ShutdownRealtimeClient();
                await PushClient.TransferPushSocket();
            }
            finally
            {
                SyncLock.Release();
            }
        }

        public async Task OnResuming()
        {
            if (Inbox.SeqId > 0)
            {
                await StartRealtimeClient();
            }

            if (StartedFromMainView)
            {
                SyncLock.Acquire(ActiveSession.SessionName);
                await UpdateInboxAndSelectedThread();
                ReelsFeed.StartReelsFeedUpdateLoop();
            }
        }
    }
}