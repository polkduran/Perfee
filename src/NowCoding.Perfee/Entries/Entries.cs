using System;
using System.Text;
using NowCoding.Perfee.Common;

namespace NowCoding.Perfee.Entries
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

    public class LogEntry : IComparable<LogEntry>, IEquatable<LogEntry>
    {
        public LogEntry(StartEntry start, EndEntry end, int nestingLevel)
        {
            Message = start.Label;
            var elapsedTicks = end.CreationTimestamp - start.CreationTimestamp;
            ElapsedTime = TimeSpan.FromTicks(elapsedTicks);
            StartTime = new DateTime(start.CreationTimestamp, DateTimeKind.Utc);
            _perfId = start.Id;
            NestingLevel = nestingLevel;
        }

        private readonly PerfId _perfId;

        internal int NestingLevel { get; }

        internal string Message { get; }

        internal TimeSpan ElapsedTime { get; }

        internal DateTime StartTime { get; }

        public int CompareTo(LogEntry other)
        {
            if (other == null)
            {
                return 1;
            }
            return StartTime.CompareTo(other.StartTime);
        }

        public override string ToString()
        {
            var tab = new string(' ', NestingLevel);
            //var log = $"{Level}.'{ElapsedTime.ToString("g")}' > '{Message}'";
            var log = $"{tab}{NestingLevel}.[{StartTime:HH:mm:ss.ffff}] - elapsed '{ElapsedTime:g}' > '{Message}'";
            return log;
        }

        /// <summary>
        /// the hash code
        /// </summary>
        /// <returns>the hash</returns>
        public override int GetHashCode()
        {
            return _perfId.GetHashCode();
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj">the other</param>
        /// <returns>yes or not</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as LogEntry);
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="other">the other</param>
        /// <returns>yes or not</returns>
        public bool Equals(LogEntry other)
        {
            if (other == null)
            {
                return false;
            }
            return other._perfId.Equals(_perfId);
        }
    }

    public class GroupLogEntry
    {
        internal long CumulTicks { get; private set; }
        private double _meanTicks;
        private double _cumulVarTicks;
        private long _n;

        private readonly StringBuilder _individualTimesStr = new StringBuilder();

        public GroupLogEntry(string groupName)
        {
            GroupName = groupName;
        }

        internal string GroupName { get; }

        internal void Add(StartEntry start, EndEntry end)
        {
            var elapsedTicks = end.CreationTimestamp - start.CreationTimestamp;
            var elapsedTime = TimeSpan.FromTicks(elapsedTicks);
            _individualTimesStr.Append($"[{elapsedTime:g}]");

            // update statitistics
            var oldMeanTicks = _meanTicks;
            _n = _n + 1;
            CumulTicks = CumulTicks + elapsedTicks;
            _meanTicks = oldMeanTicks + (elapsedTicks - oldMeanTicks) / _n;
            _cumulVarTicks = _cumulVarTicks + (elapsedTicks - oldMeanTicks) * (elapsedTicks - _meanTicks);
        }

        public override string ToString()
        {
            var varTicks = _cumulVarTicks / _n;
            var stdevTicks = Math.Sqrt(varTicks);

            var cumulTime = TimeSpan.FromTicks(CumulTicks);
            var meanTime = TimeSpan.FromTicks(Convert.ToInt64(_meanTicks));
            var stdevTime = TimeSpan.FromTicks(Convert.ToInt64(stdevTicks));

            var log = $"[GROUP] {GroupName} -> took '{cumulTime:g}' with '{_n}' hits  mean[{meanTime:g}] stdev[{stdevTime:g}]";
            if (Perfee.Configuration.ShowGroupIndividualEntries)
            {
                log += $" entries[{_individualTimesStr}]";
            }
            return log;
        }
    }
}