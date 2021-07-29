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

        public event PropertyChangedEventHandler PropertyChanged;

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

        public ReactionsWrapper(MainViewModel viewModel, ReactionsContainer source, ICollection<BaseUser> usersList)
        {
            _viewModel = viewModel;
            _users = usersList;
            var reactionsWithUser = source?.Emojis.Select(reaction => new ReactionWithUser
                {Reaction = reaction, User = GetUserFromId(reaction.SenderId)});
            EmojiReactions = reactionsWithUser != null
                ? new ObservableCollection<ReactionWithUser>(reactionsWithUser)
                : new ObservableCollection<ReactionWithUser>();
            MeLiked = EmojiReactions.Any(x => x?.User != null && x.User.Equals(_viewModel.LoggedInUser));
        }

        public void Clear()
        {
            MeLiked = false;
            EmojiReactions.Clear();
        }

        public void Add(EmojiReaction reaction)
        {
            Remove(reaction.SenderId);
            EmojiReactions.Add(new ReactionWithUser {Reaction = reaction, User = GetUserFromId(reaction.SenderId)});
            MeLiked = EmojiReactions.Any(x => x?.User != null && x.User.Equals(_viewModel.LoggedInUser));
        }

        public void Remove(long senderId)
        {
            for (int i = 0; i < EmojiReactions.Count; i++)
            {
                if (EmojiReactions[i].User.Pk == senderId)
                {
                    EmojiReactions.RemoveAt(i);
                    break;
                }
            }

            MeLiked = EmojiReactions.Any(x => x?.User != null && x.User.Equals(_viewModel.LoggedInUser));
        }

        private BaseUser GetUserFromId(long userId)
        {
            return userId == _viewModel.LoggedInUser.Pk
                ? _viewModel.LoggedInUser
                : _users?.FirstOrDefault(x => x.Pk == userId);
        }
    }

}
