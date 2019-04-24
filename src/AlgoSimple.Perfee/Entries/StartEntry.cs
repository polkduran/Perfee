using System;
using AlgoSimple.Perfee.Common;

namespace AlgoSimple.Perfee.Entries
{
    public struct StartEntry
    {
        public StartEntry(string label, bool isGroupEntry, int level)
        {
            Label = label;
            Id = PerfId.Next();
            CreationTimestamp = DateTime.UtcNow.Ticks;
            IsGroupEntry = isGroupEntry;
            Level = level;
        }

        internal int Level { get; }

        internal PerfId Id { get; }

        /// <summary>
        /// In ticks
        /// </summary>
        internal long CreationTimestamp { get; }

        /// <summary>
        /// The entry message for a <see cref="IsGroupEntry"/> entry or the group name.
        /// </summary>
        internal string Label { get; }

        /// <summary>
        /// Whether this entry belongs to a group or not.
        /// </summary>
        internal bool IsGroupEntry { get; }
    }
}