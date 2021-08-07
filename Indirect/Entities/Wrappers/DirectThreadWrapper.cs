using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Indirect.Utilities;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.User;
using InstagramAPI.Utils;
using Microsoft.Toolkit.Collections;
using InstagramAPI.Classes.Core;

namespace Indirect.Entities.Wrappers
{
    /// Wrapper of <see cref="DirectThread"/> with Observable lists
    class DirectThreadWrapper : DependencyObject, IIncrementalSource<DirectItemWrapper>, IEquatable<DirectThreadWrapper>
    {
        public static readonly DependencyProperty LastPermanentItemProperty = DependencyProperty.Register(
            nameof(LastPermanentItem),
            typeof(DirectItemWrapper),
            typeof(DirectThreadWrapper),
            new PropertyMetadata(null, OnLastPermanentItemPropertyChanged));

        public static readonly DependencyProperty IsSomeoneTypingProperty = DependencyProperty.Register(
            nameof(IsSomeoneTyping),
            typeof(bool),
            typeof(DirectThreadWrapper),
            new PropertyMetadata(false));

        public static readonly DependencyProperty DraftMessageProperty = DependencyProperty.Register(
            nameof(DraftMessage),
            typeof(string),
            typeof(DirectThreadWrapper),
            new PropertyMetadata(null));

        public static readonly DependencyProperty QuickReplyEmojiProperty = DependencyProperty.Register(
            nameof(QuickReplyEmoji),
            typeof(string),
            typeof(DirectThreadWrapper),
            new PropertyMetadata(null));

        public static readonly DependencyProperty ReplyingItemProperty = DependencyProperty.Register(
            nameof(ReplyingItem),
            typeof(DirectItemWrapper),
            typeof(DirectThreadWrapper),
            new PropertyMetadata(null));

        public static readonly DependencyProperty HasUnreadMessageProperty = DependencyProperty.Register(
            nameof(HasUnreadMessage),
            typeof(bool),
            typeof(DirectThreadWrapper),
            new PropertyMetadata(false));

        public DirectItemWrapper LastPermanentItem
        {
            get => (DirectItemWrapper) GetValue(LastPermanentItemProperty);
            private set => SetValue(LastPermanentItemProperty, value);
        }

        public bool IsSomeoneTyping
        {
            get => (bool) GetValue(IsSomeoneTypingProperty);
            private set => SetValue(IsSomeoneTypingProperty, value);
        }

        public string DraftMessage
        {
            get => (string) GetValue(DraftMessageProperty);
            set => SetValue(DraftMessageProperty, value);
        }

        public string QuickReplyEmoji
        {
            get => (string) GetValue(QuickReplyEmojiProperty);
            set => SetValue(QuickReplyEmojiProperty, value);
        }

        public DirectItemWrapper ReplyingItem
        {
            get => (DirectItemWrapper) GetValue(ReplyingItemProperty);
            set => SetValue(ReplyingItemProperty, value);
        }

        public bool HasUnreadMessage
        {
            get => (bool) GetValue(HasUnreadMessageProperty);
            set => SetValue(HasUnreadMessageProperty, value);
        }

        public DirectThread Source { get; private set; }

        public Dictionary<long,UserInfo> DetailedUserInfoDictionary { get; }
        public bool IsContactPanel { get; set; }
        public ReversedIncrementalLoadingCollection<DirectThreadWrapper, DirectItemWrapper> ObservableItems { get; }
        public BaseUser Viewer => _viewModel.LoggedInUser;
        public string ThreadId => Source.ThreadId;
        public ObservableCollection<BaseUser> Users { get; }

        private readonly MainViewModel _viewModel;
        private readonly DispatcherQueue _dispatcherQueue;

        /// <summary>
        /// Only use this constructor to make empty placeholder thread.
        /// </summary>
        /// <param name="viewModel"></param>
        /// <param name="user"></param>
        public DirectThreadWrapper(BaseUser user, MainViewModel viewModel) : this(viewModel, null)
        {
            Users[0] = user;
            Source.Title = user.Username;
        }

        public DirectThreadWrapper(MainViewModel viewModel, DirectThread source)
        {
            _viewModel = viewModel;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            Users = new ObservableCollection<BaseUser>();
            ObservableItems = new ReversedIncrementalLoadingCollection<DirectThreadWrapper, DirectItemWrapper>(this);
            DetailedUserInfoDictionary = new Dictionary<long, UserInfo>();

            if (source != null)
            {
                Source = source;
                foreach (var user in source.Users)
                {
                    Users.Add(user);
                }

                UpdateItemList(DecorateItems(source.Items));
                LastPermanentItem = ObservableItems.LastOrDefault();
                source.Items = null;
            }
            else
            {
                Source = new DirectThread();
            }

            if (Users.Count == 0)
            {
                Users.Add(new BaseUser());
            }

            if (string.IsNullOrEmpty(Source.Title) && source is RankedRecipientThread rankedThread)
            {
                Source.Title = rankedThread.ThreadTitle;
            }

            QuickReplyEmoji =
                !string.IsNullOrEmpty(ThreadId) &&
                viewModel.Settings.TryGetForThread(ThreadId, nameof(QuickReplyEmoji), out string emoji)
                    ? emoji
                    : "❤";
            ObservableItems.CollectionChanged += DecorateOnItemDeleted;
            ObservableItems.CollectionChanged += HideTypingIndicatorOnItemReceived;
            RegisterPropertyChangedCallback(QuickReplyEmojiProperty, OnQuickReplyEmojiChanged);
        }

        private void OnQuickReplyEmojiChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (!string.IsNullOrEmpty(QuickReplyEmoji))
            {
                _viewModel.Settings.SetForThread(ThreadId, nameof(QuickReplyEmoji), QuickReplyEmoji);
            }
        }

        public async Task<DirectThread> CloneThread()
        {
            var result = await _viewModel.InstaApi.GetThreadAsync(Source.ThreadId, _viewModel.Inbox.SeqId, PaginationParameters.MaxPagesToLoad(1));
            return result.IsSucceeded ? result.Value : null;
        }

        public void AddItems(List<DirectItem> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            var decoratedItems = DecorateItems(items);
            _dispatcherQueue.TryEnqueue(() =>
            {
                UpdateItemList(decoratedItems);
                // Assuming order of item is maintained. Last item after update should be the latest.
                var latestItem = ObservableItems.Last();
                var source = Source;
                if (source.LastPermanentItem == null ||
                    latestItem.Source.Timestamp > source.LastPermanentItem.Timestamp)
                {
                    // This does not update thread data like users in the thread or is thread muted or not
                    source.LastPermanentItem = latestItem.Source;
                    source.LastActivity = latestItem.Source.Timestamp;
                    source.NewestCursor = latestItem.Source.ItemId;
                    if (!latestItem.FromMe)
                    {
                        source.LastNonSenderItemAt = latestItem.Source.Timestamp;
                    }

                    Source = source;
                }

                LastPermanentItem = latestItem;
            });
        }

        public void AddItem(DirectItem item) => AddItems(new List<DirectItem> {item});

        public void RemoveItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            _dispatcherQueue.TryEnqueue(() =>
            {
                lock (ObservableItems)
                {
                    for (int i = ObservableItems.Count - 1; i >= 0; i--)
                    {
                        if (ObservableItems[i].Source.ItemId == itemId)
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
        public void Update(DirectThread source, bool fromInbox = false)
        {
            UpdateExcludeItemList(source);
            // Items from GetInbox request will interfere with GetPagedItemsAsync
            if (fromInbox)
            {
                return;
            }

            var decoratedItems = DecorateItems(source.Items);
            source.Items = null;
            _dispatcherQueue.TryEnqueue(() =>
            {
                UpdateItemList(decoratedItems);
                LastPermanentItem = ObservableItems.LastOrDefault();
            });
        }

        private static void OnLastPermanentItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thread = (DirectThreadWrapper)d;
            var sourceThread = thread.Source;
            if (sourceThread.LastSeenAt != null && sourceThread.LastSeenAt.TryGetValue(sourceThread.ViewerId, out var viewerLastSeen))
            {
                thread.HasUnreadMessage = sourceThread.LastNonSenderItemAt > viewerLastSeen.Timestamp &&
                                          sourceThread.LastActivity == sourceThread.LastNonSenderItemAt;
                return;
            }

            thread.HasUnreadMessage = false;
        }

        private void UpdateExcludeItemList(DirectThread target)
        {
            if (target == null)
            {
                return;
            }

            var source = Source;
            if (target.LastPermanentItem?.Timestamp < source.LastPermanentItem?.Timestamp)
            {
                target.LastPermanentItem = source.LastPermanentItem;
            }

            if (!string.IsNullOrEmpty(source.OldestCursor) &&
                string.Compare(source.OldestCursor, target.OldestCursor, StringComparison.Ordinal) < 0)
            {
                target.OldestCursor = source.OldestCursor;
                target.HasOlder = source.HasOlder;
            }

            if (!string.IsNullOrEmpty(source.NewestCursor) &&
                string.Compare(source.NewestCursor, target.NewestCursor, StringComparison.Ordinal) > 0)
            {
                target.NewestCursor = source.NewestCursor;
                // This implementation never has HasNewer = true
            }

            Source = target;
            UpdateUserList(target.Users);
        }

        private void UpdateItemList(ICollection<DirectItemWrapper> source)
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
                    for (var i = ObservableItems.Count - 1; i >= 0; i--)
                    {
                        if (ObservableItems[i].Equals(item))
                        {
                            ObservableItems.RemoveAt(i);
                            break;
                        }
                    }

                    for (var i = ObservableItems.Count - 1; i >= 0; i--)
                    {
                        if (item.Source.Timestamp > ObservableItems[i].Source.Timestamp)
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

                Source.Items = ObservableItems.Select(x => x.Source).ToList();
            }
        }

        private void UpdateUserList(List<UserWithFriendship> users)
        {
            if (users == null || users.Count == 0) return;
            _dispatcherQueue.TryEnqueue(() =>
            {
                lock (Users)
                {
                    var copyUsers = Users.ToList();
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

        public async Task<IEnumerable<DirectItemWrapper>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
        {
            // Without ThreadId we cant fetch thread items.
            var source = Source;
            if (string.IsNullOrEmpty(source.ThreadId) || !(source.HasOlder ?? true))
            {
                return new List<DirectItemWrapper>(0);
            }
            
            var pagesToLoad = pageSize / 20;
            if (pagesToLoad < 1) pagesToLoad = 1;
            var pagination = PaginationParameters.MaxPagesToLoad(pagesToLoad);
            pagination.StartFromMaxId(source.OldestCursor);
            
            var result = await _viewModel.InstaApi.GetThreadAsync(source.ThreadId, _viewModel.Inbox.SeqId, pagination);
            if (result.Status != ResultStatus.Succeeded || result.Value.Items == null || result.Value.Items.Count == 0)
            {
                return new List<DirectItemWrapper>(0);
            }
            
            UpdateExcludeItemList(result.Value);
            var wrappedItems = DecorateItems(result.Value.Items);
            return wrappedItems;
        }

        private void DecorateOnItemDeleted(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
                DecorateExistingItems();
        }

        private void DecorateExistingItems()
        {
            for (int i = ObservableItems.Count - 1; i >= 1; i--)
            {
                var showTimestamp = !IsCloseEnough(ObservableItems[i].Source.Timestamp, ObservableItems[i - 1].Source.Timestamp);
                var showName = ObservableItems[i].Source.UserId != ObservableItems[i - 1].Source.UserId &&
                               !ObservableItems[i].FromMe && Users.Count > 1;
                if (ObservableItems[i].ShowTimestampHeader != showTimestamp ||
                    ObservableItems[i].ShowNameHeader != showName)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        ObservableItems[i].ShowTimestampHeader = showTimestamp;
                        ObservableItems[i].ShowNameHeader = showName;
                    });
                }
            }
        }

        // Decide whether item should show timestamp header, name header etc...
        private List<DirectItemWrapper> DecorateItems(ICollection<DirectItem> items)
        {
            if (items == null || items.Count == 0) return new List<DirectItemWrapper>(0);
            var wrappedItems = items.Where(x => x != null)
                .Select(x => new DirectItemWrapper(_viewModel, x, this))
                .ToList();
            var lastItem = ObservableItems.FirstOrDefault();
            var itemList = wrappedItems.ToList();
            var refItem = itemList.Last();
            if (lastItem != null)
            {
                if (refItem.Source.Timestamp <= lastItem.Source.Timestamp)
                {
                    lastItem.ShowTimestampHeader = !IsCloseEnough(lastItem.Source.Timestamp, refItem.Source.Timestamp);
                    lastItem.ShowNameHeader = lastItem.Source.UserId != refItem.Source.UserId && !lastItem.FromMe && Users.Count > 1;
                }
                else
                {
                    // New item to be added to the top
                    refItem = itemList.First();
                    var latestItem = ObservableItems.Last();
                    refItem.ShowTimestampHeader = !IsCloseEnough(latestItem.Source.Timestamp, refItem.Source.Timestamp);
                    refItem.ShowNameHeader = latestItem.Source.UserId != refItem.Source.UserId && !refItem.FromMe && Users.Count > 1;
                }
            }

            for (int i = itemList.Count - 1; i >= 1; i--)
            {
                itemList[i].ShowTimestampHeader = !IsCloseEnough(itemList[i].Source.Timestamp, itemList[i - 1].Source.Timestamp);
                itemList[i].ShowNameHeader = itemList[i].Source.UserId != itemList[i - 1].Source.UserId && !itemList[i].FromMe && Users.Count > 1;
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
            await Dispatcher.QuickRunAsync(async () =>
            {
                try
                {
                    var source = Source;
                    if (string.IsNullOrEmpty(source.ThreadId) || source.LastSeenAt == null) return;
                    if (source.LastSeenAt.TryGetValue(source.ViewerId, out var lastSeen))
                    {
                        if (string.IsNullOrEmpty(source.LastPermanentItem?.ItemId) ||
                            lastSeen.ItemId == source.LastPermanentItem.ItemId ||
                            LastPermanentItem.FromMe)
                        {
                            return;
                        }

                        HasUnreadMessage = false;
                        await _viewModel.InstaApi.MarkItemSeenAsync(source.ThreadId, source.LastPermanentItem.ItemId)
                            .ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    DebugLogger.LogException(e);
                }
            });
        }

        public void UpdateLastSeenAt(long userId, DateTimeOffset timestamp, string itemId)
        {
            if (userId == default || timestamp == default || itemId == default)
            {
                return;
            }

            var source = Source;
            var lastSeenAt = new Dictionary<long, LastSeen>(source.LastSeenAt);
            if (lastSeenAt.TryGetValue(userId, out var lastSeen))
            {
                lastSeen.Timestamp = timestamp;
                lastSeen.ItemId = itemId;
            }
            else
            {
                lastSeenAt[userId] = new LastSeen
                {
                    ItemId = itemId,
                    Timestamp = timestamp
                };
            }

            source.LastSeenAt = lastSeenAt;
            Source = source;
        }


        /// <summary>
        /// Set IsSomeoneTyping to true for a period of time
        /// </summary>
        /// <param name="ttl">Amount of time to keep IsSomeoneTyping true. If this is 0 immediately set to false</param>
        public void PingTypingIndicator(int ttl)
        {
            if (!IsSomeoneTyping && ttl == 0) return;
            _dispatcherQueue.TryEnqueue(async () =>
            {
                if (ttl > 0)
                {
                    if (!IsSomeoneTyping) IsSomeoneTyping = true;
                    if (await Debouncer.Delay("PingTypingIndicator", ttl).ConfigureAwait(true))
                    {
                        IsSomeoneTyping = false;
                    }
                }
                else
                {
                    IsSomeoneTyping = false;
                }
            });
        }

        private void HideTypingIndicatorOnItemReceived(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null || e.NewItems.Count == 0) return;    // Item removed, not received
            if (e.NewItems.Count == 1 && !((DirectItemWrapper)e.NewItems[0]).FromMe)
            {
                PingTypingIndicator(0);
            }
        }

        public bool Equals(DirectThreadWrapper other)
        {
            return string.IsNullOrEmpty(Source.ThreadId)
                ? Source.Title == other?.Source?.Title
                : Source.ThreadId == other?.Source?.ThreadId;
        }
    }
}
