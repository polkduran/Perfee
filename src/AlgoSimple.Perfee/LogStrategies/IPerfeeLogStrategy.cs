using System;
using AlgoSimple.Perfee.Common;

namespace AlgoSimple.Perfee.LogStrategies
{
    public interface IPerfeeLogStrategy : IDisposable
    {
        PerfId OpenEntry(string label, bool isGroupEntry);

        void CloseEntry(PerfId perfId);

        string GetLogs();

        void Reset();
    }
}
