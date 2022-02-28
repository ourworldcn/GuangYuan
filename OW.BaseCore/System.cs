/*
 * 包含一些简单的类。
 */
using System.Linq;

namespace System
{
    /// <summary>
    /// 支持一种文字化显示时间间隔及不确定间隔的类。
    /// 如1m，1y分别表示一月和一年，这些都是不确定时长度的间间隔，但在实际应用中却常有需求。
    /// </summary>
    public readonly struct TimeSpanEx
    {
        public bool TryParse(string str, out TimeSpanEx result)
        {
            var u = str.Last();
            if (!int.TryParse(str[..^1], out var v))
            {
                result = default;
                return false;
            }
            result = new TimeSpanEx(v, u);
            return true;
        }

        public TimeSpanEx(string str)
        {
            Value = int.Parse(str[..^1]);
            Unit = str[^1];
        }

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
        /// 表示时间长度单位，支持，s秒，d天，w周，m月，y年
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

}