using CommunityToolkit.Mvvm.Messaging.Messages;
using InstagramAPI.Classes.Responses;

namespace Indirect.Entities.Messages
{
    internal class UserPresenceRequestMessage : RequestMessage<UserPresenceValue>
    {
        public long UserId { get; }

        public UserPresenceRequestMessage(long userId)
        {
            UserId = userId;
        }
    }
}
