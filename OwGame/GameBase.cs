/*
 * 文件放置游戏专用的一些基础类
 */
using System;
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
        /// <param name="oriVal">当前值。</param>
        /// <param name="now">时间。建议一律采用Utc时间。</param>
        /// <param name="delay">计算间隔。</param>
        /// <param name="increment">增量。</param>
        /// <param name="maxVal">最大值。不会超过此值。</param>
        public FastChangingProperty(decimal oriVal, DateTime now, TimeSpan delay, decimal increment, decimal maxVal)
        {
            LastValue = oriVal;
            LastComputerDateTime = now;
            Delay = delay;
            Increment = increment;
            MaxValue = maxVal;
        }

        public decimal MaxValue { get; set; }

        /// <summary>
        /// 获取或设置最后计算的时间。建议一律采用Utc时间。
        /// </summary>
        public DateTime LastComputerDateTime { get; set; }

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
        public bool IsComplate => LastValue == MaxValue;
    }

}