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
        public readonly ObservableCollection<StoryItemWrapper> Items = new ObservableCollection<StoryItemWrapper>();

        private readonly Dictionary<string, Reel> _userReelsDictionary = new Dictionary<string, Reel>();
        private readonly List<string> _userOrder = new List<string>();
        private int _userIndex;
        private Selector _selector;

        public ReelsWrapper(ICollection<Reel> initialReels, int selected)
        {
            if (initialReels.Count == 0)
                throw new ArgumentException("Initial reels has to have at least 1 item.", nameof(initialReels));
            _userIndex = selected;
            foreach (var reel in initialReels)
            {
                _userOrder.Add(reel.Owner.Id);
                _userReelsDictionary[reel.Owner.Id] = reel;
            }
            SyncItems();
        }

        public int GetUserIndex(string userId) => _userOrder.IndexOf(userId);

        public bool StoriesFetched(string userId) =>
            _userReelsDictionary[userId].Items != null && _userReelsDictionary[userId].Items.Length > 0;

        public void AttachSelector(Selector view)
        {
            _selector = view;
            view.SelectionChanged -= SelectorOnSelectionChanged;
            view.SelectionChanged += SelectorOnSelectionChanged;
            var storyIndex = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Owner.Id == _userOrder[_userIndex])
                {
                    storyIndex = i;
                    break;
                }
            }

            if (view.SelectedIndex != storyIndex)
                view.SelectedIndex = storyIndex;
        }

        public void DetachSelector()
        {
            _selector.SelectionChanged -= SelectorOnSelectionChanged;
        }

        private async void SelectorOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            var view = (Selector) sender;
            if (view.SelectedIndex == -1) return;
            var userIndex = GetUserIndex(Items[view.SelectedIndex].Owner.Id);
            await UpdateUserIndex(userIndex);
        }

        public async Task UpdateUserIndex(int userIndex)
        {
            var selectedUserId = _userOrder[userIndex];
            if (Items.FirstOrDefault(x => x.Owner.Id == selectedUserId) == null)
            {
                if (!StoriesFetched(selectedUserId))
                {
                    // If user index doesn't have any story, fetch some
                    if (_userOrder.Count <= 3)
                    {
                        await FetchStories(_userOrder.ToArray());
                    }
                    else if (userIndex == 0)
                    {
                        await FetchStories(_userOrder[0], _userOrder[1], _userOrder[2]);
                    }
                    else if (userIndex == _userOrder.Count-1)
                    {
                        var c = _userOrder.Count;
                        await FetchStories(_userOrder[c - 3], _userOrder[c - 2], _userOrder[c - 1]);
                    }
                    else
                    {
                        await FetchStories(_userOrder[userIndex - 1], _userOrder[userIndex], _userOrder[userIndex + 1]);
                    }
                }

                SyncItems();
            }

            if (_userIndex == userIndex) return;
            var reelsHolders = _userOrder.Select(x => _userReelsDictionary[x]).ToList();
            if (_userOrder.Count <= 3)
            {
                var userList = reelsHolders.Where(x => x.Items == null || x.Items.Length == 0).Select(x => x.Owner.Id);
                await FetchStories(userList.ToArray());
                SyncItems();
            }
            else if (_userIndex < userIndex && _userIndex + 2 < _userOrder.Count)
            {
                // Moving forward
                var count = _userIndex + 4 < _userOrder.Count ? 3 : _userOrder.Count - (_userIndex + 2);
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
            foreach (var userId in _userOrder)
            {
                var reel = _userReelsDictionary[userId];
                if (reel.Items == null) continue;
                
                for (int i = 0; i < reel.Items.Length; i++)
                {
                    if (i + indexAdder >= Items.Count)
                    {
                        Items.Add(new StoryItemWrapper(reel.Items[i]));
                    }
                    else if (reel.Items[i].Id != Items[i+indexAdder].Id)
                    {
                        Items.Insert(i+indexAdder, new StoryItemWrapper(reel.Items[i]));
                    }
                }

                indexAdder += reel.Items.Length;
            }
        }
    }
}
