using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using InstagramAPI.Classes.Story;

namespace Indirect
{
    internal partial class ApiContainer
    {
        private CancellationTokenSource _reelsUpdateLoop;
        public readonly ObservableCollection<Reel> ReelsFeed = new ObservableCollection<Reel>();

        public async Task UpdateReelsFeed()
        {
            var result = await _instaApi.GetReelsTrayFeed();
            if (!result.IsSucceeded) return;
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ReelsFeed.Clear();
                foreach (var reel in result.Value)
                {
                    ReelsFeed.Add(reel);
                }
            });
        }

        public async void StartReelsFeedUpdateLoop()
        {
            _reelsUpdateLoop?.Cancel();
            _reelsUpdateLoop?.Dispose();
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
            _reelsUpdateLoop?.Dispose();
        }
    }
}
