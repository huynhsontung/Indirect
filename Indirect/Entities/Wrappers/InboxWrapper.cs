using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Direct;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Indirect.Entities.Wrappers
{
    class InboxWrapper: Inbox, IIncrementalSource<DirectThreadWrapper>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<int, DateTimeOffset> FirstUpdated;    // callback to start SyncClient

        public int PendingRequestsCount { get; private set; }

        public long SeqId { get; set; }
        public DateTimeOffset SnapshotAt { get; set; }
        public bool PendingInbox { get; }

        public new IncrementalLoadingCollection<InboxWrapper, DirectThreadWrapper> Threads { get; }

        private readonly MainViewModel _viewModel;
        private bool _firstTime = true;
        public InboxWrapper(MainViewModel viewModel, bool pending = false)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            PendingInbox = pending;
            Threads =
                new IncrementalLoadingCollection<InboxWrapper, DirectThreadWrapper>(this);
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

            _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PendingRequestsCount))); });
        }

        public async Task UpdateInbox()
        {
            var result = await _viewModel.InstaApi.GetInboxAsync(PaginationParameters.MaxPagesToLoad(1));
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
                        var wrappedThread = new DirectThreadWrapper(_viewModel, thread);
                        wrappedThread.PropertyChanged += OnThreadChanged;
                        Threads.Insert(0, wrappedThread);
                    }
                }

                SortInboxThread();
            });
        }

        public async Task<IEnumerable<DirectThreadWrapper>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
        {
            if (!_firstTime && !HasOlder) return Array.Empty<DirectThreadWrapper>();
            var pagesToLoad = pageSize / 20;
            if (pagesToLoad < 1) pagesToLoad = 1;
            var pagination = PaginationParameters.MaxPagesToLoad(pagesToLoad);
            pagination.StartFromMaxId(OldestCursor);
            var result = await _viewModel.InstaApi.GetInboxAsync(pagination, PendingInbox);
            if (!result.IsSucceeded)
            {
                return new List<DirectThreadWrapper>(0);
            }
            var container = result.Value;
            if (_firstTime)
            {
                _firstTime = false;
                FirstUpdated?.Invoke(container.SeqId, container.SnapshotAt);
            }
            UpdateExcludeThreads(container);

            var wrappedThreadList = new List<DirectThreadWrapper>();
            foreach (var directThread in container.Inbox.Threads)
            {
                var wrappedThread = new DirectThreadWrapper(_viewModel, directThread);
                wrappedThread.PropertyChanged += OnThreadChanged;
                wrappedThreadList.Add(wrappedThread);
                _viewModel.ThreadInfoPersistentDictionary[directThread.ThreadId] = new JObject
                {
                    {"title", directThread.Title},
                    {"users", new JArray(directThread.Users.Select(x => x.Pk).ToArray())}
                }.ToString(Formatting.None);
                foreach (var user in directThread.Users)
                {
                    _viewModel.CentralUserRegistry[user.Pk] = user;
                }
            }

            return wrappedThreadList;
        }

        private void SortInboxThread()
        {
            var sorted = Threads.OrderByDescending(x => x.LastActivity).ToList();
            for (var i = 0; i < Threads.Count; i++)
            {
                var satisfied = false;
                var target = sorted[i];
                var j = i;
                for (; j < Threads.Count; j++)
                {
                    if (target.Equals(Threads[j]))
                    {
                        if (i == j)
                        {
                            satisfied = true;
                        }
                        break;
                    }
                }

                if (satisfied) continue;
                // If not satisfied, Threads[j] has to move to index i
                // ObservableCollection.Move() calls RemoveItem() under the hood which refreshes all items in collection
                // Removing Selected thread from collection will deselect the thread
                if (((App)Application.Current).ViewModel.SelectedThread != Threads[j])
                {
                    var tmp = Threads[j];
                    Threads.RemoveAt(j);
                    Threads.Insert(i, tmp);
                }
                else
                {
                    // j is always greater than i
                    for (var k = j - 1; i <= k; k--)
                    {
                        var tmp = Threads[k];
                        Threads.RemoveAt(k);
                        Threads.Insert(k+1, tmp);
                    }
                }
            }
        }

        private void OnThreadChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(DirectThreadWrapper.LastActivity) || string.IsNullOrEmpty(args.PropertyName))
            {
                SortInboxThread();
            }
        }

        public void FixThreadList()
        {
            // Somehow thread list got messed up and threads are not unique anymore
            var duplicates = Threads.GroupBy(x => x.ThreadId).Where(g => g.Count() > 1);
            foreach (var duplicateGroup in duplicates)
            {
                var duplicate = duplicateGroup.First();
                if (string.IsNullOrEmpty(duplicate.ThreadId)) continue;
                Threads.Remove(duplicate);
            }
        }
    }
}
