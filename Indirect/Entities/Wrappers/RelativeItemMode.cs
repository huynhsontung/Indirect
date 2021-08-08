using System;

namespace Indirect.Entities.Wrappers
{
    [Flags]
    public enum RelativeItemMode
    {
        None = 0,
        Before = 1,
        After = 2,
        Both = 3
    }
}
