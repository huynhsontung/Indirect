using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Indirect.Utilities;
using InstagramAPI.Classes.Direct;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp;
using InstagramAPI.Classes.Core;
using InstagramAPI.Utils;

namespace Indirect.Entities.Wrappers
{
    internal class InboxWrapper : DependencyObject, IIncrementalSource<DirectThreadWrapper>
    {
        public event EventHandler FirstUpdated;    // callback to start RealtimeClient

        public static readonly DependencyProperty SelectedThreadProperty = DependencyProperty.Register(
            nameof(SelectedThread),
            typeof(DirectThreadWrapper),
            typeof(InboxWrapper),
            new PropertyMetadata(null, OnSelectedThreadPropertyChanged));

        public DirectThreadWrapper SelectedThread
        {
            get => (DirectThreadWrapper)GetValue(SelectedThreadProperty);
            set => SetValue(SelectedThreadProperty, value);
        }

        public InboxContainer Container { get; private set; }

        public long SeqId => Container.SeqId;

        public DateTimeOffset SnapshotAt => Container.SnapshotAt;

        public bool PendingInbox { get; }

        public IncrementalLoadingCollection<InboxWrapper, DirectThreadWrapper> Threads { get; }

        private string OldestCursor { get; set; }

        private readonly object _lockObj;
        private readonly MainViewModel _viewModel;
        private bool _firstTime = true;
        private int _pageCounter;
        private DirectThreadWrapper _tempThread;

        public InboxWrapper(MainViewModel viewModel, bool pending = false)
        {
            _lockObj = new object();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            PendingInbox = pending;
            Container = new InboxContainer();
            Threads =
                new IncrementalLoadingCollection<InboxWrapper, DirectThreadWrapper>(this);
        }

        private static void OnSelectedThreadPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var inbox = (InboxWrapper)d;
            lock (inbox._lockObj)
            {
                var tempThread = inbox._tempThread;
                if (tempThread != null && tempThread != e.NewValue)
                {
                    inbox._tempThread = null;
                    if (tempThread.IsTemp) inbox.Threads.Remove(tempThread);
                }

                if (e.NewValue is DirectThreadWrapper newThread && !inbox.Threads.Contains(newThread))
                {
                    newThread.IsTemp = true;
                    inbox.Threads.Insert(0, newThread);
                    inbox._tempThread = newThread;
                    inbox.SelectedThread = newThread;
                }
            }
        }

        private void UpdateExcludeThreads(InboxContainer source, bool updateCursor)
        {
            Container = source;
            if (updateCursor)
            {
                OldestCursor = Container.Inbox.OldestCursor;
            }
        }

        public async Task UpdateInbox()
        {
            var result = await _viewModel.InstaApi.GetInboxAsync(PaginationParameters.MaxPagesToLoad(1));
            if (!result.IsSucceeded)
            {
                return;
            }

            var container = result.Value;
            UpdateExcludeThreads(container, false);
            await Dispatcher.QuickRunAsync(() =>
            {
                foreach (var thread in container.Inbox.Threads)
                {
                    var existed = false;
                    lock (_lockObj)
                    {
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
                            wrappedThread.RegisterPropertyChangedCallback(DirectThreadWrapper.LastPermanentItemProperty,
                                OnThreadLastPermanentItemChanged);
                            Threads.Insert(0, wrappedThread);
                        }
                    }
                }

                SortInboxThread();
                container.Inbox.Threads = null;
            });
        }

        public async Task<IEnumerable<DirectThreadWrapper>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
        {
            this.Log($"Getting page: {pageIndex}");
            try
            {
                while (pageIndex > _pageCounter)
                {
                    try
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        return Array.Empty<DirectThreadWrapper>();
                    }
                }

                if (!_firstTime && !Container.Inbox.HasOlder || cancellationToken.IsCancellationRequested)
                {
                    return Array.Empty<DirectThreadWrapper>();
                }

                var pagesToLoad = pageSize / 20;
                if (pagesToLoad < 1) pagesToLoad = 1;
                var pagination = PaginationParameters.MaxPagesToLoad(pagesToLoad);
                pagination.StartFromMaxId(OldestCursor);
                var result = await _viewModel.InstaApi.GetInboxAsync(pagination, PendingInbox);
                if (!result.IsSucceeded || cancellationToken.IsCancellationRequested)
                {
                    return Array.Empty<DirectThreadWrapper>();
                }

                var container = result.Value;
                UpdateExcludeThreads(container, true);
                var wrappedThreadList = new List<DirectThreadWrapper>(container.Inbox.Threads.Count);
                foreach (var directThread in container.Inbox.Threads)
                {
                    var wrappedThread = new DirectThreadWrapper(_viewModel, directThread);
                    wrappedThread.RegisterPropertyChangedCallback(DirectThreadWrapper.LastPermanentItemProperty,
                        OnThreadLastPermanentItemChanged);
                    wrappedThreadList.Add(wrappedThread);
                }

                if (_firstTime)
                {
                    _firstTime = false;
                    FirstUpdated?.Invoke(this, EventArgs.Empty);
                }

                container.Inbox.Threads = null;
                return wrappedThreadList;
            }
            finally
            {
                _pageCounter++;
            }
        }

        public async Task ClearInbox()
        {
            _firstTime = true;
            _pageCounter = 0;
            Container = new InboxContainer();
            OldestCursor = null;
            await Threads.RefreshAsync();
        }

        private void SortInboxThread()
        {
            lock (_lockObj)
            {
                var sorted = Threads.OrderByDescending(x => x.Source.LastActivity).ToList();
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
                    if (SelectedThread != Threads[j])
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
                            Threads.Insert(k + 1, tmp);
                        }
                    }
                }
            }
        }

        private void OnThreadLastPermanentItemChanged(DependencyObject sender, DependencyProperty dp)
        {
            SortInboxThread();
        }
    }
}
