using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AlgoSimple.Perfee.Common;
using AlgoSimple.Perfee.Entries;

namespace AlgoSimple.Perfee.LogStrategies
{
    public class OnDemandLogStrategy : IPerfeeLogStrategy
    {
        private ConcurrentDictionary<PerfId, (StartEntry start, EndEntry? end)> _entries = new ConcurrentDictionary<PerfId, (StartEntry, EndEntry?)>();

        private SortedSet<LogEntry> _logEntries = new SortedSet<LogEntry>();
        private SortedDictionary<string, GroupLogEntry> _groupLogEntries = new SortedDictionary<string, GroupLogEntry>();

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
                    _entries = null;
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

            _entries.TryAdd(entry.Id, (entry, null));
            return entry.Id;
        }

        public void CloseEntry(PerfId perfId)
        {
            Interlocked.Decrement(ref _level);
            if (_entries.TryGetValue(perfId, out var entryPair))
            {
                _entries.TryUpdate(perfId, (entryPair.Item1, new EndEntry(perfId)), entryPair);
            }
        }

        public void CancelEntry(PerfId perfId)
        {
            _entries.TryRemove(perfId, out _);
        }

        public string GetLogs()
        {
            lock (_syncReadRoot)
            {
                var start = DateTime.UtcNow;
                UpdateLogEntries();

                var openEntries = _entries.Values.Select(t => t.start).ToArray();
                return PerfeeUtils.BuildLogs(openEntries, start, _logEntries, _groupLogEntries.Values);
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

        private void UpdateLogEntries()
        {
            var completedEntries = _entries
                                        .Where(kv => kv.Value.end != null)
                                        .Select(kv => kv.Key);

            foreach (var id in completedEntries)
            {
                if (!_entries.TryRemove(id, out var t))
                {
                    continue;
                }

                var (start, end) = (t.start, t.end.Value);
                if (start.IsGroupEntry)
                {
                    if (!_groupLogEntries.TryGetValue(start.Label, out var groupLog))
                    {
                        groupLog = new GroupLogEntry(start.Label);
                        _groupLogEntries.Add(groupLog.GroupName, groupLog);
                        if (Perfee.Configuration.FirstGroupEntryAsLogEntry)
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
