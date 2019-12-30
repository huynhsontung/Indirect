using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using InstaSharper.Classes.Models.Direct;

namespace Indirect.Wrapper
{
    class InstaDirectReactionsWrapper : InstaDirectReactions, INotifyPropertyChanged
    {
        private uint _likesCount;
        private bool _meLiked;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<InstaUserShortWrapper> Senders = new ObservableCollection<InstaUserShortWrapper>();

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

        public InstaDirectReactionsWrapper(InstaDirectReactions source)
        {
            Likes = source.Likes;
            MeLiked = source.MeLiked;
            _likesCount = source.LikesCount;
        }

        public void Update(InstaDirectReactionsWrapper source, ICollection<InstaUserShortWrapper> usersList, long myId)
        {
            LikesCount = source.LikesCount;
            MeLiked = source.MeLiked;
            Senders.Clear();

            var set = false; 
            foreach (var like in Likes)
            {
                var user = usersList.SingleOrDefault(x => x.Pk == like.SenderId);
                if (user != null)
                {
                    Senders.Add(user);
                }

                if (like.SenderId == myId)
                {
                    set = true;
                    MeLiked = true;
                }
            }

            if (!set) MeLiked = false;
        }
    }

}
