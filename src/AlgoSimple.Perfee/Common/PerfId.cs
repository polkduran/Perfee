using System;
using System.Threading;

namespace AlgoSimple.Perfee.Common
{
    /// <summary>
    /// Represents an id for a given operation being measured.
    /// </summary>
    public struct PerfId : IEquatable<PerfId>
    {
        private static int _current;
        internal static PerfId Next()
        {
            var c = Interlocked.Increment(ref _current);
            return new PerfId(c);
        }

        internal int Id { get; }

        private PerfId(int id)
        {
            Id = id;
        }

        /// <summary>
        /// Eqeals
        /// </summary>
        /// <param name="other">The other</param>
        public bool Equals(PerfId other) => other.Id == Id;

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj">The other.</param>
        public override bool Equals(object obj) => obj is PerfId && Equals((PerfId)obj);

        /// <summary>
        /// Gets the hash
        /// </summary>
        public override int GetHashCode() => Id;

        public static bool operator ==(PerfId left, PerfId right) => left.Equals(right);

        public static bool operator !=(PerfId left, PerfId right) => !left.Equals(right);
    }
}