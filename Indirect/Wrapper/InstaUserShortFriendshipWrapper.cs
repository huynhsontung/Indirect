using InstaSharper.API;
using InstaSharper.Classes.Models.User;

namespace Indirect.Wrapper
{
    public class InstaUserShortFriendshipWrapper : InstaUserShortWrapper
    {
        public InstaFriendshipShortStatus FriendshipStatus { get; set; }

        public InstaUserShortFriendshipWrapper(InstaUserShortFriendship source, InstaApi api) : base(source, api)
        {
            FriendshipStatus = source.FriendshipStatus;
        }
    }
}
