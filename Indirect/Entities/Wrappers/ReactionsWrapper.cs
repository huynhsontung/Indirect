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
        private uint _likesCount;
        private bool _meLiked;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<BaseUser> Senders = new ObservableCollection<BaseUser>();

        public new bool MeLiked
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

        public ReactionsWrapper()
        {
            Likes = new List<LikeReaction>(0);
            MeLiked = false;
            LikesCount = 0;
        }

        public ReactionsWrapper(ReactionsContainer source)
        {
            Likes = source.Likes;
            MeLiked = source.MeLiked;
            LikesCount = source.LikesCount;
        }

        public void Clear()
        {
            LikesCount = 0;
            MeLiked = false;
            Senders.Clear();
        }

        public void Update(ReactionsWrapper source, ICollection<BaseUser> usersList)
        {
            LikesCount = source.LikesCount;
            MeLiked = source.MeLiked;
            Senders.Clear();

            foreach (var like in source.Likes)
            {
                var user = usersList.SingleOrDefault(x => x.Pk == like.SenderId);
                if (user != null)
                {
                    Senders.Add(user);
                }
            }
        }
    }

}
