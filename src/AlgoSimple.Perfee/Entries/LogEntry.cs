using System;
using AlgoSimple.Perfee.Common;

namespace AlgoSimple.Perfee.Entries
{
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
}