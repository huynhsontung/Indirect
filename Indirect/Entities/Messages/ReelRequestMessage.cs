using CommunityToolkit.Mvvm.Messaging.Messages;
using Indirect.Entities.Wrappers;
using InstagramAPI.Classes.User;

namespace Indirect.Entities.Messages
{
    internal class ReelRequestMessage : RequestMessage<ReelWrapper>
    {
        public BaseUser User { get; }

        public ReelRequestMessage(BaseUser user)
        {
            User = user;
        }
    }
}
