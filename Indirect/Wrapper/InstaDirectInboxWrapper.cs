using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using InstagramAPI;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Direct;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp;

namespace Indirect.Wrapper
{
    class InstaDirectInboxWrapper: Inbox, IIncrementalSource<InstaDirectInboxThreadWrapper>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<int, DateTimeOffset> FirstUpdated;    // callback to start SyncClient

        private int _pendingRequestCount;
        public int PendingRequestsCount
        {
            get => _pendingRequestCount;
            private set
            {
                _pendingRequestCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PendingRequestsCount)));
            }
        }

        public long SeqId { get; set; }
        public DateTimeOffset SnapshotAt { get; set; }
        public bool PendingInbox { get; }

        public new IncrementalLoadingCollection<InstaDirectInboxWrapper, InstaDirectInboxThreadWrapper> Threads { get; }

        private readonly Instagram _instaApi;
        private bool _firstTime = true;
        public InstaDirectInboxWrapper(Instagram api, bool pending = false)
        {
            _instaApi = api ?? throw new NullReferenceException();
            PendingInbox = pending;
            Threads =
                new IncrementalLoadingCollection<InstaDirectInboxWrapper, InstaDirectInboxThreadWrapper>(this);
        }

        private void UpdateExcludeThreads(InboxContainer source)
        {
            PendingRequestsCount = source.PendingRequestsCount;
            SeqId = source.SeqId;
            SnapshotAt = source.SnapshotAt;
            var inbox = source.Inbox;
            UnseenCount = inbox.UnseenCount;
            UnseenCountTs = inbox.UnseenCountTs;
            BlendedInboxEnabled = inbox.BlendedInboxEnabled;
            if (string.IsNullOrEmpty(OldestCursor) ||
                string.Compare(OldestCursor, inbox.OldestCursor, StringComparison.Ordinal) > 0)
            {
                OldestCursor = inbox.OldestCursor;
                HasOlder = inbox.HasOlder;
            }
        }

        public async Task UpdateInbox()
        {
            var result = await _instaApi.GetInboxAsync(PaginationParameters.MaxPagesToLoad(1));
            InboxContainer container;
            if (result.Status == ResultStatus.Succeeded)
                container = result.Value;
            else return;
            UpdateExcludeThreads(container);
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (var thread in container.Inbox.Threads)
                {
                    var existed = false;
                    foreach (var existingThread in Threads)
                    {
                        if (thread.ThreadId != existingThread.ThreadId) continue;
                        existingThread.Update(thread, true);
                        existed = true;
                        break;
                    }

                    if (!existed)
                    {
                        var wrappedThread = new InstaDirectInboxThreadWrapper(thread, _instaApi);
                        wrappedThread.PropertyChanged += OnThreadChanged;
                        Threads.Insert(0, wrappedThread);
                    }
                }

                SortInboxThread();
            });
        }

        public async Task<IEnumerable<InstaDirectInboxThreadWrapper>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
        {
            if (!_firstTime && !HasOlder) return Array.Empty<InstaDirectInboxThreadWrapper>();
            var pagesToLoad = pageSize / 20;
            if (pagesToLoad < 1) pagesToLoad = 1;
            var pagination = PaginationParameters.MaxPagesToLoad(pagesToLoad);
            pagination.StartFromMaxId(OldestCursor);
            var result = await _instaApi.GetInboxAsync(pagination, PendingInbox);
            InboxContainer container;
            if (result.Status == ResultStatus.Succeeded)
            {
                container = result.Value;
                if (_firstTime)
                {
                    _firstTime = false;
                    FirstUpdated?.Invoke(container.SeqId, container.SnapshotAt);
                }
            }
            else
            {
                return new List<InstaDirectInboxThreadWrapper>(0);
            }
            UpdateExcludeThreads(container);

            var wrappedThreadList = new List<InstaDirectInboxThreadWrapper>();
            foreach (var directThread in container.Inbox.Threads)
            {
                var wrappedThread = new InstaDirectInboxThreadWrapper(directThread, _instaApi);
                wrappedThread.PropertyChanged += OnThreadChanged;
                wrappedThreadList.Add(wrappedThread);
            }

            return wrappedThreadList;
        }

        private void SortInboxThread()
        {
            var sorted = Threads.OrderByDescending(x => x.LastActivity).ToList();
            bool satisfied = true;
            for (var i = 0; i < Threads.Count; i++)
            {
                var thread = Threads[i];
                var j = i;
                for (; j < sorted.Count; j++)
                {
                    if (!thread.Equals(sorted[j]) || i == j) continue;
                    satisfied = false;
                    break;
                }

                if (satisfied) continue;
                // Threads.Move(i,j);
                // ObservableCollection.Move call ObservableCollection implementation of RemoveItem which is cause to refresh all items
                if (ApiContainer.Instance.SelectedThread != Threads[i])
                {
                    var tmp = Threads[i];
                    Threads.RemoveAt(i);
                    Threads.Insert(j, tmp);
                    i--;
                    satisfied = true;
                }
                else
                {
                    // If Selected thread is Threads[i], RemoveAt(i) will deselect the thread
                    var tmp = Threads[j];
                    Threads.RemoveAt(j);
                    Threads.Insert(i, tmp);
                    i--;
                    satisfied = true;
                }
            }
        }

        private void OnThreadChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(InstaDirectInboxThreadWrapper.LastActivity) || string.IsNullOrEmpty(args.PropertyName))
            {
                SortInboxThread();
            }
        }

        public void FixThreadList()
        {
            // Somehow thread list got messed up and threads are not unique anymore
            var duplicates = Threads.GroupBy(x => x.ThreadId).Where(g => g.Count() > 1)
                .Select(y => y);
            foreach (var duplicateGroup in duplicates)
            {
                var duplicate = duplicateGroup.First();
                if (string.IsNullOrEmpty(duplicate.ThreadId)) continue;
                Threads.Remove(duplicate);
            }
        }
    }
}
