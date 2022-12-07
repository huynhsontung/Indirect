using Indirect.Entities.Wrappers;

namespace Indirect.Entities.Messages
{
    internal class OpenReelMessage
    {
        public ReelWrapper Start { get; }

        public OpenReelMessage(ReelWrapper start)
        {
            Start = start;
        }
    }
}
