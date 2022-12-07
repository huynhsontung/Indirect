using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Indirect.Entities.Messages;
using Indirect.Entities.Wrappers;
using InstagramAPI.Classes.User;

namespace Indirect.Entities
{
    internal sealed partial class ProfilePictureWithStoryViewModel : ObservableRecipient, IRecipient<ReelsFeedUpdatedMessage>
    {
        [ObservableProperty] private bool _hasReel;
        [ObservableProperty] private bool _unseen;
        [ObservableProperty] private ObservableCollection<BaseUser> _users;

        private BaseUser _singleUser;
        private ReelWrapper _reel;

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
                ReelWrapper reel = _reel = Messenger.Send(new ReelRequestMessage(_singleUser)).Response;
                HasReel = reel != null;
                Unseen = reel?.HasUnseenItems ?? false;
            }
        }

        public void Receive(ReelsFeedUpdatedMessage message)
        {
            if (_singleUser != null)
            {
                Update(message.Value);
            }
        }

        [RelayCommand]
        public void OpenReel()
        {
            if (_reel == null) return;
            Messenger.Send(new OpenReelMessage(_reel));
        }

        private void Update(IReadOnlyList<ReelWrapper> reels)
        {
            foreach (ReelWrapper reel in reels)
            {
                if (reel.Source.User.Equals(_singleUser))
                {
                    _reel = reel;
                    HasReel = true;
                    Unseen = reel.HasUnseenItems;
                    return;
                }
            }

            HasReel = false;
            Unseen = false;
        }
    }
}
