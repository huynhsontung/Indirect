using System;
using System.Collections.Generic;

namespace Indirect.Utilities
{
    class TimestampClosenessComparer : IEqualityComparer<DateTimeOffset>
    {
        public bool Equals(DateTimeOffset x, DateTimeOffset y)
        {
            return TimeSpan.FromHours(-3) < x - y && x - y < TimeSpan.FromHours(3);
        }

        public int GetHashCode(DateTimeOffset obj) => 0;
    }
}