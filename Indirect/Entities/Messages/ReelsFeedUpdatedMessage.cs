using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging.Messages;
using InstagramAPI.Classes;

namespace Indirect.Entities.Messages
{
    internal class ReelsFeedUpdatedMessage : ValueChangedMessage<IReadOnlyList<Reel>>
    {
        public ReelsFeedUpdatedMessage(IReadOnlyList<Reel> value) : base(value)
        {
        }
    }
}
