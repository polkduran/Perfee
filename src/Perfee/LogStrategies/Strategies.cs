using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Perfee.Common;
using Perfee.Entries;

namespace Perfee.LogStrategies
{
    public interface IPerfeeLogStrategy : IDisposable
    {
        PerfId OpenEntry(string label, bool isGroupEntry);

        void CloseEntry(PerfId perfId);

        string GetLogs();

        void Reset();
    }

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
            StartEntry start;
            if (!_startEntries.TryRemove(perfId, out start))
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
                && Common.Perfee.Configuration.FirstGroupEntryAsLogEntry
                && logEntry.ElapsedTime < Common.Perfee.Configuration.LogElapsedTimeThreshold)
            {
                if (_keepAllEntries)
                {
                    _logEntries.Add(logEntry);
                }
                PerfeeUtils.WriteLogs(logEntry.ToString());
            }
            var groupLog = _groupLogEntries.GetOrAdd(start.Label, groupName => new GroupLogEntry(groupName));
            groupLog.Add(start, end);

            if (groupLog.CumulTicks < Common.Perfee.Configuration.LogElapsedTimeThreshold.Ticks)
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
            if (logEntry.ElapsedTime < Common.Perfee.Configuration.LogElapsedTimeThreshold)
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
            int openSingle = 0, openGrouped = 0;
            foreach (var startEntry in _startEntries)
            {
                if (startEntry.Value.IsGroupEntry)
                {
                    openGrouped++;
                }
                else
                {
                    openSingle++;
                }
            }
            return PerfeeUtils.BuildLogs(openSingle, openGrouped, start, _logEntries, _groupLogEntries.Values);
        }

        public void Reset()
        {
            _startEntries.Clear();
            _groupLogEntries.Clear();
        }
    }

    public class OnDemandLogStrategy : IPerfeeLogStrategy
    {
        private static IProducerConsumerCollection<T> BuildConcurrentCollection<T>()
        {
            return new ConcurrentBag<T>();
        }

        private IProducerConsumerCollection<StartEntry> _startEntries = BuildConcurrentCollection<StartEntry>();
        private IProducerConsumerCollection<EndEntry> _endEntries = BuildConcurrentCollection<EndEntry>();

        private SortedSet<LogEntry> _logEntries = new SortedSet<LogEntry>();
        private Dictionary<string, GroupLogEntry> _groupLogEntries = new Dictionary<string, GroupLogEntry>();

        private readonly object _syncReadRoot = new object();

        private int _level;

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                lock (_syncReadRoot)
                {
                    _startEntries = null;
                    _endEntries = null;
                    _logEntries = null;
                    _groupLogEntries = null;
                }
            }
            _disposed = true;
        }

        ~OnDemandLogStrategy()
        {
            Dispose(false);
        }

        public PerfId OpenEntry(string label, bool isGroupEntry)
        {
            // naive implementation of nested entries
            var level = Interlocked.Increment(ref _level);
            var entry = new StartEntry(label, isGroupEntry, level);
            _startEntries.TryAdd(entry);
            return entry.Id;
        }

        public void CloseEntry(PerfId perfId)
        {
            Interlocked.Decrement(ref _level);
            _endEntries.TryAdd(new EndEntry(perfId));
        }

        public string GetLogs()
        {
            lock (_syncReadRoot)
            {
                var start = DateTime.UtcNow;
                int singleEntriesOpen, groupEntriesOpen;
                UpdateLogEntries(out singleEntriesOpen, out groupEntriesOpen);
                return PerfeeUtils.BuildLogs(singleEntriesOpen, groupEntriesOpen, start, _logEntries, _groupLogEntries.Values);
            }
        }

        public void Reset()
        {
            lock (_syncReadRoot)
            {
                _logEntries.Clear();
                _groupLogEntries.Clear();
            }
        }

        private void UpdateLogEntries(out int singleEntriesOpen, out int groupEntriesOpen)
        {
            singleEntriesOpen = 0;
            groupEntriesOpen = 0;
            StartEntry[] startEntries;
            {
                var tmp = _startEntries;
                _startEntries = BuildConcurrentCollection<StartEntry>();
                startEntries = tmp.ToArray();
            }
            Dictionary<PerfId, EndEntry> endEntries;
            {
                var tmp = _endEntries;
                _endEntries = BuildConcurrentCollection<EndEntry>();
                endEntries = tmp.ToArray().ToDictionary(e => e.Id, e => e);
            }

            for (var i = 0; i < startEntries.Length; i++)
            {
                var start = startEntries[i];
                EndEntry end;
                if (!endEntries.TryGetValue(start.Id, out end))
                {
                    // not closed yet, put it back
                    _startEntries.TryAdd(start);
                    if (start.IsGroupEntry)
                    {
                        groupEntriesOpen++;
                    }
                    else
                    {
                        singleEntriesOpen++;
                    }
                    continue;
                }

                if (start.IsGroupEntry)
                {
                    GroupLogEntry groupLog;
                    if (!_groupLogEntries.TryGetValue(start.Label, out groupLog))
                    {
                        groupLog = new GroupLogEntry(start.Label);
                        _groupLogEntries.Add(groupLog.GroupName, groupLog);
                        if (Common.Perfee.Configuration.FirstGroupEntryAsLogEntry)
                        {
                            _logEntries.Add(new LogEntry(start, end, start.Level));
                        }
                    }
                    groupLog.Add(start, end);
                }
                else
                {
                    _logEntries.Add(new LogEntry(start, end, start.Level));
                }
            }
        }
    }
}
