using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Indirect.Entities.Wrappers;
using Indirect.Utilities;
using InstagramAPI;
using InstagramAPI.Classes;
using InstagramAPI.Utils;
using Windows.System;
using CommunityToolkit.Mvvm.Messaging;
using Indirect.Entities.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using Indirect.Pages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Core;

namespace Indirect.Entities
{
    internal class ReelsFeed : ObservableRecipient, IRecipient<ReelRequestMessage>, IRecipient<OpenReelMessage>
    {
        public ObservableCollection<ReelWrapper> Reels { get; } = new ObservableCollection<ReelWrapper>();

        public ImmutableList<Reel> LatestReelsFeed { get; private set; }

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly MainViewModel _viewModel;
        private CancellationTokenSource _reelsUpdateLoop;
        private CoreViewHandle _reelPageView;

        public ReelsFeed(MainViewModel viewModel)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _viewModel = viewModel;

            IsActive = true;
        }

        public void Receive(ReelRequestMessage message)
        {
            if (LatestReelsFeed == null || message.User == null)
            {
                message.Reply(null);
                return;
            }

            foreach (ReelWrapper reel in Reels)
            {
                if (reel.Source.User.Equals(message.User))
                {
                    message.Reply(reel);
                    return;
                }
            }

            message.Reply(null);
        }

        public async void Receive(OpenReelMessage message)
        {
            await OpenReelsAsync(message.Start);
        }

        public async Task OpenReelsAsync(ReelWrapper startReel)
        {
            if (_reelPageView != null && !_viewModel.ShowStoryInNewWindow)
            {
                await CloseSecondaryReelViews();
            }

            if (DeviceFamilyHelpers.MultipleViewsSupport && _viewModel.ShowStoryInNewWindow)
            {
                OpenReelsInNewWindow(startReel);
            }
            else
            {
                var flatReels = await PrepareFlatReelsContainer(startReel);
                ((Frame)Window.Current.Content).Navigate(typeof(ReelPage), flatReels);
            }
        }

        private async Task CloseSecondaryReelViews()
        {
            CoreViewHandle handle = _reelPageView;
            if (handle != null)
            {
                await App.CloseSecondaryView(handle.Id);
                _reelPageView = null;
            }
        }

        private void OpenReelsInNewWindow(ReelWrapper startReel)
        {
            int selectedIndex = LatestReelsFeed.IndexOf(startReel.Source);

            if (_reelPageView != null && ((App)App.Current).IsViewOpen(_reelPageView.Id))
            {
                async void PrepareReels()
                {
                    FlatReelsContainer flatReels = await PrepareReelsForSecondaryViewAsync(selectedIndex);
                    Frame frame = (Frame)Window.Current.Content;
                    frame.Navigate(typeof(ReelPage), flatReels);
                    frame.BackStack.Clear();
                }

                _reelPageView.CoreView.DispatcherQueue.TryEnqueue(PrepareReels);
            }
            else
            {
                CoreApplicationView view = CoreApplication.CreateNewView();

                async void RunOnMainThread()
                {
                    FlatReelsContainer flatReels = await PrepareReelsForSecondaryViewAsync(selectedIndex);
                    int viewId = await ((App)App.Current).CreateAndShowNewView(typeof(ReelPage), flatReels, view);
                    _reelPageView = new CoreViewHandle(viewId, view);
                }

                view.DispatcherQueue.TryEnqueue(RunOnMainThread);
            }

        }

        private async Task<FlatReelsContainer> PrepareReelsForSecondaryViewAsync(int selectedIndex)
        {
            List<ReelWrapper> wrappedReels = LatestReelsFeed.Select(x => new ReelWrapper(x)).ToList();
            FlatReelsContainer flatReels = await PrepareFlatReelsContainer(wrappedReels, selectedIndex);
            flatReels.SecondaryView = true;
            return flatReels;
        }

        public async Task UpdateReelsFeedAsync(ReelsTrayFetchReason fetchReason = ReelsTrayFetchReason.ColdStart)
        {
            if (_viewModel.HideReelsFeed) return;
            if (Debouncer.Throttle(nameof(UpdateReelsFeed), TimeSpan.FromSeconds(10)) ||
                fetchReason == ReelsTrayFetchReason.ColdStart)
            {
                await UpdateReelsFeed(fetchReason);
            }
        }

        private async Task UpdateReelsFeed(ReelsTrayFetchReason fetchReason)
        {
            var result = await ((App)App.Current).ViewModel.InstaApi.GetReelsTrayFeed(fetchReason);
            if (!result.IsSucceeded) return;
            _dispatcherQueue.TryEnqueue(fetchReason == ReelsTrayFetchReason.PullToRefresh
                ? DispatcherQueuePriority.Normal
                : DispatcherQueuePriority.Low,
            () =>
            {
                try
                {
                    var reels = result.Value.Where(x => x.ReelType == "user_reel").ToArray();
                    LatestReelsFeed = reels.ToImmutableList();
                    SyncReels(reels);
                    WeakReferenceMessenger.Default.Send(new ReelsFeedUpdatedMessage(Reels));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    // TODO: investigate origin of ArgumentOutOfRangeException
                    DebugLogger.LogException(e);
                }
            });
        }

        private void SyncReels(Reel[] target)
        {
            if (target.Length == 0) return;

            lock (Reels)
            {
                // Remove existing reels that are not in the target
                for (int i = 0; i < Reels.Count; i++)
                {
                    var existingReel = Reels[i];
                    if (target.All(x => !x.Id.Equals(existingReel.Source.Id)))
                    {
                        Reels.RemoveAt(i);
                        i--;
                    }
                }

                // Add new reels from target and also update existing ones
                for (int i = 0; i < target.Length; i++)
                {
                    var reel = target[i];
                    var equivalentIndex = -1;
                    for (int j = 0; j < Reels.Count; j++)
                    {
                        if (Reels[j].Source.Id.Equals(reel.Id))
                        {
                            equivalentIndex = j;
                            break;
                        }
                    }

                    if (equivalentIndex >= 0)
                    {
                        Reels[equivalentIndex].Source = reel;
                        if (i == equivalentIndex) continue;
                        Reels.RemoveAt(equivalentIndex);
                    }

                    Reels.Insert(i > Reels.Count ? Reels.Count : i, new ReelWrapper(reel));
                }
            }
        }

        public async Task<FlatReelsContainer> PrepareFlatReelsContainer(ReelWrapper selected)
        {
            FlatReelsContainer flatReelsContainer;
            int selectedIndex;
            lock (Reels)
            {
                selectedIndex = Reels.IndexOf(selected);
                if (selectedIndex == -1) return null;
                flatReelsContainer = new FlatReelsContainer(Reels, selectedIndex);
            }
            await flatReelsContainer.UpdateUserIndex(selectedIndex);
            return flatReelsContainer;
        }

        public async Task<FlatReelsContainer> PrepareFlatReelsContainer(ICollection<ReelWrapper> reels, int selectedIndex)
        {
            var flatReelsContainer = new FlatReelsContainer(reels, selectedIndex);
            await flatReelsContainer.UpdateUserIndex(selectedIndex);
            return flatReelsContainer;
        }

        public async void StartReelsFeedUpdateLoop()
        {
            CancellationTokenSource tokenSource;
            _reelsUpdateLoop?.Cancel();
            lock (this)
            {
                tokenSource = _reelsUpdateLoop = new CancellationTokenSource();
            }

            try
            {
                while (!tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), tokenSource.Token);
                        await UpdateReelsFeedAsync();
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                }
            }
            finally
            {
                lock (this)
                {
                    if (_reelsUpdateLoop == tokenSource)
                    {
                        _reelsUpdateLoop = null;
                    }
                }

                tokenSource.Dispose();
            }
        }

        public void StopReelsFeedUpdateLoop(bool clear = false)
        {
            _reelsUpdateLoop?.Cancel();
            if (clear)
            {
                lock (Reels)
                {
                    Reels.Clear();
                }
            }
        }
    }
}
