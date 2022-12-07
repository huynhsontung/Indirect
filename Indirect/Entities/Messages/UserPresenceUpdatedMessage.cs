using InstagramAPI.Classes.Responses;

namespace Indirect.Entities.Messages
{
    internal class UserPresenceUpdatedMessage
    {
        public long UserId { get; }
        public UserPresenceValue Presence { get; }

        public UserPresenceUpdatedMessage(long userId, UserPresenceValue presence)
        {
            UserId = userId;
            Presence = presence;
        }
    }
}
