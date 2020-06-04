using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Indirect.Utilities;
using Indirect.Wrapper;
using InstagramAPI;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Android;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.Media;
using InstagramAPI.Classes.Responses;
using InstagramAPI.Classes.User;
using InstagramAPI.Push;
using InstagramAPI.Sync;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using Buffer = Windows.Storage.Streams.Buffer;

namespace Indirect
{
    internal partial class ApiContainer : INotifyPropertyChanged, IDisposable
    {
        private static ApiContainer _instance;
        public static ApiContainer Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new ApiContainer();
                return _instance;
            }
        }


        private readonly Instagram _instaApi = Instagram.Instance;
        private DateTimeOffset _lastUpdated = DateTimeOffset.Now;
        private CancellationTokenSource _searchCancellationToken;
        private InstaDirectInboxThreadWrapper _selectedThread;

        public event PropertyChangedEventHandler PropertyChanged;

        public PushClient PushClient => _instaApi.PushClient;
        public SyncClient SyncClient => _instaApi.SyncClient;

        public Dictionary<long, UserPresenceValue> UserPresenceDictionary { get; } = new Dictionary<long, UserPresenceValue>();
        public InstaDirectInboxWrapper PendingInbox { get; } = new InstaDirectInboxWrapper(Instagram.Instance, true);
        public InstaDirectInboxWrapper Inbox { get; } = new InstaDirectInboxWrapper(Instagram.Instance);
        public CurrentUser LoggedInUser { get; private set; }
        public ObservableCollection<InstaUser> NewMessageCandidates { get; } = new ObservableCollection<InstaUser>();
        public InstaDirectInboxThreadWrapper SelectedThread
        {
            get => _selectedThread;
            set
            {
                _selectedThread = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedThread)));
            }
        }

        public AndroidDevice Device => _instaApi?.Device;
        public bool IsUserAuthenticated => _instaApi.IsUserAuthenticated;
        public ReelsFeed ReelsFeed { get; } = new ReelsFeed();

        private ApiContainer()
        {
            _instaApi.SyncClient.MessageReceived += OnMessageSyncReceived;
            _instaApi.SyncClient.ActivityIndicatorChanged += OnActivityIndicatorChanged;
            _instaApi.SyncClient.UserPresenceChanged += OnUserPresenceChanged;
            _instaApi.SyncClient.FailedToStart += async (sender, exception) =>
            {
#if !DEBUG
                Crashes.TrackError(exception);
#endif
                await HandleException();
            };
            Inbox.FirstUpdated += async (seqId, snapshotAt) => await _instaApi.SyncClient.Start(seqId, snapshotAt).ConfigureAwait(false);
            PushClient.MessageReceived += (sender, args) =>
            {
                Debug.Write("Background notification: ");
                Debug.WriteLine(args.Json);
            };
        }

        public async Task OnLoggedIn()
        {
            if (!_instaApi.IsUserAuthenticated) throw new Exception("User is not logged in.");
            await UpdateLoggedInUser();
            GetUserPresence();
            PushClient.Start();
            await ReelsFeed.UpdateReelsFeed();
            ReelsFeed.StartReelsFeedUpdateLoop();
        }

        public void SetSelectedThreadNull()
        {
            SelectedThread = null;
        }

        public Task<Result<LoginResult>> Login(string username, string password) => _instaApi.LoginAsync(username, password);

        public Task<Result<LoginResult>> LoginWithFacebook(string fbAccessToken) =>
            _instaApi.LoginWithFacebookAsync(fbAccessToken);

        public void Logout()
        {
            _instaApi.Logout();
            _ = ImageCache.Instance.ClearAsync();
            // _settings.Values.Clear();
        }

        private async Task UpdateLoggedInUser()
        {
            var loggedInUser = await _instaApi.GetCurrentUserAsync();
            LoggedInUser = loggedInUser.Value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoggedInUser)));
        }

        private void OnActivityIndicatorChanged(object sender, PubsubEventArgs data)
        {
            try
            {
                var indicatorData = data.Data[0];
                var segments = indicatorData.Path.Trim('/').Split('/');
                var threadId = segments[2];
                if (string.IsNullOrEmpty(threadId)) return;
                var thread = Inbox.Threads.SingleOrDefault(wrapper => wrapper.ThreadId == threadId);
                if (thread == null) return;
                if (indicatorData.Indicator.ActivityStatus == 1)
                    thread.PingTypingIndicator(indicatorData.Indicator.TimeToLive);
                else
                    thread.PingTypingIndicator(0);
            }
            catch (Exception e)
            {
#if !DEBUG
                Crashes.TrackError(e);
#endif
                Debug.WriteLine(e);
            }
        }

        private async void OnMessageSyncReceived(object sender, List<MessageSyncEventArgs> data)
        {
            try
            {
                var updateInbox = false;
                foreach (var syncEvent in data)
                {
                    var itemData = syncEvent.Data[0];
                    if (syncEvent.SeqId > Inbox.SeqId)
                    {
                        Inbox.SeqId = syncEvent.SeqId;
                        Inbox.SnapshotAt = itemData.Item.Timestamp;
                    }
                    var segments = itemData.Path.Trim('/').Split('/');
                    var threadId = segments[2];
                    if (string.IsNullOrEmpty(threadId)) continue;
                    var thread = Inbox.Threads.SingleOrDefault(wrapper => wrapper.ThreadId == threadId);
                    if (thread == null)
                    {
                        if (!updateInbox) updateInbox = itemData.Op == "add";
                        continue;
                    }

                    switch (itemData.Op)
                    {
                        case "add":
                        {
                            var item = itemData.Item;
                            if (item.ItemType == DirectItemType.Placeholder)
                            {
                                var result = await _instaApi.GetItemsInDirectThreadAsync(threadId, itemData.Item.ItemId);
                                if (result.IsSucceeded && result.Value.Items.Count > 0) 
                                    item = result.Value.Items[0];
                            }
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                () => thread.AddItem(item));
                            break;
                        }
                        case "replace":
                        {
                            if (itemData.Path.Contains("has_seen", StringComparison.Ordinal) && long.TryParse(segments[4], out var userId))
                            {
                                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                    () => thread.UpdateLastSeenAt(userId, itemData.Item.Timestamp, itemData.Item.ItemId));
                                continue;
                            }
                            var item = thread.ObservableItems.SingleOrDefault(x => x.ItemId == itemData.Item.ItemId);
                            if (item == null) continue;

                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                            {
                                if (itemData.Item.Reactions == null)
                                {
                                    item.Reactions.Clear();
                                }
                                else
                                {
                                    item.Reactions?.Update(new InstaDirectReactionsWrapper(itemData.Item.Reactions, thread.ViewerId),
                                        thread.Users);
                                }
                            });
                            break;
                        }
                    }
                }
                if (updateInbox)
                {
                    await Inbox.UpdateInbox();   
                }
            }
            catch (Exception e)
            {
#if !DEBUG
                Crashes.TrackError(e);
#endif
                Debug.WriteLine(e);
                if (DateTimeOffset.Now - _lastUpdated > TimeSpan.FromSeconds(0.5))
                    UpdateInboxAndSelectedThread();
            }
            Debug.WriteLine("Sync(s) received.");
        }

        public async Task UpdateSelectedThread()
        {
            if (SelectedThread == null)
                return;
            try
            {
                var result = await _instaApi.GetThreadAsync(SelectedThread.ThreadId, PaginationParameters.MaxPagesToLoad(1));
                if (result.IsSucceeded)
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () => { SelectedThread.Update(result.Value); });
            }
            catch (Exception e)
            {
#if !DEBUG
                Crashes.TrackError(e);
#endif
            }
        }

        public async void UpdateInboxAndSelectedThread()
        {
            _lastUpdated = DateTime.Now;
            await Inbox.UpdateInbox();
            if (SelectedThread == null) return;
            if (Inbox.Threads.Contains(SelectedThread))
            {
                await UpdateSelectedThread();
                MarkLatestItemSeen(SelectedThread);
            }
            else
            {
                var preferSelectedThread = Inbox.Threads.SingleOrDefault(x => x.ThreadId == SelectedThread.ThreadId);
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

        public async void Search(string query, Action<List<InstaDirectInboxThreadWrapper>> updateAction)
        {
            if (query.Length > 50) return;
            if (!await SearchReady()) return;

            var result = await _instaApi.GetRankedRecipientsByUsernameAsync(query);
            if (!result.IsSucceeded) return;
            var recipients = result.Value;
            var threadsFromUser = recipients.Users.Select(x => new InstaDirectInboxThreadWrapper(x, _instaApi)).ToList();
            var threadsFromRankedThread = recipients.Threads.Select(x => new InstaDirectInboxThreadWrapper(x, _instaApi)).ToList();
            var list = new List<InstaDirectInboxThreadWrapper>(threadsFromRankedThread.Count + threadsFromUser.Count);
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

        public async void SearchWithoutThreads(string query, Action<List<InstaUser>> updateAction)
        {
            if (query.Length > 50) return;
            if (!await SearchReady()) return;

            var result = await _instaApi.GetRankedRecipientsByUsernameAsync(query, false);
            if (!result.IsSucceeded) return;
            var recipients = result.Value.Users;
            if (recipients?.Count > 0)
                updateAction?.Invoke(recipients);
        }

        /// <summary>
        /// User ids will be fetched from NewMessageCandidates
        /// </summary>
        /// <returns></returns>
        public async Task CreateThread()
        {
            if (NewMessageCandidates.Count == 0 || NewMessageCandidates.Count > 32) return;
            var userIds = NewMessageCandidates.Select(x => x.Pk);
            var result = await _instaApi.CreateGroupThreadAsync(userIds);
            if (!result.IsSucceeded) return;
            var thread = result.Value;
            var existingThread = Inbox.Threads.SingleOrDefault(x => x.ThreadId == thread.ThreadId);
            SelectedThread = existingThread ?? new InstaDirectInboxThreadWrapper(thread, _instaApi);
        }

        public async void MakeProperInboxThread(InstaDirectInboxThreadWrapper placeholderThread)
        {
            InstaDirectInboxThreadWrapper thread;
            if (string.IsNullOrEmpty(placeholderThread.ThreadId))
            {
                var userIds = placeholderThread.Users.Select(x => x.Pk);
                var result = await _instaApi.GetThreadByParticipantsAsync(userIds);
                if (!result.IsSucceeded) return;
                thread = result.Value != null && result.Value.Users.Count > 0 ? 
                    new InstaDirectInboxThreadWrapper(result.Value, _instaApi) : new InstaDirectInboxThreadWrapper(placeholderThread.Users?[0], _instaApi);
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

        public async void MarkLatestItemSeen(InstaDirectInboxThreadWrapper thread)
        {
            try
            {
                if (thread == null || string.IsNullOrEmpty(thread.ThreadId) || thread.LastSeenAt == null) return;
                if (thread.LastSeenAt.TryGetValue(thread.ViewerId, out var lastSeen))
                {
                    if (string.IsNullOrEmpty(thread.LastPermanentItem?.ItemId) || 
                        lastSeen.ItemId == thread.LastPermanentItem.ItemId ||
                        thread.LastPermanentItem.FromMe) return;
                    await _instaApi.MarkItemSeenAsync(thread.ThreadId, thread.LastPermanentItem.ItemId).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
#if !DEBUG
                Crashes.TrackError(e);
#endif
            }
        }

        private async void GetUserPresence()
        {
            try
            {
                var presenceResult = await _instaApi.GetPresence();
                if (!presenceResult.IsSucceeded) return;
                foreach (var userPresenceValue in presenceResult.Value.UserPresence)
                {
                    UserPresenceDictionary[userPresenceValue.Key] = userPresenceValue.Value;
                }
            }
            catch (Exception e)
            {
#if !DEBUG
                Crashes.TrackError(e);
#endif
            }
        }

        private async void OnUserPresenceChanged(object sender, UserPresenceEventArgs e)
        {
            UserPresenceDictionary[e.UserId] = e;
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserPresenceDictionary)));
            });
        }

        public static IAsyncAction HandleException(string message = null, Exception e = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "An unexpected error has occured. Indirect doesn't know how to proceed next and may crash. " +
                          "If this happens frequently, please submit an issue on Indirect's Github page.\n\n" +
                          "https://github.com/huynhsontung/Indirect";
            }

            return CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
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
        }
    }
}