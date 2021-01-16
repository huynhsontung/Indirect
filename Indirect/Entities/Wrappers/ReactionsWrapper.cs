using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Indirect.Utilities;
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

        public ReactionsWrapper(MainViewModel viewModel)
        {
            _viewModel = viewModel;
            EmojiReactions = new ObservableCollection<ReactionWithUser>();
            MeLiked = false;
        }

        public ReactionsWrapper(MainViewModel viewModel, ReactionsContainer source, ICollection<BaseUser> usersList) :
            this(viewModel)
        {
            _users = usersList;
            Update(source);
        }

        public void Clear()
        {
            MeLiked = false;
            Senders.Clear();
            EmojiReactions.Clear();
        }

        public void Update(ReactionsContainer source)
        {
            if (source == null) return;
            _reactionsContainer = source;

            if (source.Emojis == null || source.Emojis.Length == 0) return;

            var emojiReactions = new List<EmojiReaction>(source.Emojis);

            if (source.Likes != null && source.Likes.Length > 0)
            {
                emojiReactions.AddRange(source.Likes.Select(x =>
                {
                    var emojiReaction = new EmojiReaction();
                    PropertyCopier<LikeReaction, EmojiReaction>.Copy(x, emojiReaction);
                    emojiReaction.Emoji = "♥";
                    return emojiReaction;
                }));
            }

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
                { Reaction = emojiReaction, User = GetUserFromId(emojiReaction.SenderId) });
            }

            MeLiked = EmojiReactions.Any(x => x?.User != null && x.User.Equals(_viewModel.LoggedInUser));
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
