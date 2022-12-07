using CommunityToolkit.Mvvm.Messaging.Messages;
using InstagramAPI.Classes;
using InstagramAPI.Classes.User;

namespace Indirect.Entities.Messages
{
    internal class ReelRequestMessage : RequestMessage<Reel>
    {
        public BaseUser User { get; }

        public ReelRequestMessage(BaseUser user)
        {
            User = user;
        }
    }
}
