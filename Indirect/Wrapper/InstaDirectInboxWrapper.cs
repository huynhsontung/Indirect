using System;
using System.Collections.Generic;
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
    class InstaDirectInboxWrapper: Inbox, IIncrementalSource<InstaDirectInboxThreadWrapper>
    {
        public event Action<int, DateTimeOffset> FirstUpdated;    // callback to start SyncClient

        public int PendingRequestsCount { get; set; }
        public int SeqId { get; set; }
        public DateTimeOffset SnapshotAt { get; set; }

        public new IncrementalLoadingCollection<InstaDirectInboxWrapper, InstaDirectInboxThreadWrapper> Threads { get; }

        private readonly Instagram _instaApi;
        private bool _firstTime = true;
        public InstaDirectInboxWrapper(Instagram api)
        {
            _instaApi = api ?? throw new NullReferenceException();
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
            var result = await _instaApi.GetInboxAsync(PaginationParameters.MaxPagesToLoad(1)).ConfigureAwait(false);
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
                    for (var i = 0; i < Threads.Count; i++)
                    {
                        // if (i > 60) break;
                        var existingThread = Threads[i];
                        if (thread.ThreadId != existingThread.ThreadId) continue;
                        existingThread.Update(thread, true);
                        existed = true;
                        break;
                    }

                    if (!existed)
                    {
                        Threads.Insert(0, new InstaDirectInboxThreadWrapper(thread, _instaApi));
                    }
                }

                SortInboxThread();
            });
        }

        public async Task<IEnumerable<InstaDirectInboxThreadWrapper>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
        {
            var pagesToLoad = pageSize / 20;
            if (pagesToLoad < 1) pagesToLoad = 1;
            var pagination = PaginationParameters.MaxPagesToLoad(pagesToLoad);
            pagination.StartFromMaxId(OldestCursor);
            var result = await _instaApi.GetInboxAsync(pagination).ConfigureAwait(false);
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
            else return new List<InstaDirectInboxThreadWrapper>(0);
            UpdateExcludeThreads(container);
            return container.Inbox.Threads.Select(x => new InstaDirectInboxThreadWrapper(x, _instaApi));
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
                var tmp = Threads[i];
                Threads.RemoveAt(i);
                Threads.Insert(j, tmp);
                i--;
                satisfied = true;
            }
        }
    }
}
