using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NowCoding.Perfee.Entries;

namespace NowCoding.Perfee.Common
{
    public static class PerfeeUtils
    {
        public static string BuildLogs(int singleOpenEntries, int groupedOpenEntries, DateTime logsGenerationStartTime, IEnumerable<LogEntry> logEntries, IEnumerable<GroupLogEntry> groupEntries)
        {
            if (Perfee.Configuration.LogElapsedTimeThreshold > TimeSpan.Zero)
            {
                logEntries = logEntries.Where(x => x.ElapsedTime >= Perfee.Configuration.LogElapsedTimeThreshold);
                groupEntries = groupEntries.Where(x => x.CumulTicks >= Perfee.Configuration.LogElapsedTimeThreshold.Ticks);
            }

            var logBuilder = new StringBuilder()
                .AppendLine("<--------------- Perfee --------------->")
                .AppendLine($"  Log elapsed time threshold '{Perfee.Configuration.LogElapsedTimeThreshold}'. ");

            if (singleOpenEntries > 0)
            {
                logBuilder.Append("{singleOpenEntries} single entries still open. ");
            }
            if (groupedOpenEntries > 0)
            {
                logBuilder.Append("{groupedOpenEntries} group entries still open. ");
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