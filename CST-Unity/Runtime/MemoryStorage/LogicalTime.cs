using System;

namespace HIAAC.CstUnity.MemoryStorage
{
    /// <summary>
    /// A logical time for distributed communication.
    /// </summary>
    public abstract class LogicalTime : IComparable<LogicalTime>
    {
        /// <summary>
        /// Returns a time with the self time incremented by one.
        /// </summary>
        public abstract LogicalTime Increment();


        public abstract override string ToString();


        /// <summary>
        /// Creates an instance from a string.
        /// </summary>
        public static LogicalTime FromString(string value)
        {
            throw new NotImplementedException("Must be implemented in derived class.");
        }


        /// <summary>
        /// Compares two times and returns the current time.
        /// </summary>
        public static LogicalTime Synchronize(LogicalTime t0, LogicalTime t1)
        {
            throw new NotImplementedException("Must be implemented in derived class.");
        }


        public abstract int CompareTo(LogicalTime other);


        public static bool operator <(LogicalTime a, LogicalTime b) =>
            a.CompareTo(b) < 0;


        public static bool operator >(LogicalTime a, LogicalTime b) =>
            a.CompareTo(b) > 0;


        public static bool operator <=(LogicalTime a, LogicalTime b) =>
            a.CompareTo(b) <= 0;


        public static bool operator >=(LogicalTime a, LogicalTime b) =>
            a.CompareTo(b) >= 0;
    }
}