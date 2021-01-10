using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using InstagramAPI.Classes.Direct.ItemContent;
using InstagramAPI.Classes.User;

namespace Indirect.Entities.Wrappers
{
    public class ReactionWithUser
    {
        public BaseUser User { get; set; }
        
        public EmojiReaction Reaction { get; set; }
    }
    
    class ReactionsWrapper : INotifyPropertyChanged
    {
        private readonly MainViewModel _viewModel;
        private readonly ICollection<BaseUser> _users;
        private uint _likesCount;
        private bool _meLiked;
        private ReactionsContainer _reactionsContainer;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<BaseUser> Senders = new ObservableCollection<BaseUser>();

        public ObservableCollection<ReactionWithUser> EmojiReactions { get; }

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
            EmojiReactions = new ObservableCollection<ReactionWithUser>();
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
            if (source.Emojis.Length == EmojiReactions.Count)
            {
                for (int i = 0; i < EmojiReactions.Count; i++)
                {
                    var local = EmojiReactions[i];
                    var reference = source.Emojis[i];
                    if (local.Reaction.SenderId != reference.SenderId || local.Reaction.Timestamp != reference.Timestamp)
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
            
            EmojiReactions.Clear();
            foreach (var emojiReaction in source.Emojis)
            {
                EmojiReactions.Add(new ReactionWithUser
                    {Reaction = emojiReaction, User = GetUserFromId(emojiReaction.SenderId)});
            }
        }

        public void Update(ReactionsWrapper source) => Update(source._reactionsContainer);

        private BaseUser GetUserFromId(long userId)
        {
            return userId == _viewModel.LoggedInUser.Pk
                ? _viewModel.LoggedInUser
                : _users?.FirstOrDefault(x => x.Pk == userId);
        }
    }

}
