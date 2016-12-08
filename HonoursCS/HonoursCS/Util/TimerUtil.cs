using System;

namespace HonoursCS.Util
{
    public class Timer
    {
        /// <summary>
        /// Measures the elapsed time on some action.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static TimeSpan Time(Action action)
        {
            var startTime = DateTime.Now;
            action();
            var endTime = DateTime.Now;
            return endTime - startTime;
        }
    }
}