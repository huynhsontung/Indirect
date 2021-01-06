using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using InstagramAPI.Classes.Direct.ItemContent;
using InstagramAPI.Classes.User;

namespace Indirect.Entities.Wrappers
{
    class ReactionsWrapper : INotifyPropertyChanged
    {
        private readonly MainViewModel _viewModel;
        private readonly ICollection<BaseUser> _users;
        private readonly ObservableCollection<EmojiReaction> _emojiReactions;
        private uint _likesCount;
        private bool _meLiked;
        private ReactionsContainer _reactionsContainer;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<BaseUser> Senders = new ObservableCollection<BaseUser>();

        public ReadOnlyObservableCollection<EmojiReaction> EmojiReactions { get; }

        public bool MeLiked
        {
            get => _meLiked;
            set
            {
                _meLiked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MeLiked)));
            }
        }
        
        public uint LikesCount
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
            _emojiReactions = new ObservableCollection<EmojiReaction>();
            EmojiReactions = new ReadOnlyObservableCollection<EmojiReaction>(_emojiReactions);
            MeLiked = false;
            LikesCount = 0;
        }

        public ReactionsWrapper(MainViewModel viewModel, ReactionsContainer source, ICollection<BaseUser> usersList) :
            this(viewModel)
        {
            _users = usersList;
            Update(source);
        }

        public void Clear()
        {
            LikesCount = 0;
            MeLiked = false;
            Senders.Clear();
        }

        public void Update(ReactionsContainer source)
        {
            if (source == null) return;
            _reactionsContainer = source;

            #region To Be Deprecated
            
            //TODO: TO BE DEPRECATED
            var likes = source.Likes ?? new LikeReaction[0];
            LikesCount = source.LikesCount;
            MeLiked = likes.Any(x => x.SenderId == _viewModel.LoggedInUser.Pk);
            Senders.Clear();

            foreach (var like in likes)
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

            #endregion

            if (source.Emojis == null || source.Emojis.Length == 0) return;

            var consistent = true;
            if (source.Emojis.Length == _emojiReactions.Count)
            {
                for (int i = 0; i < _emojiReactions.Count; i++)
                {
                    var local = _emojiReactions[i];
                    var reference = source.Emojis[i];
                    if (local.SenderId != reference.SenderId || local.Timestamp != reference.Timestamp)
                    {
                        consistent = false;
                        break;
                    }
                }
            }
            else
            {
                consistent = false;
            }

            if (consistent)
            {
                return;
            }
            
            _emojiReactions.Clear();
            foreach (var emojiReaction in source.Emojis)
            {
                _emojiReactions.Add(emojiReaction);
            }
        }

        public void Update(ReactionsWrapper source) => Update(source._reactionsContainer);
    }

}
