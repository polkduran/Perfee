using System;

namespace AlgoSimple.Perfee.Common
{
    internal class PerfeeWrapper : IDisposable
    {
        private readonly PerfId _perfId;

        public PerfeeWrapper(string label, bool single)
        {
            _perfId = single ? Perfee.StartPoint(label) : Perfee.StartGroupPoint(label);
        }

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
                Perfee.EndPoint(_perfId);
            }
            _disposed = true;
        }

        ~PerfeeWrapper()
        {
            Dispose(false);
        }
    }
}