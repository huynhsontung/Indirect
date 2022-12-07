using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Indirect.Entities.Messages;
using InstagramAPI.Classes;
using InstagramAPI.Classes.User;

namespace Indirect.Entities
{
    internal sealed partial class ProfilePictureWithStoryViewModel : ObservableRecipient, IRecipient<ReelsFeedUpdatedMessage>
    {
        [ObservableProperty] private bool _hasReel;
        [ObservableProperty] private bool _unseen;
        [ObservableProperty] private ObservableCollection<BaseUser> _users;

        private BaseUser _singleUser;

        public ProfilePictureWithStoryViewModel()
        {
            IsActive = true;
        }

        partial void OnUsersChanged(ObservableCollection<BaseUser> value)
        {
            if (value.Count is > 1 or 0)
            {
                HasReel = false;
                Unseen = false;
            }
            else
            {
                _singleUser = value[0];
                Reel reel = Messenger.Send(new ReelRequestMessage(_singleUser)).Response;
                HasReel = reel != null;
                Unseen = reel?.Seen == null;
            }
        }

        public void Receive(ReelsFeedUpdatedMessage message)
        {
            if (_singleUser != null)
            {
                Update(message.Value);
            }
        }

        private void Update(IReadOnlyList<Reel> reels)
        {
            foreach (Reel reel in reels)
            {
                if (reel.User.Equals(_singleUser))
                {
                    HasReel = true;
                    Unseen = reel.Seen == null;
                    return;
                }
            }

            HasReel = false;
            Unseen = false;
        }
    }
}
