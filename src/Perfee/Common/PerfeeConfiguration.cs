using System;
using System.Collections.Generic;
using Perfee.LogStrategies;

namespace Perfee.Common
{
    /// <summary>
    /// Perfee configuration.
    /// </summary>
    public class PerfeeConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not display individual elapsed times for grouped entries.
        /// The default value is false.
        /// </summary>
        public TimeSpan LogElapsedTimeThreshold { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets a value indicating whether or not display individual elapsed times for grouped entries.
        /// The default value is false.
        /// </summary>
        public bool ShowGroupIndividualEntries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the first group entry must be treated as a standalone log entry too.
        /// </summary>
        public bool FirstGroupEntryAsLogEntry { get; set; } = true;

        internal IPerfeeLogStrategy LogStrategy { get; private set; } = new OnDemandLogStrategy();

        private readonly List<Action<string>> _loggers = new List<Action<string>>();
        internal IEnumerable<Action<string>> Loggers => _loggers;

        /// <summary>
        /// Sets a threshold to filter the log entries with.
        /// </summary>
        /// <param name="elapsedTimeThreshold">Use TimeSpan.Zero to avoid filtering log entries.</param>
        /// <returns>The current instance.</returns>
        public PerfeeConfiguration SetLogElapsedTimeThreshold(TimeSpan elapsedTimeThreshold)
        {
            LogElapsedTimeThreshold = elapsedTimeThreshold;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether or not display individual elapsed times for grouped entries
        /// </summary>
        /// <param name="showGroupIndividualEntries">The default value is false.</param>
        /// <returns>The current instance.</returns>
        public PerfeeConfiguration SetShowGroupIndividualEntries(bool showGroupIndividualEntries)
        {
            ShowGroupIndividualEntries = showGroupIndividualEntries;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the first group entry must be treated as a standalone log entry too.
        /// </summary>
        /// <param name="firstGroupEntryAsLogEntry">The default value is true.</param>
        /// <returns>The current instance.</returns>
        public PerfeeConfiguration SetFirstGroupEntryAsLogEntry(bool firstGroupEntryAsLogEntry)
        {
            FirstGroupEntryAsLogEntry = firstGroupEntryAsLogEntry;
            return this;
        }

        /// <summary>
        /// Defines the log stragegy.
        /// </summary>
        /// <param name="logStrategy">The strategy.</param>
        /// <returns>The current isntance.</returns>
        public PerfeeConfiguration UseStrategy(IPerfeeLogStrategy logStrategy)
        {
            if (logStrategy == null)
            {
                throw new ArgumentNullException(nameof(logStrategy));
            }
            LogStrategy?.Dispose();
            LogStrategy = logStrategy;
            return this;
        }

        /// <summary>
        /// Defines an autoflush log strategy.
        /// Each time an entry is added it is flushed.
        /// Entries are not kept.
        /// </summary>
        /// <returns>The current instance.</returns>
        public PerfeeConfiguration UsAutoFlushOnlyStrategy()
        {
            return UseStrategy(new AutoFlushLogStrategy(false));
        }

        /// <summary>
        /// Defines an autoflush log strategy that keeps a trace of all the log entries.
        /// </summary>
        /// <returns>The current instance.</returns>
        public PerfeeConfiguration UseAutoFlushAndAggregatedOnDemandStrategy()
        {
            return UseStrategy(new AutoFlushLogStrategy(true));
        }

        /// <summary>
        /// Defines an on demand strategy for logs, log entries are not autoflushed.
        /// </summary>
        /// <returns>The current instance.</returns>
        public PerfeeConfiguration UseOnDemandOnlyStrategy()
        {
            return UseStrategy(new OnDemandLogStrategy());
        }

        /// <summary>
        /// Adds a logger that is called when flushing or demanding the log string representation.
        /// </summary>
        /// <param name="logger">Not null delegate.</param>
        /// <returns>The current instance.</returns>
        public PerfeeConfiguration AddLogger(Action<string> logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            _loggers.Add(logger);
            return this;
        }

        /// <summary>
        /// Clears the registered loggers.
        /// </summary>
        /// <returns>The current instance.</returns>
        public PerfeeConfiguration ClearLoggers()
        {
            _loggers.Clear();
            return this;
        }
    }
}