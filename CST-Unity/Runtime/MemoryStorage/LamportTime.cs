using System;

namespace HIAAC.CstUnity.MemoryStorage
{
    public sealed class LamportTime : LogicalTime, IEquatable<LamportTime>
    {
        private readonly int _time;


        public LamportTime(int initialTime = 0)
        {
            _time = initialTime;
        }


        public override LogicalTime Increment()
        {
            return new LamportTime(_time + 1);
        }



        public override string ToString()
        {
            return _time.ToString();
        }


        public static new LamportTime FromString(string value)
        {
            return new LamportTime(int.Parse(value));
        }


        public static LamportTime Synchronize(LamportTime t0, LamportTime t1)
        {
            if (t0 == null || t1 == null)
                throw new ArgumentNullException("LamportTime cannot be null.");


            int newTime = Math.Max(t0._time, t1._time) + 1;
            return new LamportTime(newTime);
        }


        public override int CompareTo(LogicalTime other)
        {
            if (other is not LamportTime o)
                throw new ArgumentException("Cannot compare different LogicalTime types.");


            return _time.CompareTo(o._time);
        }


        public bool Equals(LamportTime other)
        {
            if (other is null) return false;
            return _time == other._time;
        }


        public override bool Equals(object obj)
        {
            return Equals(obj as LamportTime);
        }


        public override int GetHashCode()
        {
            return _time.GetHashCode();
        }
    }
}