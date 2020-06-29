using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Indirect.Utilities;
using InstagramAPI;
using InstagramAPI.Classes;
using InstagramAPI.Utils;

namespace Indirect.Wrapper
{
    class ReelsFeed
    {
        public readonly ObservableCollection<Reel> Reels = new ObservableCollection<Reel>();

        private CancellationTokenSource _reelsUpdateLoop;
        private bool _justUpdated;

        public async Task UpdateReelsFeed()
        {
            if (_justUpdated) return;
            var result = await Instagram.Instance.GetReelsTrayFeed();
            if (!result.IsSucceeded) return;
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    SyncReels(result.Value);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    // TODO: investigate origin of ArgumentOutOfRangeException
                    DebugLogger.LogException(e);
                }
            });
            _justUpdated = true;
            _ = Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(x => { _justUpdated = false; });
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
                    if (target.All(x => x.Id != existingReel.Id))
                    {
                        Reels.RemoveAt(i);
                        i--;
                    }
                }

                // Add new reels from target and also update existing ones
                for (int i = 0; i < target.Length; i++)
                {
                    var reel = target[i];
                    Reel equivalent = null;
                    var equivalentIndex = -1;
                    for (int j = 0; j < Reels.Count; j++)
                    {
                        if (Reels[j].Id == reel.Id)
                        {
                            equivalent = Reels[j];
                            equivalentIndex = j;
                            break;
                        }
                    }
                    if (equivalent != null)
                    {
                        PropertyCopier<Reel, Reel>.Copy(reel, equivalent);
                        if (i == equivalentIndex) continue;
                        Reels.RemoveAt(equivalentIndex);
                        Reels.Insert(i > Reels.Count ? Reels.Count : i, equivalent);
                    }
                    else
                    {
                        Reels.Insert(i > Reels.Count ? Reels.Count : i, reel);
                    }
                }
            }
        }

        public async Task<ReelsWrapper> PrepareReelsWrapper(int selectedIndex)
        {
            ReelsWrapper reelsWrapper;
            lock (Reels)
            {
                reelsWrapper = new ReelsWrapper(Reels, selectedIndex);
            }
            await reelsWrapper.UpdateUserIndex(selectedIndex);
            return reelsWrapper;
        }

        public async void StartReelsFeedUpdateLoop()
        {
            _reelsUpdateLoop?.Cancel();
            _reelsUpdateLoop = new CancellationTokenSource();
            while (!_reelsUpdateLoop.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), _reelsUpdateLoop.Token);
                    await UpdateReelsFeed();
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }

        public void StopReelsFeedUpdateLoop()
        {
            _reelsUpdateLoop?.Cancel();
        }
    }
}
