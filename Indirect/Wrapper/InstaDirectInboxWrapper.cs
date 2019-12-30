using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InstaSharper.API;
using InstaSharper.Classes;
using InstaSharper.Classes.Models.Direct;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp;

namespace Indirect.Wrapper
{
    class InstaDirectInboxWrapper: InstaDirectInbox, IIncrementalSource<InstaDirectInboxThreadWrapper>
    {
        public new IncrementalLoadingCollection<InstaDirectInboxWrapper, InstaDirectInboxThreadWrapper> Threads { get; }

        private readonly IInstaApi _instaApi;
        public InstaDirectInboxWrapper(IInstaApi api)
        {
            _instaApi = api ?? throw new NullReferenceException();
            Threads =
                new IncrementalLoadingCollection<InstaDirectInboxWrapper, InstaDirectInboxThreadWrapper>(this);
        }

        private void UpdateExcludeThreads(InstaDirectInbox source)
        {
            UnseenCount = source.UnseenCount;
            UnseenCountTs = source.UnseenCountTs;
            BlendedInboxEnabled = source.BlendedInboxEnabled;
            if (string.IsNullOrEmpty(OldestCursor) ||
                string.Compare(OldestCursor, source.OldestCursor, StringComparison.Ordinal) > 0)
            {
                OldestCursor = source.OldestCursor;
                HasOlder = source.HasOlder;
            }
        }

        public async Task UpdateInbox()
        {
            var result = await _instaApi.MessagingProcessor.GetInboxAsync(PaginationParameters.MaxPagesToLoad(1));
            InstaDirectInbox inbox;
            if (result.Succeeded)
                inbox = result.Value.Inbox;
            else return;
            UpdateExcludeThreads(inbox);
            foreach (var thread in inbox.Threads)
            {
                var existed = false;
                foreach (var existingThread in Threads)
                {
                    if (thread.ThreadId != existingThread.ThreadId) continue;
                    existingThread.Update(thread);
                    existed = true;
                    break;
                }

                if (!existed)
                {
                    Threads.Insert(0, new InstaDirectInboxThreadWrapper(thread, _instaApi));
                }
            }
            SortInboxThread();
        }

        public async Task<IEnumerable<InstaDirectInboxThreadWrapper>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
        {
            var pagesToLoad = pageSize / 20;
            if (pagesToLoad < 1) pagesToLoad = 1;
            var pagination = PaginationParameters.MaxPagesToLoad(pagesToLoad);
            pagination.StartFromMaxId(OldestCursor);
            var result = await _instaApi.MessagingProcessor.GetInboxAsync(pagination);
            InstaDirectInbox inbox;
            if (result.Succeeded)
                inbox = result.Value.Inbox;
            else return new List<InstaDirectInboxThreadWrapper>(0);
            UpdateExcludeThreads(inbox);
            return inbox.Threads.Select(x => new InstaDirectInboxThreadWrapper(x, _instaApi));
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
