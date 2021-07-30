/*
 * 文件放置游戏专用的一些基础类
 */
using System;
using System.Runtime.CompilerServices;

namespace OW.Game
{
    /// <summary>
    /// 渐变属性封装类。
    /// </summary>
    public class FastChangingProperty
    {
        /// <summary>
        /// 构造函数、
        /// </summary>
        /// <param name="delay">计算间隔。</param>
        /// <param name="increment">增量。</param>
        /// <param name="maxVal">最大值。不会超过此值。</param>
        /// <param name="currentVal">当前值。</param>
        /// <param name="lastComputerDateTime">时间。建议一律采用Utc时间。</param>
        public FastChangingProperty(TimeSpan delay, decimal increment, decimal maxVal, decimal currentVal, DateTime lastComputerDateTime)
        {
            _LastValue = currentVal;
            LastDateTime = lastComputerDateTime;
            Delay = delay;
            Increment = increment;
            MaxValue = maxVal;
        }

        /// <summary>
        /// 自动跳变到的最大值。
        /// </summary>
        public decimal MaxValue { get; set; }

        /// <summary>
        /// 获取或设置最后计算的时间。建议一律采用Utc时间。默认值是构造时的当前时间。
        /// </summary>
        public DateTime LastDateTime { get; set; } = DateTime.UtcNow;

        private decimal _LastValue;

        /// <summary>
        /// 获取或设置最后计算的结果。<see cref="LastComputerDateTime"/>这个时点上计算的值。
        /// </summary>
        public decimal LastValue { get => _LastValue; set => _LastValue = value; }

        ///// <summary>
        ///// 设置最后计算的值和时间，并根据值是否大于或等于最大值，引发事件。
        ///// 如果已经结束则不会引发事件，只有当设置之前未结束，设置之后结束才会引发事件。
        ///// </summary>
        ///// <param name="lastValue"></param>
        ///// <param name="lastDateTime"></param>
        ///// <param name="maxValue">设置的最大值，省略或空则不会设置最大值<see cref="MaxValue"/>属性。</param>
        ///// <returns>true引发了事件，false未引发事件。</returns>
        //public bool SetAndRaiseEvent(decimal lastValue, DateTime lastDateTime, decimal? maxValue = null)
        //{
        //    DateTime dt = DateTime.UtcNow;
        //    var isComplete = LastValue >= MaxValue || ComputeComplateDateTime() <= dt;
        //    LastValue = lastValue;
        //    LastDateTime = lastDateTime;
        //    if (maxValue.HasValue)
        //    {
        //        MaxValue = maxValue.Value;
        //    }

        //    if (LastValue < MaxValue && !isComplete)   //若需引发事件
        //    {
        //        OnCompleted(new CompletedEventArgs(lastDateTime));
        //        return true;
        //    }
        //    return false;
        //}

        /// <summary>
        /// 多久计算一次。
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// 每次计算的增量。
        /// </summary>
        public decimal Increment { get; set; }

        /// <summary>
        /// 获取当前值。自动修改<see cref="LastComputerDateTime"/>和<see cref="LastValue"/>属性。
        /// </summary>
        /// <param name="now">当前时间。返回时可能更改，如果没有正好到跳变时间，则会略微提前到上一次跳变的时间点。</param>
        /// <returns>更改后的值(<see cref="LastValue"/>)。</returns>
        public decimal GetCurrentValue(ref DateTime now)
        {
            if (_LastValue >= MaxValue)  //若已经结束
            {
                LastDateTime = now;
                return _LastValue;
            }
            var count = Math.DivRem((now - LastDateTime).Ticks, Delay.Ticks, out long remainder);  //跳变次数 和 余数
            var val = Math.Min(count * Increment + _LastValue, MaxValue);
            _LastValue = val; //计算得到最后值
            now = now - TimeSpan.FromTicks(remainder);
            LastDateTime = now;
            if (_LastValue >= MaxValue)
            {
                OnCompleted(new CompletedEventArgs(now));
            }

            return _LastValue;
        }

        /// <summary>
        /// 以当前<see cref="LastValue"/>为准预估完成时间点。
        /// </summary>
        /// <returns>预估完成时间。不会刷新计算最新值。</returns>
        public DateTime ComputeComplateDateTime()
        {
            if (_LastValue >= MaxValue)  //若已经结束
            {
                return LastDateTime;
            }

            var count = (long)Math.Round((MaxValue - _LastValue) / Increment, MidpointRounding.AwayFromZero);  //到结束还需跳变多少次
            return LastDateTime.AddTicks(Delay.Ticks * count);
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
        /// 名字，本类成员不使用该属性。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 一个记录额外信息的属性。本类成员不使用该属性。
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// 获取指示该渐变属性是否已经完成。会更新计算时间。
        /// </summary>
        public bool IsComplate => GetCurrentValueWithUtc() >= MaxValue;

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
                    {
                        return false;
                    }

                    Increment = dec;
                    break;
                case 'd':
                    if (!OwHelper.TryGetDecimal(val, out dec))
                    {
                        return false;
                    }

                    Delay = TimeSpan.FromSeconds((double)dec);
                    break;
                case 'm':
                    if (!OwHelper.TryGetDecimal(val, out dec))
                    {
                        return false;
                    }

                    MaxValue = dec;
                    break;
                case 'c':   //当前刷新后的最后值
                    if (!OwHelper.TryGetDecimal(val, out dec))
                    {
                        return false;
                    }

                    _LastValue = dec;
                    LastDateTime = DateTime.UtcNow;
                    break;
                case 'l':
                    if (!OwHelper.TryGetDecimal(val, out dec))
                    {
                        return false;
                    }

                    _LastValue = dec;
                    break;
                case 't':
                    if (!DateTime.TryParse(val as string, out var dt))
                    {
                        return false;
                    }

                    LastDateTime = dt;
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
                case 'i':   //增量
                    result = Increment;
                    break;
                case 'd':   //增量间隔，单位:秒
                    result = (decimal)Delay.TotalSeconds;
                    break;
                case 'm':   //最大值
                    result = MaxValue;
                    break;
                case 'c':   //当前刷新后的最后值
                    result = GetCurrentValueWithUtc();
                    break;
                case 'l':   //最后计算结果值
                    result = _LastValue;
                    break;
                case 't':   //最后计算时间点
                    GetCurrentValueWithUtc();
                    result = LastDateTime.ToString("s");
                    break;
                default:
                    result = default;
                    return false;
            }
            return true;
        }

        #region 事件及相关

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeOnCompleted(CompletedEventArgs e)
        {
            OnCompleted(e);
        }

        public event EventHandler<CompletedEventArgs> Completed;

        /// <summary>
        /// 在直接或间接调用<see cref="GetCurrentValue(ref DateTime)"/>时，如果计算状态由未完成变为完成则引发该事件。
        /// </summary>
        /// <remarks>如果使用未完成时间计算后，再用完成的时间点计算，如此返回将每次都引发事件。
        /// 处理函数最好是各种管理器的实例成员。因为需要并发锁定，否则行为未知。</remarks>
        /// <param name="e"></param>
        protected virtual void OnCompleted(CompletedEventArgs e)
        {
            Completed?.Invoke(this, e);
        }

        /// <summary>
        /// 设置最后计算得到的值，同时将计算时间更新到最接近指定点的时间。
        /// </summary>
        /// <param name="val">这个时间点不晚于指定时间点，且又是正好一跳的时间点。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLastValue(decimal val, ref DateTime dateTime)
        {
            var remainder = (dateTime - LastDateTime).Ticks % Delay.Ticks;
            LastDateTime = dateTime.AddTicks(-remainder);
            if (LastDateTime > dateTime)    //若时间点超过指定值
                LastDateTime -= Delay;
            dateTime = LastDateTime;
            _LastValue = val;
        }
        #endregion 事件及相关
    }

    /// <summary>
    /// <see cref="FastChangingProperty.Completed"/>事件所用参数。
    /// </summary>
    public class CompletedEventArgs : EventArgs
    {
        public CompletedEventArgs(DateTime completedDateTime)
        {
            CompletedDateTime = completedDateTime;
        }

        /// <summary>
        /// 获取或设置完成的时间点。
        /// </summary>
        public DateTime CompletedDateTime { get; set; }

    }

}