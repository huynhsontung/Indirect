using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using InstaSharper.Classes.Models.Direct;
using InstaSharper.Classes.Models.User;

namespace Indirect.Wrapper
{
    class InstaDirectReactionsWrapper : InstaDirectReactions, INotifyPropertyChanged
    {
        private uint _likesCount;
        private bool _meLiked;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<InstaUserShort> Senders = new ObservableCollection<InstaUserShort>();

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

        public InstaDirectReactionsWrapper()
        {
            Likes = new List<InstaDirectLikeReaction>(0);
            MeLiked = false;
            LikesCount = 0;
        }

        public InstaDirectReactionsWrapper(InstaDirectReactions source, long viewerId)
        {
            Likes = source.Likes;
            MeLiked = source.MeLiked ? source.MeLiked : source.Likes.Any(x => x.SenderId == viewerId);
            LikesCount = source.LikesCount;
        }

        public void Clear()
        {
            LikesCount = 0;
            MeLiked = false;
            Senders.Clear();
        }

        public void Update(InstaDirectReactionsWrapper source, ICollection<InstaUserShort> usersList)
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
