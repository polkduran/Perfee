using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using AlgoSimple.Perfee.Common;
using AlgoSimple.Perfee.Entries;

namespace AlgoSimple.Perfee.LogStrategies
{
    public class AutoFlushLogStrategy : IPerfeeLogStrategy
    {
        private ConcurrentDictionary<PerfId, StartEntry> _startEntries = new ConcurrentDictionary<PerfId, StartEntry>();
        private readonly SortedSet<LogEntry> _logEntries = new SortedSet<LogEntry>();
        private ConcurrentDictionary<string, GroupLogEntry> _groupLogEntries = new ConcurrentDictionary<string, GroupLogEntry>();
        private int _level;
        private readonly bool _keepAllEntries;
        private bool _disposed;

        public AutoFlushLogStrategy(bool keepAllEntries)
        {
            _keepAllEntries = keepAllEntries;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _startEntries = null;
                _groupLogEntries = null;
            }
            _disposed = true;
        }

        ~AutoFlushLogStrategy()
        {
            Dispose(false);
        }

        public PerfId OpenEntry(string label, bool isGroupEntry)
        {
            // naive implementation of nested entries
            var level = Interlocked.Increment(ref _level);
            var entry = new StartEntry(label, isGroupEntry, level);
            _startEntries.TryAdd(entry.Id, entry);
            return entry.Id;
        }

        public void CloseEntry(PerfId perfId)
        {
            Interlocked.Decrement(ref _level);
            if (!_startEntries.TryRemove(perfId, out var start))
            {
                return;
            }

            var end = new EndEntry(perfId);
            var logEntry = new LogEntry(start, end, start.Level);
            if (!start.IsGroupEntry)
            {
                LogSingleEntry(logEntry);
            }
            else
            {
                LogGroupedEntry(start, logEntry, end);
            }
        }

        private void LogGroupedEntry(StartEntry start, LogEntry logEntry, EndEntry end)
        {
            if (!_groupLogEntries.ContainsKey(start.Label)
                && Perfee.Configuration.FirstGroupEntryAsLogEntry
                && logEntry.ElapsedTime < Perfee.Configuration.LogElapsedTimeThreshold)
            {
                if (_keepAllEntries)
                {
                    _logEntries.Add(logEntry);
                }
                PerfeeUtils.WriteLogs(logEntry.ToString());
            }
            var groupLog = _groupLogEntries.GetOrAdd(start.Label, groupName => new GroupLogEntry(groupName));
            groupLog.Add(start, end);

            if (groupLog.CumulTicks < Perfee.Configuration.LogElapsedTimeThreshold.Ticks)
            {
                return;
            }
            PerfeeUtils.WriteLogs(groupLog.ToString());
        }

        private void LogSingleEntry(LogEntry logEntry)
        {
            if (_keepAllEntries)
            {
                _logEntries.Add(logEntry);
            }
            if (logEntry.ElapsedTime < Perfee.Configuration.LogElapsedTimeThreshold)
            {
                PerfeeUtils.WriteLogs(logEntry.ToString());
            }
        }

        public string GetLogs()
        {
            if (!_keepAllEntries)
            {
                return "Autoflush: logs are written as they are created.";
            }
            var start = DateTime.UtcNow;
            return PerfeeUtils.BuildLogs(_startEntries.Values, start, _logEntries, _groupLogEntries.Values);
        }

        public void Reset()
        {
            _startEntries.Clear();
            _groupLogEntries.Clear();
        }
    }
}
