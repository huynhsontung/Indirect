using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Indirect.Entities.Wrappers;

namespace Indirect.Entities
{
    public class FlatReelsContainer : DependencyObject
    {
        public static DependencyProperty SelectedIndexProperty = DependencyProperty.Register(nameof(SelectedIndex),
            typeof(int),
            typeof(FlatReelsContainer),
            PropertyMetadata.Create(-1, OnSelectedIndexChanged));

        public int SelectedIndex
        {
            get => (int) GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public bool SecondaryView { get; set; }

        public ObservableCollection<ReelItemWrapper> Items { get; } = new ObservableCollection<ReelItemWrapper>();

        public List<long> UserOrder { get; } = new List<long>();

        private static MainViewModel ViewModel => ((App)Application.Current).ViewModel;
        private readonly Dictionary<long, ReelWrapper> _userReelsDictionary = new Dictionary<long, ReelWrapper>();
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly object _lockObj = new object();
        private int _userIndex;

        public FlatReelsContainer(ICollection<ReelWrapper> initialReels, int selected)
        {
            if (initialReels.Count == 0)
                throw new ArgumentException("Initial reels has to have at least 1 item.", nameof(initialReels));
            _userIndex = selected;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            foreach (var reel in initialReels)
            {
                UserOrder.Add(reel.Source.User.Pk);
                _userReelsDictionary[reel.Source.User.Pk] = reel;
            }
        }

        public void SelectItemToView()
        {
            lock (_lockObj)
            {
                var userItems = Items.Where(x => x.Source.User.Pk == UserOrder[_userIndex]).ToArray();
                if (userItems.Length == 0)
                {
                    return;
                }

                var firstUnseenItem = userItems.FirstOrDefault(x => x.Source.TakenAt > x.Parent.Source.Seen);
                var storyIndex = Items.IndexOf(firstUnseenItem ?? userItems[0]);
                SelectedIndex = storyIndex;
            }
        }

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue)
            {
                return;
            }

            var view = (FlatReelsContainer)d;
            view._dispatcherQueue.TryEnqueue(async () =>
            {
                await view.OnSelectionChanged((int) e.NewValue);
            });
        }

        private int GetUserIndex(long userId) => UserOrder.IndexOf(userId);

        private bool StoriesFetched(long userId) =>
            _userReelsDictionary[userId].Source.Items != null && _userReelsDictionary[userId].Source.Items.Length > 0;

        private async Task OnSelectionChanged(int selectedIndex)
        {
            if (selectedIndex == -1 || selectedIndex >= Items.Count) return;
            var userIndex = GetUserIndex(Items[selectedIndex].Source.User.Pk);
            await UpdateUserIndex(userIndex);
            await TryMarkStorySeen(selectedIndex);
        }

        public async Task UpdateUserIndex(int userIndex)
        {
            var selectedUserId = UserOrder[userIndex];
            ReelItemWrapper firstReelFromUser;
            lock (_lockObj)
            {
                firstReelFromUser = Items.FirstOrDefault(x => x.Source.User.Pk == selectedUserId);
            }

            if (firstReelFromUser == null)
            {
                if (!StoriesFetched(selectedUserId))
                {
                    // If user index doesn't have any story, fetch some
                    if (UserOrder.Count <= 3)
                    {
                        await FetchStories(UserOrder.ToArray());
                    }
                    else if (userIndex == 0)
                    {
                        await FetchStories(UserOrder[0], UserOrder[1], UserOrder[2]);
                    }
                    else if (userIndex == UserOrder.Count-1)
                    {
                        var c = UserOrder.Count;
                        await FetchStories(UserOrder[c - 3], UserOrder[c - 2], UserOrder[c - 1]);
                    }
                    else
                    {
                        await FetchStories(UserOrder[userIndex - 1], UserOrder[userIndex], UserOrder[userIndex + 1]);
                    }
                }

                SyncItems();
            }
            else if(userIndex == 0 && UserOrder.Count >= 2 && !StoriesFetched(UserOrder[1]))
            {
                await FetchStories(UserOrder[1]);
                SyncItems();
            }


            if (_userIndex == userIndex) return;
            var reelsHolders = UserOrder.Select(x => _userReelsDictionary[x]).ToList();
            if (UserOrder.Count <= 3)
            {
                var userList = reelsHolders.Where(x => x.Source.Items == null || x.Source.Items.Length == 0)
                    .Select(x => x.Source.User.Pk);
                await FetchStories(userList.ToArray());
                SyncItems();
            }
            else if (_userIndex < userIndex && _userIndex + 2 < UserOrder.Count)
            {
                // Moving forward
                var count = _userIndex + 4 < UserOrder.Count ? 3 : UserOrder.Count - (_userIndex + 2);
                var reels = reelsHolders.GetRange(_userIndex + 2, count);
                var userList = reels.Where(x => x.Source.Items == null || x.Source.Items.Length == 0)
                    .Select(x => x.Source.User.Pk);
                await FetchStories(userList.ToArray());
                SyncItems();
            }
            else if (_userIndex - 2 >= 0)
            {
                // Moving backward
                var startIndex = _userIndex - 4 >= 0 ? _userIndex - 4 : 0;
                var count = _userIndex - 2 - startIndex + 1;
                var reels = reelsHolders.GetRange(startIndex, count);
                var userList = reels.Where(x => x.Source.Items == null || x.Source.Items.Length == 0)
                    .Select(x => x.Source.User.Pk);
                await FetchStories(userList.ToArray());
                SyncItems();
            }

            _userIndex = userIndex;
        }

        private async Task TryMarkStorySeen(int storyIndex)
        {
            var story = Items[storyIndex];
            var storySource = story.Source;
            if (story.Parent.Source.Seen != null && story.Parent.Source.Seen >= storySource.TakenAt) return;
            await ViewModel.InstaApi.MarkStorySeenAsync(storySource.Id, storySource.User.Pk, storySource.TakenAt ?? DateTimeOffset.Now);
            story.Parent.Source.Seen = storySource.TakenAt;
        }

        private async Task FetchStories(params long[] users)
        {
            if (users == null || users.Length == 0) return;
            var result = await ViewModel.InstaApi.GetReels(users);
            if (result.IsSucceeded)
            {
                foreach (var (userId, reel) in result.Value)
                {
                    if (reel == null) continue;

                    if (_userReelsDictionary.ContainsKey(userId) && _userReelsDictionary[userId] != null)
                    {
                        _userReelsDictionary[userId].Source = reel;
                    }
                    else
                    {
                        _userReelsDictionary[userId] = new ReelWrapper(reel);
                    }
                }
            }
            
        }

        private void SyncItems()
        {
            var indexAdder = 0;
            foreach (var userId in UserOrder)
            {
                var reel = _userReelsDictionary[userId];
                var media = reel.Source.Items;
                if (media == null) continue;
                
                for (int i = 0; i < media.Length; i++)
                {
                    lock (_lockObj)
                    {
                        if (i + indexAdder >= Items.Count)
                        {
                            Items.Add(new ReelItemWrapper(media[i], reel));
                        }
                        else if (media[i].Id != Items[i+indexAdder].Source.Id)
                        {
                            Items.Insert(i+indexAdder, new ReelItemWrapper(media[i], reel));
                        }
                    }
                }

                indexAdder += media.Length;
            }
        }
    }
}
