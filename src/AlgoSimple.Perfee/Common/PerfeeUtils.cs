using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlgoSimple.Perfee.Entries;

namespace AlgoSimple.Perfee.Common
{
    public static class PerfeeUtils
    {
        public static string BuildLogs(IEnumerable<StartEntry> openedEntries, DateTime logsGenerationStartTime, IEnumerable<LogEntry> logEntries, IEnumerable<GroupLogEntry> groupEntries)
        {
            if (Perfee.Configuration.LogElapsedTimeThreshold > TimeSpan.Zero)
            {
                logEntries = logEntries.Where(x => x.ElapsedTime >= Perfee.Configuration.LogElapsedTimeThreshold);
                groupEntries = groupEntries.Where(x => x.CumulTicks >= Perfee.Configuration.LogElapsedTimeThreshold.Ticks);
            }

            var logBuilder = new StringBuilder()
                .AppendLine("<--------------- Perfee --------------->")
                .AppendLine($"  Log elapsed time threshold '{Perfee.Configuration.LogElapsedTimeThreshold}'. ");

            var groupedOpenedEntries = openedEntries
                                        .Select(e => e.IsGroupEntry ? $"[GROUP]{e.Label}" : e.Label)
                                        .GroupBy(label => label)
                                        .Aggregate(new StringBuilder(), (b, g) => b.AppendLine($"{g.Key} -> {g.Count()}"));

            if (groupedOpenedEntries.Length > 0)
            {
                logBuilder.AppendLine("_______ Opened entries _______");
                logBuilder.Append(groupedOpenedEntries);
                logBuilder.AppendLine("_______ /Opened entries _______");
            }

            logBuilder.AppendLine();

            foreach (var entry in logEntries)
            {
                logBuilder.AppendLine(entry.ToString());
            }
            logBuilder.AppendLine(); 

            foreach (var entry in groupEntries)
            {
                logBuilder.AppendLine(entry.ToString());
            }

            logBuilder
                .AppendLine($"Logs generation started at {logsGenerationStartTime:HH:mm:ss.ffff} and took {DateTime.UtcNow - logsGenerationStartTime:g}")
                .AppendLine($"<--------------- /Perfee -------------->");

            return logBuilder.ToString();
        }

        public static void WriteLogs(string log)
        {
            foreach (var logger in Perfee.Configuration.Loggers)
            {
                try
                {
                    logger?.Invoke($"[Perfee] {log}");
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}