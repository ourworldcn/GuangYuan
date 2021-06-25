/*
 * 文件放置游戏专用的一些基础类
 */
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OwGame
{
    /// <summary>
    /// 渐变属性封装类。
    /// </summary>
    public class FastChangingProperty
    {
        /// <summary>
        /// 构造函数、
        /// </summary>
        /// <param name="currentVal">当前值。</param>
        /// <param name="lastComputerDateTime">时间。建议一律采用Utc时间。</param>
        /// <param name="delay">计算间隔。</param>
        /// <param name="increment">增量。</param>
        /// <param name="maxVal">最大值。不会超过此值。</param>
        public FastChangingProperty(decimal currentVal, DateTime lastComputerDateTime, TimeSpan delay, decimal increment, decimal maxVal)
        {
            LastValue = currentVal;
            LastComputerDateTime = lastComputerDateTime;
            Delay = delay;
            Increment = increment;
            MaxValue = maxVal;
        }

        public decimal MaxValue { get; set; }

        /// <summary>
        /// 获取或设置最后计算的时间。建议一律采用Utc时间。默认值是构造时的当前时间。
        /// </summary>
        public DateTime LastComputerDateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 获取或设置最后计算的结果。
        /// </summary>
        public decimal LastValue { get; set; }

        /// <summary>
        /// 多久计算一次。
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// 增量。
        /// </summary>
        public decimal Increment { get; set; }

        /// <summary>
        /// 获取当前值。自动修改LastComputerDateTime和LastValue属性。
        /// </summary>
        /// <param name="now">当前时间。返回时可能更改，如果没有正好到跳变时间，则会略微提前到上一次跳变的时间点。</param>
        /// <returns>当前值。</returns>
        public decimal GetCurrentValue(ref DateTime now)
        {
            if (LastValue >= MaxValue)  //若已经结束
            {
                LastComputerDateTime = now;
                return LastValue;
            }
            var count = Math.DivRem((now - LastComputerDateTime).Ticks, Delay.Ticks, out long remainder);  //跳变次数 和 余数
            var val = Math.Min(count * Increment + LastValue, MaxValue);
            LastValue = val; //计算得到最后值
            now = now - TimeSpan.FromTicks(remainder);
            LastComputerDateTime = now;
            return LastValue;
        }

        /// <summary>
        /// 使用当前Utc时间获取当前值。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal GetCurrentValueWithUtc()
        {
            DateTime now = DateTime.UtcNow;
            return GetCurrentValue(ref now);
        }

        /// <summary>
        /// 一个记录额外信息的属性。本类成员不使用该属性。
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// 获取指示该渐变属性是否已经完成。
        /// </summary>
        public bool IsComplate => GetCurrentValueWithUtc() >= MaxValue;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        static public void ToDictionary(FastChangingProperty obj, IDictionary<string, object> dic, string name)
        {
            dic[$"{ClassPrefix}i{name}"] = obj.Increment;
            dic[$"{ClassPrefix}d{name}"] = obj.Delay.TotalSeconds;
            dic[$"{ClassPrefix}m{name}"] = obj.MaxValue;
            dic[$"{ClassPrefix}c{name}"] = obj.LastValue;
            dic[$"{ClassPrefix}t{name}"] = obj.LastComputerDateTime.ToString("s");
        }

        static public FastChangingProperty FromDictionary(IReadOnlyDictionary<string, object> dic, string name)
        {
            OwHelper.TryGetDecimal(dic[$"{ClassPrefix}i{name}"], out var pi);
            OwHelper.TryGetDecimal(dic[$"{ClassPrefix}d{name}"], out var pd);
            OwHelper.TryGetDecimal(dic[$"{ClassPrefix}m{name}"], out var pm);
            OwHelper.TryGetDecimal(dic[$"{ClassPrefix}c{name}"], out var pc);
            DateTime.TryParse(dic[$"{ClassPrefix}t{name}"] as string, out var pt);
            return new FastChangingProperty(pc, pt, TimeSpan.FromSeconds((double)pd), pi, pm);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyNamePrefix">i设置增量，d设置计算间隔。</param>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool SetPropertyValue(char propertyNamePrefix, object val)
        {
            switch (propertyNamePrefix)
            {
                case 'i':
                    if (!OwHelper.TryGetDecimal(val, out var dec))
                        return false;
                    Increment = dec;
                    break;
                case 'd':
                    if (!OwHelper.TryGetDecimal(val, out dec))
                        return false;
                    Delay = TimeSpan.FromSeconds((double)dec);
                    break;
                case 'm':
                    if (!OwHelper.TryGetDecimal(val, out dec))
                        return false;
                    MaxValue = dec;
                    break;
                case 'l':
                    if (!OwHelper.TryGetDecimal(val, out dec))
                        return false;
                    LastValue = dec;
                    break;
                case 't':
                    if (!DateTime.TryParse(val as string, out var dt))
                        return false;
                    LastComputerDateTime = dt;
                    break;
                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyNamePrefix"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetPropertyValue(char propertyNamePrefix, out object result)
        {
            switch (propertyNamePrefix)
            {
                case 'i':
                    result = Increment;
                    break;
                case 'd':
                    result = (decimal)Delay.TotalSeconds;
                    break;
                case 'm':
                    result = MaxValue;
                    break;
                case 'c':
                    result = GetCurrentValueWithUtc();
                    break;
                case 'l':
                    result = LastValue;
                    break;
                case 't':
                    GetCurrentValueWithUtc();
                    result = LastComputerDateTime.ToString("s");
                    break;
                default:
                    result = default;
                    return false;
            }
            return true;
        }

        public const string ClassPrefix = "fcp";
    }

}