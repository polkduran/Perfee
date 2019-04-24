using System;
using System.Text;

namespace AlgoSimple.Perfee.Entries
{
    public class GroupLogEntry
    {
        internal long CumulTicks { get; private set; }
        private long _maxElapsedTicks;
        private double _meanTicks;
        private double _cumulVarTicks;
        private long _n;
        private readonly object _lock = new object();

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

            if (Perfee.Configuration.ShowGroupIndividualEntries)
            {
                _individualTimesStr.Append($"[{elapsedTime:g}]");
            }

            // update statitistics
            lock (_lock)
            {
                _maxElapsedTicks = Math.Max(_maxElapsedTicks, elapsedTicks);
                var oldMeanTicks = _meanTicks;
                _n += 1;
                CumulTicks += elapsedTicks;
                _meanTicks = oldMeanTicks + (elapsedTicks - oldMeanTicks) / _n;
                _cumulVarTicks += (elapsedTicks - oldMeanTicks) * (elapsedTicks - _meanTicks);
            }
        }

        public override string ToString()
        {
            var varTicks = _cumulVarTicks / _n;
            var stdevTicks = Math.Sqrt(varTicks);

            var cumulTime = TimeSpan.FromTicks(CumulTicks);
            var meanTime = TimeSpan.FromTicks(Convert.ToInt64(_meanTicks));
            var stdevTime = TimeSpan.FromTicks(Convert.ToInt64(stdevTicks));
            var maxTime = TimeSpan.FromTicks(_maxElapsedTicks);

            var log = $"[GROUP] {GroupName} -> took '{cumulTime:g}' with '{_n}' hits  mean[{meanTime:g}] stdev[{stdevTime:g}] max[{maxTime:g}]";
            if (Perfee.Configuration.ShowGroupIndividualEntries)
            {
                log += $" entries[{_individualTimesStr}]";
            }

            return log;
        }
    }
}