using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Easy.IO
{
    /// <summary>
    /// 时间类型枚举
    /// </summary>
    public enum TimeUnit
    {
        NANOSECONDS,
        MICROSECONDS,
        MILLISECONDS,
        SECONDS,
        MINUTES,
        HOURS,
        DAYS
    }

    /// <summary>
    /// 时间类型枚举扩展
    /// </summary>
    public static class TimeUnitExtension
    {

        static readonly long C0 = 1L;
        static readonly long C1 = C0 * 1000L;
        static readonly long C2 = C1 * 1000L;
        static readonly long C3 = C2 * 1000L;
        static readonly long C4 = C3 * 60L;
        static readonly long C5 = C4 * 60L;
        static readonly long C6 = C5 * 24L;
        static readonly long MAX = Int64.MaxValue;

        /// <summary>
        /// 转换成纳秒的时间
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static Int64 ToNanos(this TimeUnit unit, Int64 d)
        {
            switch (unit)
            {
                case TimeUnit.NANOSECONDS:
                    return d;
                case TimeUnit.MICROSECONDS:
                    return SaveMultip(d, C1 / C0, MAX / (C1 / C0)); ;
                case TimeUnit.MILLISECONDS:
                    return SaveMultip(d, C2 / C0, MAX / (C2 / C0));
                case TimeUnit.SECONDS:
                    return SaveMultip(d, C3 / C0, MAX / (C3 / C0));
                case TimeUnit.MINUTES:
                    return SaveMultip(d, C4 / C0, MAX / (C4 / C0));
                case TimeUnit.HOURS:
                    return SaveMultip(d, C5 / C0, MAX / (C5 / C0));
                case TimeUnit.DAYS:
                    return SaveMultip(d, C6 / C0, MAX / (C6 / C0));
                default:
                    return 0L;
            }
        }

        /// <summary>
        /// 转换成微秒的时间
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static Int64 ToMicros(this TimeUnit unit, Int64 d)
        {
            switch (unit)
            {
                case TimeUnit.NANOSECONDS:
                    return d / (C1 / C0);
                case TimeUnit.MICROSECONDS:
                    return d;
                case TimeUnit.MILLISECONDS:
                    return SaveMultip(d, C2 / C1, MAX / (C2 / C1));
                case TimeUnit.SECONDS:
                    return SaveMultip(d, C3 / C1, MAX / (C3 / C1));
                case TimeUnit.MINUTES:
                    return SaveMultip(d, C4 / C1, MAX / (C4 / C1));
                case TimeUnit.HOURS:
                    return SaveMultip(d, C5 / C1, MAX / (C5 / C1));
                case TimeUnit.DAYS:
                    return SaveMultip(d, C6 / C1, MAX / (C6 / C1));
                default:
                    return 0L;
            }
        }

        /// <summary>
        /// 转换成毫秒的时间
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static Int64 ToMillis(this TimeUnit unit, Int64 d)
        {
            switch (unit)
            {
                case TimeUnit.NANOSECONDS:
                    return d / (C3 / C0);
                case TimeUnit.MICROSECONDS:
                    return d / (C3 / C1);
                case TimeUnit.MILLISECONDS:
                    return d;
                case TimeUnit.SECONDS:
                    return SaveMultip(d, C3 / C2, MAX / (C3 / C2));
                case TimeUnit.MINUTES:
                    return SaveMultip(d, C4 / C2, MAX / (C4 / C2));
                case TimeUnit.HOURS:
                    return SaveMultip(d, C5 / C2, MAX / (C5 / C2));
                case TimeUnit.DAYS:
                    return SaveMultip(d, C6 / C2, MAX / (C6 / C2));
                default:
                    return 0L;
            }
        }

        /// <summary>
        /// 转换成秒的时间
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static Int64 ToSeconds(this TimeUnit unit, Int64 d)
        {
            switch (unit)
            {
                case TimeUnit.NANOSECONDS:
                    return d / (C3 / C0);
                case TimeUnit.MICROSECONDS:
                    return d / (C3 / C1);
                case TimeUnit.MILLISECONDS:
                    return d / (C3 / C2);
                case TimeUnit.SECONDS:
                    return d;
                case TimeUnit.MINUTES:
                    return SaveMultip(d, C4 / C3, MAX / (C4 / C3));
                case TimeUnit.HOURS:
                    return SaveMultip(d, C5 / C3, MAX / (C5 / C3));
                case TimeUnit.DAYS:
                    return SaveMultip(d, C6 / C3, MAX / (C6 / C3));
                default:
                    return 0L;
            }
        }

        /// <summary>
        /// 转换成分钟的时间
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static Int64 ToMinutes(this TimeUnit unit, Int64 d)
        {
            switch (unit)
            {
                case TimeUnit.NANOSECONDS:
                    return d / (C4 / C0);
                case TimeUnit.MICROSECONDS:
                    return d / (C4 / C1);
                case TimeUnit.MILLISECONDS:
                    return d / (C4 / C2);
                case TimeUnit.SECONDS:
                    return d / (C4 / C3);
                case TimeUnit.MINUTES:
                    return d;
                case TimeUnit.HOURS:
                    return SaveMultip(d, C5 / C4, MAX / (C5 / C4));
                case TimeUnit.DAYS:
                    return SaveMultip(d, C6 / C4, MAX / (C6 / C4));
                default:
                    return 0L;
            }
        }

        /// <summary>
        /// 转换成小时的时间
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static Int64 ToHours(this TimeUnit unit, Int64 d)
        {
            switch (unit)
            {
                case TimeUnit.NANOSECONDS:
                    return d / (C5 / C0);
                case TimeUnit.MICROSECONDS:
                    return d / (C5 / C1);
                case TimeUnit.MILLISECONDS:
                    return d / (C5 / C2);
                case TimeUnit.SECONDS:
                    return d / (C5 / C3);
                case TimeUnit.MINUTES:
                    return d / (C5 / C4); ;
                case TimeUnit.HOURS:
                    return d;
                case TimeUnit.DAYS:
                    return SaveMultip(d, C6 / C5, MAX / (C6 / C5));
                default:
                    return 0L;
            }
        }

        /// <summary>
        /// 转换成天的时间
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static Int64 ToDays(this TimeUnit unit, Int64 d)
        {
            switch (unit)
            {
                case TimeUnit.NANOSECONDS:
                    return d / (C6 / C0);
                case TimeUnit.MICROSECONDS:
                    return d / (C6 / C1);
                case TimeUnit.MILLISECONDS:
                    return d / (C6 / C2);
                case TimeUnit.SECONDS:
                    return d / (C6 / C3);
                case TimeUnit.MINUTES:
                    return d / (C6 / C4);
                case TimeUnit.HOURS:
                    return d / (C6 / C5);
                case TimeUnit.DAYS:
                    return d;
                default:
                    return 0L;
            }
        }

        /// <summary>
        /// 转换日期格式
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="d"></param>
        /// <param name="newUnit"></param>
        /// <returns></returns>
        public static Int64 convert(this TimeUnit unit, Int64 d, TimeUnit newUnit)
        {
            switch (unit)
            {
                case TimeUnit.NANOSECONDS:
                    return newUnit.ToNanos(d);
                case TimeUnit.MICROSECONDS:
                    return newUnit.ToMicros(d);
                case TimeUnit.MILLISECONDS:
                    return newUnit.ToMillis(d);
                case TimeUnit.SECONDS:
                    return newUnit.ToSeconds(d);
                case TimeUnit.MINUTES:
                    return newUnit.ToMinutes(d);
                case TimeUnit.HOURS:
                    return newUnit.ToHours(d);
                case TimeUnit.DAYS:
                    return newUnit.ToDays(d);
                default:
                    return 0L;
            }
        }

        /// <summary>
        /// 以毫秒为单位有限等待
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="obj"></param>
        /// <param name="timeout"></param>
        public static void timedWait(this TimeUnit unit, Object obj, Int32 timeout)
        {
            if (timeout > 0)
            {
                var ms = (int)unit.ToMillis(timeout);
                Monitor.Wait(obj, timeout);
            }
        }

        /// <summary>
        /// 以毫秒为单位join
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="thread"></param>
        /// <param name="timeout"></param>
        public static void timedJoin(this TimeUnit unit, Thread thread, Int32 timeout)
        {
            if (timeout > 0)
            {
                var ms = (int)unit.ToMillis(timeout);
                thread.Join(ms);
            }
        }

        /// <summary>
        /// 以毫秒为单位睡眠
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="timeout"></param>
        public static void sleep(this TimeUnit unit, Int32 timeout)
        {
            if (timeout > 0)
            {
                var ms = (int)unit.ToMillis(timeout);
                Thread.Sleep(ms);
            }
        }

        #region Utils
        /// <summary>
        /// 安全转换 溢出则返回最大或者最小的数据范围
        /// </summary>
        /// <param name="d"></param>
        /// <param name="m"></param>
        /// <param name="over"></param>
        /// <returns></returns>
        private static Int64 SaveMultip(long d, long m, long over)
        {
            if (d > over)
            {
                return Int64.MaxValue;
            }
            if (d < -over)
            {
                return Int64.MinValue;
            }
            return d * m;
        }
        #endregion
    }
}
