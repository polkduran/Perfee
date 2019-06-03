using System;
using AlgoSimple.Perfee.Common;

namespace AlgoSimple.Perfee
{
    /// <summary>
    ///
    /// Measures the time consumed on a defined code block.
    /// Entries may be measured in a SINGLE or in a GROUPED way.
    /// Grouped entries produce indicators as: total time, mean time and the standard deviation.
    ///
    /// Measuring an operation time
    /// ===========================
    ///
    /// Withing an using block:
    /// <code>
    ///     // Single entry time measurement
    ///     using (Perfee.Measure("operationName"))
    ///     {
    ///         [...] some code here [...]
    ///     }
    ///
    ///     // Time measurement by grouped entries
    ///     using (Perfee.MeasureGroup("groupName"))
    ///     {
    ///         [...] some code here [...]
    ///     }
    /// </code>
    ///
    ///
    /// OR handling manually the start and end measurement points:
    /// <code>
    ///     // Single entry time measurement
    ///     var p = Perfee.StartPoint("operationName");
    ///     [...] some code here [...]
    ///     Perfee.EndPoint(p);
    ///
    ///     // Time measurement by grouped entries
    ///     var p = Perfee.StartGroupPoint("groupName");
    ///     [...] some code here [...]
    ///     Perfee.EndPoint(p);
    /// </code>
    ///
    ///
    /// OR passing an Action or Fun{T} delegate:
    /// <code>
    ///     // Single entry time measurement
    ///     Perfee.Measure("operationName", () => [...] some code here [...]);
    ///
    ///     // Time measurement by grouped entries retrieving the delegate result
    ///     var res = Perfee.MeasureGroup("groupName", () => [...] some code here [...]);
    /// </code>
    ///
    /// Retrieving the measured time logs:
    /// <code>
    ///     var logs = Perfee.GetLogs();
    /// </code>
    ///
    /// Configuration:
    /// ==============
    /// Perfee.Configuration.LogElapsedTimeThreshold:TimeSpan
    /// The threshold under which an entry is not logged (but it is saved), the total time for grouped entries.
    /// The default value (TimeSpan.Zero) logs every entry.
    ///
    /// Perfee.Configuration.ShowGroupIndividualEntries:bool
    /// Whether or not display individual elapsed times for grouped entries.
    /// The default value is false.
    ///
    /// Loggers
    /// =======
    /// Add a logger callback, eg. Perfee.Configuration.AddLogger(Console.WriteLine);
    ///
    /// Log strategies
    /// ==============
    ///  - Perfee.Configuration.UseOnDemandOnlyStrategy(): he default log strategy is "logs on demand", logs are only written when requested.
    ///  - Perfee.Configuration.UsAutoFlushOnlyStrategy(): auto flushes each time a log is created, does not keep a trace of logs already flushed.
    ///  - Perfee.Configuration.UseAutoFlushAndAggregatedOnDemandStrategy(): auto flushes and keeps trace of all logs to be displayed on demand.
    /// </summary>
    public static class Perfee
    {
        public static PerfeeConfiguration Configuration { get; } = new PerfeeConfiguration();

        /// <summary>
        /// Starts an individual time measurement point.
        /// Needs to be manually closed calling <see cref="EndPoint"/>.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <returns>An id used by the <see cref="EndPoint"/> method.</returns>
        public static PerfId StartPoint(string message)
        {
            return AddEntry(message, isGroupEntry: false);
        }

        /// <summary>
        /// Starts a time measurement point for an entry belonging to a group.
        /// Needs to be manually closed calling <see cref="EndPoint"/>.
        /// </summary>
        /// <param name="groupName">The group name.</param>
        /// <returns>An id used by the <see cref="EndPoint"/> method.</returns>
        public static PerfId StartGroupPoint(string groupName)
        {
            return AddEntry(groupName, isGroupEntry: true);
        }

        /// <summary>
        /// Cancels the entry associated to the given id as long as the entry has not been closed yet.
        /// </summary>
        /// <param name="id">A measure point id provided by <see cref="StartPoint"/> or <see cref="StartGroupPoint"/>.</param>
        public static void DiscardMeasure(PerfId id)
        {
            Configuration.LogStrategy.CancelEntry(id);
        }

        /// <summary>
        /// Closes an opened entry (individual or grouped).
        /// </summary>
        /// <param name="id">The id generated after calling <see cref="StartPoint"/> or <see cref="StartGroupPoint"/>.</param>
        public static void EndPoint(PerfId id)
        {
            Configuration.LogStrategy.CloseEntry(id);
        }

        private static PerfId AddEntry(string label, bool isGroupEntry)
        {
            return Configuration.LogStrategy.OpenEntry(label, isGroupEntry);
        }

        /// <summary>
        /// Builds and returns all the individual and grouped entries logs.
        /// </summary>
        /// <returns>string representing all the individual and grouped entries</returns>
        public static string GetLogs()
        {
            var logs = Configuration.LogStrategy.GetLogs();
            PerfeeUtils.WriteLogs(logs);
            return logs;
        }

        /// <summary>
        /// Clears the existing logs.
        /// Do not clean opened entries.
        /// </summary>
        public static void Reset()
        {
            Configuration.LogStrategy.Reset();
        }

        /// <summary>
        /// Measures the elapsed time consumed by the given <paramref name="action"/>.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="action">The delegate to be executed.</param>
        public static void Measure(string message, Action action)
        {
            using (Measure(message))
            {
                action();
            }
        }

        /// <summary>
        /// Measures the elapsed time consumed by the given <paramref name="fun"/> delegate and returns its result.
        /// </summary>
        /// <typeparam name="T">The <paramref name="fun"/> return type.</typeparam>
        /// <param name="message">The log message.</param>
        /// <param name="fun">The delegate to be executed.</param>
        /// <returns>The result after the delegate execution.</returns>
        public static T Measure<T>(string message, Func<T> fun)
        {
            using (Measure(message))
            {
                return fun();
            }
        }

        /// <summary>
        /// Measures the elapsed time consumed by the given <paramref name="action"/> for a given entry group.
        /// </summary>
        /// <param name="groupName">The group name.</param>
        /// <param name="action">The delegate to be executed.</param>
        public static void MeasureGroup(string groupName, Action action)
        {
            using (MeasureGroup(groupName))
            {
                action();
            }
        }

        /// <summary>
        /// Measures the elapsed time consumed by the given <paramref name="fun"/> delegate and returns its result for a given entry group.
        /// </summary>
        /// <typeparam name="T">The <paramref name="fun"/> return type.</typeparam>
        /// <param name="groupName">The group name.</param>
        /// <param name="fun">The delegate to be executed.</param>
        /// <returns>The result after the delegate execution.</returns>
        public static T MeasureGroup<T>(string groupName, Func<T> fun)
        {
            using (MeasureGroup(groupName))
            {
                return fun();
            }
        }

        /// <summary>
        /// Starts an individual entry which be closed after the calling <see cref="IDisposable.Dispose"/> on the returned instance.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <returns>A disposable instance that ends the time measure when <see cref="IDisposable.Dispose"/> is called.</returns>
        public static IDisposable Measure(string message)
        {
            return new PerfeeWrapper(message, true);
        }

        /// <summary>
        /// Starts a grouped entry which be closed after the calling <see cref="IDisposable.Dispose"/> on the returned instance.
        /// </summary>
        /// <param name="groupName">The group name.</param>
        /// <returns>A disposable instance that ends the time measure when <see cref="IDisposable.Dispose"/> is called.</returns>
        public static IDisposable MeasureGroup(string groupName)
        {
            return new PerfeeWrapper(groupName, false);
        }
    }
}