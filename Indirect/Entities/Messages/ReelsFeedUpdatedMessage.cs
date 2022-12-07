using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Indirect.Entities.Wrappers;

namespace Indirect.Entities.Messages
{
    internal class ReelsFeedUpdatedMessage : ValueChangedMessage<IReadOnlyList<ReelWrapper>>
    {
        public ReelsFeedUpdatedMessage(IReadOnlyList<ReelWrapper> value) : base(value)
        {
        }
    }
}
