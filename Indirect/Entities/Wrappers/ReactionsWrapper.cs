using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using InstagramAPI.Classes.Direct.ItemContent;
using InstagramAPI.Classes.User;

namespace Indirect.Entities.Wrappers
{
    class ReactionsWrapper : ReactionsContainer, INotifyPropertyChanged
    {
        private readonly MainViewModel _viewModel;
        private readonly ICollection<BaseUser> _users;
        private uint _likesCount;
        private bool _meLiked;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<BaseUser> Senders = new ObservableCollection<BaseUser>();

        public bool MeLiked
        {
            get => _meLiked;
            set
            {
                _meLiked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MeLiked)));
            }
        }
        public new uint LikesCount
        {
            get => _likesCount;
            set
            {
                _likesCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LikesCount)));
            }
        }

        public ReactionsWrapper(MainViewModel viewModel)
        {
            _viewModel = viewModel;
            Likes = new List<LikeReaction>(0);
            MeLiked = false;
            LikesCount = 0;
        }

        public ReactionsWrapper(MainViewModel viewModel, ReactionsContainer source, ICollection<BaseUser> usersList)
        {
            _viewModel = viewModel;
            _users = usersList;
            Update(source);
        }

        public void Clear()
        {
            Likes.Clear();
            LikesCount = 0;
            MeLiked = false;
            Senders.Clear();
        }

        public void Update(ReactionsContainer source)
        {
            Likes = source.Likes ?? new List<LikeReaction>();
            LikesCount = source.LikesCount;
            MeLiked = Likes.Any(x => x.SenderId == _viewModel.LoggedInUser.Pk);
            Senders.Clear();

            foreach (var like in Likes)
            {
                var user = _users?.FirstOrDefault(x => x.Pk == like.SenderId);
                if (user != null)
                {
                    Senders.Add(user);
                }
                else
                {
                    if (like.SenderId == _viewModel.LoggedInUser.Pk)
                    {
                        Senders.Add(_viewModel.LoggedInUser);
                    }
                }
            }
        }
    }

}
