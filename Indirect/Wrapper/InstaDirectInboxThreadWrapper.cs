using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Indirect.Utilities;
using InstagramAPI;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.User;
using InstagramAPI.Utils;
using Microsoft.Toolkit.Collections;

namespace Indirect.Wrapper
{
    /// Wrapper of <see cref="DirectThread"/> with Observable lists
    class InstaDirectInboxThreadWrapper : DirectThread, INotifyPropertyChanged, IIncrementalSource<InstaDirectInboxItemWrapper>, IDisposable
    {
        private readonly Instagram _instaApi;
        private CancellationTokenSource _typingCancellationTokenSource;
        public CoreDispatcher Dispatcher { get; private set; } = CoreApplication.MainView.CoreWindow.Dispatcher;
        public bool IsSecondaryView { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isSomeoneTyping;
        public bool IsSomeoneTyping
        {
            get => _isSomeoneTyping;
            private set
            {
                _isSomeoneTyping = value;
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSomeoneTyping)));
                });
            }
        }

        private bool _showSeenIndicator;
        public bool ShowSeenIndicator
        {
            get => _showSeenIndicator;
            private set
            {
                _showSeenIndicator = value;
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowSeenIndicator)));
                });
            }
        }

        private string _draftMessage;
        public string DraftMessage
        {
            get => _draftMessage;
            set
            {
                _draftMessage = value;
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DraftMessage)));
                });
            }
        }

        public Dictionary<long,UserInfo> DetailedUserInfoDictionary = new Dictionary<long, UserInfo>();

        public ReversedIncrementalLoadingCollection<InstaDirectInboxThreadWrapper, InstaDirectInboxItemWrapper> ObservableItems { get; set; }
        public new ObservableCollection<BaseUser> Users { get; } = new ObservableCollection<BaseUser>();
        public List<string> UsersSeenLatestMessage { get; } = new List<string>(); // If Users.Count == 1 then this is always empty

        private InstaDirectInboxThreadWrapper(Instagram api)
        {
            ObservableItems = new ReversedIncrementalLoadingCollection<InstaDirectInboxThreadWrapper, InstaDirectInboxItemWrapper>(this);
            _instaApi = api;
            PropertyChanged += SeenCheckProperty;
            ObservableItems.CollectionChanged += DecorateOnItemDeleted;
            ObservableItems.CollectionChanged += SeenCheckCollection;
            ObservableItems.CollectionChanged += HideTypingIndicatorOnItemReceived;
        }

        /// <summary>
        /// Only use this constructor to make empty placeholder thread.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="api"></param>
        public InstaDirectInboxThreadWrapper(BaseUser user, Instagram api) : this(api)
        {
            Users.Add(user);
            Title = user.Username;
            if (Users.Count == 0) Users.Add(new BaseUser());
        }

        public InstaDirectInboxThreadWrapper(RankedRecipientThread rankedThread, Instagram api) : this(api)
        {
            PropertyCopier<RankedRecipientThread, InstaDirectInboxThreadWrapper>.Copy(rankedThread, this);
            Title = rankedThread.ThreadTitle;
            ThreadType = DirectThreadType.Private;
            foreach (var user in rankedThread.Users)
            {
                Users.Add(user);
            }
            if (Users.Count == 0) Users.Add(new BaseUser());
        }

        public InstaDirectInboxThreadWrapper(DirectThread source, Instagram api) : this(api)
        {
            PropertyCopier<DirectThread, InstaDirectInboxThreadWrapper>.Copy(source, this);

            foreach (var instaUserShortFriendship in source.Users)
            {
                Users.Add(instaUserShortFriendship);
            }
            if (Users.Count == 0) Users.Add(new BaseUser());
            UpdateItemList(DecorateItems(source.Items));
        }

        public async Task<InstaDirectInboxThreadWrapper> CloneThreadForSecondaryView(CoreDispatcher dispatcher)
        {
            var result = await _instaApi.GetThreadAsync(ThreadId, PaginationParameters.MaxPagesToLoad(1));
            if (!result.IsSucceeded) return null;
            var clone = new InstaDirectInboxThreadWrapper(result.Value, _instaApi)
                {Dispatcher = dispatcher, IsSecondaryView = true};
            return clone;
        }

        public async Task AddItems(List<DirectItem> items)
        {
            if (items.Count == 0) return;
            await UpdateItemListAsync(DecorateItems(items));

            var latestItem = ObservableItems.Last();    // Assuming order of item is maintained. Last item after update should be the latest.
            if (latestItem.Timestamp > LastPermanentItem.Timestamp)
            {
                // This does not update thread data like users in the thread or is thread muted or not
                LastPermanentItem = latestItem;
                LastActivity = latestItem.Timestamp;
                NewestCursor = latestItem.ItemId;
                if (!latestItem.FromMe)
                {
                    LastNonSenderItemAt = latestItem.Timestamp;
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastPermanentItem)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastActivity)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewestCursor)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasUnreadMessage))); // LastNonSenderItemAt
                });
            }
        }

        public Task AddItem(DirectItem item) => AddItems(new List<DirectItem> {item});

        public async Task RemoveItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (ObservableItems)
                {
                    for (int i = ObservableItems.Count - 1; i >= 0; i--)
                    {
                        if (ObservableItems[i].ItemId == itemId)
                        {
                            ObservableItems.RemoveAt(i);
                            break;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Update everything in a thread. Use it if you have all thread metadata.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="fromInbox"></param>
        public async void Update(DirectThread source, bool fromInbox = false)
        {
            await UpdateExcludeItemList(source);
            if (fromInbox) return;  // Items from GetInbox request will interfere with GetPagedItemsAsync
            await UpdateItemListAsync(DecorateItems(source.Items));
        }

        private async Task UpdateExcludeItemList(DirectThread source)
        {
            Canonical = source.Canonical;
            //HasNewer = source.HasNewer;
            //HasOlder = source.HasOlder;
            IsSpam = source.IsSpam;
            Muted = source.Muted;
            Named = source.Named;
            Pending = source.Pending;
            ViewerId = source.ViewerId;
            LastActivity = source.LastActivity;
            ThreadId = source.ThreadId;
            IsGroup = source.IsGroup;
            IsPin = source.IsPin;
            ValuedRequest = source.ValuedRequest;
            VCMuted = source.VCMuted;
            ReshareReceiveCount = source.ReshareReceiveCount;
            ReshareSendCount = source.ReshareSendCount;
            ExpiringMediaReceiveCount = source.ExpiringMediaReceiveCount;
            ExpiringMediaSendCount = source.ExpiringMediaSendCount;
            ThreadType = source.ThreadType;
            Title = source.Title;
            MentionsMuted = source.MentionsMuted;

            Inviter = source.Inviter;
            LastPermanentItem = source.LastPermanentItem?.Timestamp > LastPermanentItem?.Timestamp ?
                source.LastPermanentItem : LastPermanentItem;
            LeftUsers = source.LeftUsers;
            LastSeenAt = source.LastSeenAt;

            if (string.IsNullOrEmpty(OldestCursor) || 
                string.Compare(OldestCursor, source.OldestCursor, StringComparison.Ordinal) > 0)
            {
                OldestCursor = source.OldestCursor;
                HasOlder = source.HasOlder;
            }

            if (string.IsNullOrEmpty(NewestCursor) || 
                string.Compare(NewestCursor, source.NewestCursor, StringComparison.Ordinal) < 0)
            {
                NewestCursor = source.NewestCursor;
                // This implementation never has HasNewer = true
            }

            await UpdateUserList(source.Users);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            });
        }

        private IAsyncAction UpdateItemListAsync(ICollection<InstaDirectInboxItemWrapper> source)
        {
            return Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateItemList(source);
            });
        }

        private void UpdateItemList(ICollection<InstaDirectInboxItemWrapper> source)
        {
            if (source == null || source.Count == 0) return;

            lock (ObservableItems)
            {
                if (ObservableItems.Count == 0)
                {
                    foreach (var item in source)
                        ObservableItems.Add(item);
                    return;
                }

                foreach (var item in source)
                {
                    var existingItem = ObservableItems.LastOrDefault(x => x.Equals(item));
                    var existed = existingItem != null;

                    if (existed)
                    {
                        if (item.Reactions != null)
                        {
                            existingItem.Reactions.Update(item.Reactions, Users);
                        }
                        continue;
                    }
                    for (var i = ObservableItems.Count - 1; i >= 0; i--)
                    {
                        if (item.Timestamp > ObservableItems[i].Timestamp)
                        {
                            ObservableItems.Insert(i + 1, item);
                            break;
                        }

                        if (i == 0)
                        {
                            ObservableItems.Insert(0, item);
                        }
                    }
                }
            }
        }

        private async Task UpdateUserList(List<UserWithFriendship> users)
        {
            if (users == null || users.Count == 0) return;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (Users)
                {
                    var copyUsers =
                        Users.ToList(); // attempt to troubleshoot InvalidOperationException: Collection was modified
                    var toBeAdded = users.Where(p2 => copyUsers.All(p1 => !p1.Equals(p2)));
                    var toBeDeleted = copyUsers.Where(p1 => users.All(p2 => !p1.Equals(p2)));
                    foreach (var user in toBeAdded)
                    {
                        Users.Add(user);
                    }

                    foreach (var user in toBeDeleted)
                    {
                        Users.Remove(user);
                    }
                }
            });
        }

        public async Task<IEnumerable<InstaDirectInboxItemWrapper>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
        {
            // Without ThreadId we cant fetch thread items.
            if (string.IsNullOrEmpty(ThreadId) || !(HasOlder ?? true)) return new List<InstaDirectInboxItemWrapper>(0);
            var pagesToLoad = pageSize / 20;
            if (pagesToLoad < 1) pagesToLoad = 1;
            var pagination = PaginationParameters.MaxPagesToLoad(pagesToLoad);
            pagination.StartFromMaxId(OldestCursor);
            var result = await _instaApi.GetThreadAsync(ThreadId, pagination);
            if (result.Status != ResultStatus.Succeeded || result.Value.Items == null || result.Value.Items.Count == 0) return new List<InstaDirectInboxItemWrapper>(0);
            await UpdateExcludeItemList(result.Value);
            var wrappedItems = DecorateItems(result.Value.Items);
            return wrappedItems;
        }

        private async void DecorateOnItemDeleted(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
                await DecorateExistingItems();
        }

        private async Task DecorateExistingItems()
        {
            for (int i = ObservableItems.Count - 1; i >= 1; i--)
            {
                var showTimestamp = !IsCloseEnough(ObservableItems[i].Timestamp, ObservableItems[i - 1].Timestamp);
                var showName = ObservableItems[i].UserId != ObservableItems[i - 1].UserId &&
                               !ObservableItems[i].FromMe && Users.Count > 1;
                if (ObservableItems[i].ShowTimestampHeader != showTimestamp ||
                    ObservableItems[i].ShowNameHeader != showName)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        ObservableItems[i].ShowTimestampHeader = showTimestamp;
                        ObservableItems[i].ShowNameHeader = showName;
                    });
                }
            }
        }

        // Decide whether item should show timestamp header, name header etc...
        private List<InstaDirectInboxItemWrapper> DecorateItems(IEnumerable<DirectItem> items)
        {
            var wrappedItems = items.Select(x => new InstaDirectInboxItemWrapper(x, this, _instaApi)).ToList();
            var lastItem = ObservableItems.FirstOrDefault();
            var itemList = wrappedItems.ToList();
            var refItem = itemList.Last();
            if (lastItem != null)
            {
                if (refItem.Timestamp <= lastItem.Timestamp)
                {
                    lastItem.ShowTimestampHeader = !IsCloseEnough(lastItem.Timestamp, refItem.Timestamp);
                    lastItem.ShowNameHeader = lastItem.UserId != refItem.UserId && !lastItem.FromMe && Users.Count > 1;
                }
                else
                {
                    // New item to be added to the top
                    refItem = itemList.First();
                    var latestItem = ObservableItems.Last();
                    refItem.ShowTimestampHeader = !IsCloseEnough(latestItem.Timestamp, refItem.Timestamp);
                    refItem.ShowNameHeader = latestItem.UserId != refItem.UserId && !refItem.FromMe && Users.Count > 1;
                }
            }

            for (int i = itemList.Count - 1; i >= 1; i--)
            {
                itemList[i].ShowTimestampHeader = !IsCloseEnough(itemList[i].Timestamp, itemList[i - 1].Timestamp);
                itemList[i].ShowNameHeader = itemList[i].UserId != itemList[i - 1].UserId && !itemList[i].FromMe && Users.Count > 1;
            }

            return wrappedItems;
        }

        private const int TimestampClosenessThreshold = 3; // hours
        private static bool IsCloseEnough(DateTimeOffset x, DateTimeOffset y)
        {
            return TimeSpan.FromHours(-TimestampClosenessThreshold) < x - y &&
                   x - y < TimeSpan.FromHours(TimestampClosenessThreshold);
        }

        public async Task MarkLatestItemSeen()
        {
            try
            {
                if (string.IsNullOrEmpty(ThreadId) || LastSeenAt == null) return;
                if (LastSeenAt.TryGetValue(ViewerId, out var lastSeen))
                {
                    if (string.IsNullOrEmpty(LastPermanentItem?.ItemId) ||
                        lastSeen.ItemId == LastPermanentItem.ItemId ||
                        LastPermanentItem.FromMe) return;
                    await _instaApi.MarkItemSeenAsync(ThreadId, LastPermanentItem.ItemId).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
            }
        }

        public async Task UpdateLastSeenAt(long userId, DateTimeOffset timestamp, string itemId)
        {
            if (userId == default || timestamp == default || itemId == default) return;
            if (LastSeenAt.TryGetValue(userId, out var lastSeen))
            {
                lastSeen.Timestamp = timestamp;
                lastSeen.ItemId = itemId;
            }
            else
            {
                LastSeenAt[userId] = new LastSeen
                {
                    ItemId = itemId,
                    Timestamp = timestamp
                };
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastSeenAt)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasUnreadMessage)));
            });
        }


        /// <summary>
        /// Set IsSomeoneTyping to true for a period of time
        /// </summary>
        /// <param name="ttl">Amount of time to keep IsSomeoneTyping true. If this is 0 immediately set to false</param>
        public async void PingTypingIndicator(int ttl)
        {
            if (!IsSomeoneTyping && ttl == 0) return;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                _typingCancellationTokenSource?.Cancel();
                _typingCancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _typingCancellationTokenSource.Token;
                if (ttl > 0)
                {
                    if (!IsSomeoneTyping) IsSomeoneTyping = true;
                    try
                    {
                        await Task.Delay(ttl, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }

                    IsSomeoneTyping = false;
                }
                else
                {
                    _typingCancellationTokenSource?.Cancel();
                    _typingCancellationTokenSource = null;
                    IsSomeoneTyping = false;
                }
            });
        }

        private void HideTypingIndicatorOnItemReceived(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null || e.NewItems.Count == 0) return;    // Item removed, not received
            if (e.NewItems.Count == 1 && !((InstaDirectInboxItemWrapper)e.NewItems[0]).FromMe)
            {
                PingTypingIndicator(0);
            }
        }

        private void SeenCheckProperty(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName != nameof(LastSeenAt) && !string.IsNullOrEmpty(args.PropertyName)) return;
            SeenCheckCollection(sender, null);
        }

        private async void SeenCheckCollection(object sender, NotifyCollectionChangedEventArgs e)
        {
            UsersSeenLatestMessage.Clear();

            // If there is no item then don't show seen indicator
            if (ObservableItems.Count == 0 && ShowSeenIndicator)
            {
                ShowSeenIndicator = false;
                return;
            }

            // If latest item is not from me then don't show seen indicator
            var latestItem = ObservableItems.Last();
            if (!latestItem.FromMe || LastSeenAt.Count == 0 && ShowSeenIndicator)
            {
                ShowSeenIndicator = false;
                return;
            }

            // When Users.Count == 1 don't use the list
            if (Users.Count == 1)
            {
                var value = LastSeenAt.Any(pair =>
                    pair.Key != ViewerId && pair.Value.Timestamp >= latestItem.Timestamp);
                if (value != ShowSeenIndicator) ShowSeenIndicator = value;
                return;
            }

            var seenUsers = LastSeenAt.Where(x => x.Key != ViewerId && x.Value.Timestamp >= latestItem.Timestamp)
                .Select(x => Users.SingleOrDefault(y => y.Pk == x.Key))
                .Where(x => x != null)
                .Select(x => x.Username);
            UsersSeenLatestMessage.AddRange(seenUsers);

            var showIndicator = UsersSeenLatestMessage.Count > 0;
            if (UsersSeenLatestMessage.Count == Users.Count)
            {
                UsersSeenLatestMessage.Clear();
                UsersSeenLatestMessage.Add("@everyone");
            }
            if (showIndicator != ShowSeenIndicator)
                ShowSeenIndicator = showIndicator;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UsersSeenLatestMessage)));
            });
        }

        public void Dispose()
        {
            _typingCancellationTokenSource?.Dispose();
        }
    }
}
