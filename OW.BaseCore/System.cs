/*
 * 包含一些简单的类。
 */
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System
{
    /// <summary>
    /// 支持一种文字化显示时间间隔及不确定间隔的类。支持：s秒，d天，w周，m月，y年
    /// 如1m，1y分别表示一月和一年，这些都是不确定时长度的间间隔，但在实际应用中却常有需求。
    /// </summary>
    public readonly struct TimeSpanEx
    {
        /// <summary>
        /// 支持的单位符号。
        /// </summary>
        public const string UnitChars = "sdwmy";

        public bool TryParse([NotNull] string str, [MaybeNullWhen(false)] out TimeSpanEx result)
        {
            var u = str[^1];
            if (!UnitChars.Contains(u))
            {
                result = default;
                return false;
            }
            if (!int.TryParse(str[..^1], out var v))
            {
                result = default;
                return false;
            }
            result = new TimeSpanEx(v, u);
            return true;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="str"></param>
        public TimeSpanEx(string str)
        {
            Value = int.Parse(str[..^1]);
            Unit = str[^1];
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="unit"></param>
        public TimeSpanEx(int value, char unit)
        {
            Value = value;
            Unit = unit;
        }

        /// <summary>
        /// 数值。
        /// </summary>
        public readonly int Value;

        /// <summary>
        /// 表示时间长度单位，支持：s秒，d天，w周，m月，y年
        /// </summary>
        public readonly char Unit;

        /// <summary>
        /// 重载计算加法的运算符。
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static DateTime operator +(DateTime dt, TimeSpanEx ts)
        {
            return ts + dt;
        }

        /// <summary>
        /// 重载计算加法的运算符。
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime operator +(TimeSpanEx ts, DateTime dt)
        {
            DateTime result;
            switch (ts.Unit)
            {
                case 's':
                    result = dt + TimeSpan.FromSeconds(ts.Value);
                    break;
                case 'd':
                    result = dt + TimeSpan.FromDays(ts.Value);
                    break;
                case 'w':
                    result = dt + TimeSpan.FromDays(ts.Value * 7);
                    break;
                case 'm':
                    result = dt.AddMonths(ts.Value);
                    break;
                case 'y':
                    result = dt.AddYears(ts.Value);
                    break;
                default:
                    throw new InvalidOperationException($"{ts.Unit}不是有效字符。");
            }
            return result;
        }
    }

    /// <summary>
    /// 指定起始时间的周期对象。
    /// </summary>
    public class DateTimePeriod
    {
        public DateTimePeriod()
        {

        }

        public DateTimePeriod(DateTime startDateTime, TimeSpanEx period)
        {
            StartDateTime = startDateTime;
            Period = period;
        }

        /// <summary>
        /// 起始时间。
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// 周期。
        /// </summary>
        public TimeSpanEx Period { get; set; }

        /// <summary>
        /// 获取指定时间所处周期的起始时间点。
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public DateTime GetPeriodStart(DateTime dt)
        {
            DateTime start; //最近一个周期的开始时间
            switch (Period.Unit)
            {
                case 'n':   //无限
                    start = StartDateTime;
                    break;
                case 's':
                    var times = (dt - StartDateTime).Ticks / TimeSpan.FromSeconds(Period.Value).Ticks;  //相隔秒数
                    start = StartDateTime.AddTicks(times * TimeSpan.FromSeconds(Period.Value).Ticks);
                    break;
                case 'd':   //日周期
                    times = (dt - StartDateTime).Ticks / TimeSpan.FromDays(Period.Value).Ticks;  //相隔日数
                    start = StartDateTime.AddTicks(times * TimeSpan.FromDays(Period.Value).Ticks);
                    break;
                case 'w':   //周周期
                    times = (dt - StartDateTime).Ticks / TimeSpan.FromDays(7 * Period.Value).Ticks;  //相隔周数
                    start = StartDateTime.AddTicks(TimeSpan.FromDays(7 * Period.Value).Ticks * times);
                    break;
                case 'm':   //月周期
                    DateTime tmp;
                    for (tmp = StartDateTime; tmp <= dt; tmp = tmp.AddMonths(Period.Value))
                    {
                    }
                    start = tmp.AddMonths(-Period.Value);
                    break;
                case 'y':   //年周期
                    for (tmp = StartDateTime; tmp <= dt; tmp = tmp.AddYears(Period.Value))
                    {
                    }
                    start = tmp.AddYears(-Period.Value);
                    break;
                default:
                    throw new InvalidOperationException("无效的周期表示符。");
            }
            return start;

        }

    }

    /// <summary>
    /// 唯一字符串的全局锁的帮助类。
    /// 比较字符串的方法是<see cref="StringComparison.InvariantCulture"/>_使用区分区域性的排序规则和固定区域性比较字符串。
    /// </summary>
    public static class StringLocker
    {
        static readonly ConcurrentDictionary<string, string> _Data = new ConcurrentDictionary<string, string>(StringComparer.InvariantCulture);

        /// <summary>
        /// 清理字符串拘留池中没有锁定的对象。
        /// </summary>
        public static void TrimExcess()
        {
            string uniStr;
            foreach (var item in _Data.Keys)
            {
                uniStr = IsInterned(item);
                if (uniStr is null && !Monitor.TryEnter(uniStr, TimeSpan.Zero))
                    continue;
                try
                {
                    if (ReferenceEquals(IsInterned(uniStr), uniStr))
                        _Data.TryRemove(uniStr, out _);
                }
                finally
                {
                    Monitor.Exit(uniStr);
                }
            }
        }

        /// <summary>
        /// 如果 str 在暂存池中，则返回对它的引用；否则返回 null。
        /// </summary>
        /// <param name="str">测试值相等的字符串。</param>
        /// <returns>如果 str 值相等的实例在暂存池中，则返回池中对象的引用；否则返回 null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string IsInterned(string str) => _Data.TryGetValue(str, out var tmp) ? tmp : null;

        /// <summary>
        /// 检索对指定 String 的引用。
        /// </summary>
        /// <param name="str"></param>
        /// <returns>如果暂存了 str值相等的实例在暂存池中，则返回池中的引用；否则返回对值为 str 的字符串的新引用，并加入池中。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Intern(string str) => _Data.GetOrAdd(str, str);

        /// <summary>
        /// 锁定字符串在当前应用程序域内的唯一实例。
        /// </summary>
        /// <param name="str">试图锁定的字符串的值，返回时可能变为池中原有对象，或无变化，锁是加在该对象上的</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryEnter(ref string str, TimeSpan timeout)
        {
            str = Intern(str);
            var start = DateTime.UtcNow;
            if (!Monitor.TryEnter(str, timeout))
                return false;
            while (!ReferenceEquals(str, IsInterned(str)))
            {
                Monitor.Exit(str);
                var tmp = OwHelper.ComputeTimeout(start, timeout);
                if (tmp == TimeSpan.Zero)   //若超时
                    return false;
                str = Intern(str);
                if (!Monitor.TryEnter(str, tmp))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// <seealso cref="TryEnter(ref string, TimeSpan)"/>
        /// </summary>
        /// <param name="str"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnter(string str, TimeSpan timeout) => TryEnter(ref str, timeout);

        /// <summary>
        /// <seealso cref="Monitor.IsEntered(object)"/>
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsEntered(string str)
        {
            str = IsInterned(str);
            if (str is null)
                return false;
            return Monitor.IsEntered(str);
        }

        /// <summary>
        /// 在字符串在当前应用程序域内的唯一实例上进行解锁。
        /// </summary>
        /// <param name="str"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Exit(string str)
        {
            var uniStr = IsInterned(str);
            Monitor.Exit(uniStr);
        }
    }
}