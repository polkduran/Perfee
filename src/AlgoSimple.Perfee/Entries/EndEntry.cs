using System;
using AlgoSimple.Perfee.Common;

namespace AlgoSimple.Perfee.Entries
{
    public struct EndEntry
    {
        public EndEntry(PerfId id)
        {
            Id = id;
            CreationTimestamp = DateTime.UtcNow.Ticks;
        }

        internal PerfId Id { get; }
        internal long CreationTimestamp { get; }
    }
}