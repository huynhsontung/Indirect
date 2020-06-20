using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using InstagramAPI;
using InstagramAPI.Classes.Story;

namespace Indirect.Wrapper
{
    public class ReelsWrapper
    {
        public ObservableCollection<StoryItemWrapper> Items { get; } = new ObservableCollection<StoryItemWrapper>();
        public List<string> UserOrder { get; } = new List<string>();

        private readonly Dictionary<string, Reel> _userReelsDictionary = new Dictionary<string, Reel>();
        private int _userIndex;
        private bool _loaded;

        public ReelsWrapper(ICollection<Reel> initialReels, int selected)
        {
            if (initialReels.Count == 0)
                throw new ArgumentException("Initial reels has to have at least 1 item.", nameof(initialReels));
            _userIndex = selected;
            foreach (var reel in initialReels)
            {
                UserOrder.Add(reel.Owner.Id);
                _userReelsDictionary[reel.Owner.Id] = reel;
            }
        }

        public int GetUserIndex(string userId) => UserOrder.IndexOf(userId);

        public bool StoriesFetched(string userId) =>
            _userReelsDictionary[userId].Items != null && _userReelsDictionary[userId].Items.Length > 0;

        public async Task OnLoaded(Selector view)
        {
            if (view == null) return;
            _loaded = true;
            var storyIndex = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Owner.Id == UserOrder[_userIndex])
                {
                    storyIndex = i;
                    break;
                }
            }

            if (view.SelectedIndex != storyIndex)
            {
                view.SelectedIndex = storyIndex;
            }
            else
            {
                await OnSelectionChanged(storyIndex);
            }
        }

        public async Task OnSelectionChanged(int selectedIndex)
        {
            if (selectedIndex == -1 || selectedIndex >= Items.Count || !_loaded) return;
            var userIndex = GetUserIndex(Items[selectedIndex].Owner.Id);
            await UpdateUserIndex(userIndex);
            await TryMarkStorySeen(selectedIndex);
        }

        public async Task UpdateUserIndex(int userIndex)
        {
            var selectedUserId = UserOrder[userIndex];
            if (Items.FirstOrDefault(x => x.Owner.Id == selectedUserId) == null)
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
                var userList = reelsHolders.Where(x => x.Items == null || x.Items.Length == 0).Select(x => x.Owner.Id);
                await FetchStories(userList.ToArray());
                SyncItems();
            }
            else if (_userIndex < userIndex && _userIndex + 2 < UserOrder.Count)
            {
                // Moving forward
                var count = _userIndex + 4 < UserOrder.Count ? 3 : UserOrder.Count - (_userIndex + 2);
                var reels = reelsHolders.GetRange(_userIndex + 2, count);
                var userList = reels.Where(x => x.Items == null || x.Items.Length == 0).Select(x => x.Owner.Id);
                await FetchStories(userList.ToArray());
                SyncItems();
            }
            else if (_userIndex - 2 >= 0)
            {
                // Moving backward
                var startIndex = _userIndex - 4 >= 0 ? _userIndex - 4 : 0;
                var count = _userIndex - 2 - startIndex + 1;
                var reels = reelsHolders.GetRange(startIndex, count);
                var userList = reels.Where(x => x.Items == null || x.Items.Length == 0).Select(x => x.Owner.Id);
                await FetchStories(userList.ToArray());
                SyncItems();
            }

            _userIndex = userIndex;
        }

        private async Task TryMarkStorySeen(int storyIndex)
        {
            var story = Items[storyIndex];
            if (story.Parent.Seen != null && story.Parent.Seen >= story.TakenAtTimestamp) return;
            await Instagram.Instance.MarkStorySeenAsync(story.Id, story.Owner.Id, story.TakenAtTimestamp);
            story.Parent.Seen = story.TakenAtTimestamp;
        }

        private async Task FetchStories(params string[] users)
        {
            var result = await Instagram.Instance.GetReels(users);
            if (result.IsSucceeded)
            {
                foreach (var reel in result.Value)
                {
                    _userReelsDictionary[reel.Owner.Id] = reel;
                }
            }
            
        }

        private void SyncItems()
        {
            var indexAdder = 0;
            foreach (var userId in UserOrder)
            {
                var reel = _userReelsDictionary[userId];
                if (reel.Items == null) continue;
                
                for (int i = 0; i < reel.Items.Length; i++)
                {
                    if (i + indexAdder >= Items.Count)
                    {
                        Items.Add(new StoryItemWrapper(reel.Items[i], reel));
                    }
                    else if (reel.Items[i].Id != Items[i+indexAdder].Id)
                    {
                        Items.Insert(i+indexAdder, new StoryItemWrapper(reel.Items[i], reel));
                    }
                }

                indexAdder += reel.Items.Length;
            }
        }
    }
}
