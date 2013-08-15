using System;
using System.Collections.Generic;

namespace RubiksCube
{
    static class Performance
    {
        private static Dictionary<string, Timer> timers = new Dictionary<string, Timer>();
        public static Dictionary<string, Timer> Timers { get { return timers; } }

        private static Dictionary<string, Counter> counters = new Dictionary<string, Counter>();
        public static Dictionary<string, Counter> Counters { get { return counters; } }

        public static void AddTimer(string id, Timer timer)
        {
            timers.Add(id, timer);
        }

        public static void AddCounter(string id, Counter counter)
        {
            counters.Add(id, counter);
        }
    }

    struct PerformanceStopwatch : IDisposable
    {
        private DateTime startTime;
        private string id;

        public PerformanceStopwatch(string id)
        {
            startTime = DateTime.Now;
            this.id = id;
        }

        public void Dispose()
        {
            Performance.Timers[id].AddTime(DateTime.Now - startTime);
        }
    }

    class Counter
    {
        protected int result = 0;
        public int Result { get { return result; } }

        public virtual void Increment()
        {
            result++;
        }

        public override string ToString()
        {
            return result.ToString();
        }
    }

    class Timer
    {
        protected float time;
        public float Time { get { return time; } }

        public virtual void AddTime(TimeSpan newTime)
        {
            time = (float)newTime.TotalMilliseconds;
        }

        public override string ToString()
        {
            return PadFloat(time, 1, 10) + " millisecond" + (time == 1 ? "" : "s");
        }

        private string PadFloat(float number, int wholeDigits, int decimalDigits)
        {
            string result = number.ToString();

            string wholeDigitString = result.Split('.')[0];
            wholeDigitString = wholeDigitString.PadLeft(wholeDigits, '0');

            string decimalDigitString = "";
            if (result.Contains("."))
            {
                decimalDigitString = result.Split('.')[1];
                decimalDigitString = decimalDigitString.PadRight(decimalDigits, '0');
            }

            return wholeDigitString + "." + decimalDigitString.PadRight(decimalDigits, '0');
        }
    }

    class MovingAverage : Timer
    {
        private TimeSpan[] times;
        private int oldestTimeIndex;

        public MovingAverage(int span)
        {
            times = new TimeSpan[span];
            oldestTimeIndex = 0;

            time = 0;
        }

        public override void AddTime(TimeSpan newTime)
        {
            times[oldestTimeIndex] = newTime;
            oldestTimeIndex = (oldestTimeIndex + 1) % times.Length;

            time = 0;
            for (int i = 0; i < times.Length; i++)
            {
                time += (float)times[i].TotalMilliseconds;
            }

            time /= times.Length;
        }
    }
}