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
        private static IProducerConsumerCollection<T> BuildConcurrentCollection<T>()
        {
            return new ConcurrentBag<T>();
        }

        private IProducerConsumerCollection<StartEntry> _startEntries = BuildConcurrentCollection<StartEntry>();
        private IProducerConsumerCollection<EndEntry> _endEntries = BuildConcurrentCollection<EndEntry>();

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
