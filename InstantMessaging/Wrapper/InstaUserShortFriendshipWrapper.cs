using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using InstaSharper.API;
using InstaSharper.Classes.Models.User;

namespace InstantMessaging.Wrapper
{
    public class InstaUserShortFriendshipWrapper : InstaUserShortWrapper
    {
        public InstaFriendshipShortStatus FriendshipStatus { get; set; }

        public InstaUserShortFriendshipWrapper(InstaUserShortFriendship source, IInstaApi api) : base(source, api)
        {
            FriendshipStatus = source.FriendshipStatus;
        }
    }
}
