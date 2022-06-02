/*
 * 文件放置游戏专用的一些基础类
 * 一些游戏中常用的基础数据结构。
 */
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace OW.Game
{
    /// <summary>
    /// 当日刷新的数据帮助器类。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TodayDataWrapper<T> : IDisposable
    {
        /// <summary>
        /// 最后一次刷新结果的键名后缀。
        /// </summary>
        public const string LastValuesKeySuffix = "LastValues";

        /// <summary>
        /// 当日刷新的所有值的键名后缀。
        /// </summary>
        public const string TodayValuesKeySuffix = "TodayValues";

        /// <summary>
        /// 最后一次刷新日期键名后缀。
        /// </summary>
        public const string LastDateKeySuffix = "LastDate";

        /// <summary>
        /// 分隔符。
        /// </summary>
        public const string Separator = "`";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="prefix"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public static TodayDataWrapper<T> Create([NotNull] Dictionary<string, object> dic, [NotNull] string prefix, DateTime now)
        {
            return new TodayDataWrapper<T>(dic, prefix, now);
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="dic">保存在该对象的<see cref="SimpleExtendPropertyBase.Properties"/>属性中。</param>
        /// <param name="prefix">记录这些属性的前缀。</param>
        /// <param name="now">当前日期时间。</param>
        protected TodayDataWrapper([NotNull] Dictionary<string, object> dic, [NotNull] string prefix, DateTime now)
        {
            _Dictionary = dic;
            _Prefix = prefix;
            _Now = now;
        }

        private TypeConverter _Converter;

        /// <summary>
        /// 类型的转换器。
        /// </summary>
        public TypeConverter Converter => _Converter ??= TypeDescriptor.GetConverter(typeof(T));

        private Dictionary<string, object> _Dictionary;
        private string _Prefix;
        private readonly DateTime _Now;
        private string _LastValuesKey;

        /// <summary>
        /// 记录最后一次值的键名。
        /// </summary>
        public string LastValuesKey => _LastValuesKey ??= $"{_Prefix}{LastValuesKeySuffix}";

        private string _TodayValuesKey;
        /// <summary>
        /// 记录当日值的键名。
        /// </summary>
        public string TodayValuesKey => _TodayValuesKey ??= $"{_Prefix}{TodayValuesKeySuffix}";

        private string _LastDateKey;
        /// <summary>
        /// 最后刷新时间键名。
        /// </summary>
        public string LastDateKey => _LastDateKey ??= $"{_Prefix}{LastDateKeySuffix}";


        private List<T> _TodayValues;

        /// <summary>
        /// 今日所有数据。
        /// </summary>
        public List<T> TodayValues
        {
            get
            {
                if (_TodayValues is null)
                    if (!_Dictionary.ContainsKey(LastDateKey) || _Dictionary.GetDateTimeOrDefault(LastDateKey).Date != _Now.Date)  //若已经需要刷新
                    {
                        _TodayValues = new List<T>();
                    }
                    else //若当日有数据
                    {
                        string val = _Dictionary.GetStringOrDefault(TodayValuesKey);
                        if (string.IsNullOrWhiteSpace(val))  //若没有值
                            _TodayValues = new List<T>();
                        else
                        {
                            var converter = Converter;
                            _TodayValues = val.Split(Separator).Select(c => (T)converter.ConvertFrom(c)).ToList();
                        }
                    }
                return _TodayValues;
            }
        }

        private List<T> _LastValues;
        /// <summary>
        /// 最后一次刷新的数据。
        /// </summary>
        public List<T> LastValues
        {
            get
            {
                if (_LastValues is null)
                {
                    if (!_Dictionary.ContainsKey(LastDateKey) || _Dictionary.GetDateTimeOrDefault(LastDateKey).Date != _Now.Date)  //若已经需要刷新
                    {
                        _LastValues = new List<T>();
                    }
                    else //若最后一次的数据有效
                    {
                        string val = _Dictionary.GetStringOrDefault(LastValuesKey);
                        if (string.IsNullOrWhiteSpace(val))  //若没有值
                            _LastValues = new List<T>();
                        else //若有值
                        {
                            var converter = Converter;
                            _LastValues = val.Split(Separator).Select(c => (T)converter.ConvertFrom(c)).ToList();
                        }
                    }
                }
                return _LastValues;
            }
        }

        /// <summary>
        /// 当日是否有数据。
        /// 直到调用<see cref="Save"/>后此属性才会变化。
        /// </summary>
        public virtual bool HasData => _Dictionary.GetDateTimeOrDefault(LastDateKey, DateTime.MinValue).Date == _Now.Date;

        /// <summary>
        /// 本类成员不使用该属性，调用者可以用来记录一些信息。
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// 获取或创建最后一次的刷新数据。
        /// 用<see cref="HasData"/>属性判定是否需要刷新。刷新的数据自动合并到<see cref="TodayValues"/>中。
        /// </summary>
        /// <param name="cretor">刷新得到新数据的回调。</param>
        /// <returns>最后一次刷新的数据或是新数据。</returns>
        public IEnumerable<T> GetOrAddLastValues(Func<IEnumerable<T>> creator)
        {
            if (HasData)
                return LastValues;
            var coll = creator();
            LastValues.Clear();
            LastValues.AddRange(coll);
            TodayValues.AddRange(coll);
            return LastValues;
        }

        /// <summary>
        /// 保存数据到字典中。
        /// </summary>
        public void Save()
        {
            _Dictionary[LastDateKey] = _Now.ToString("s");

            var converter = Converter;
            _Dictionary[LastValuesKey] = string.Join(Separator, LastValues.Select(c => converter.ConvertToString(c)));
            _Dictionary[TodayValuesKey] = string.Join(Separator, TodayValues.Select(c => converter.ConvertToString(c)));
        }

        #region IDisposable接口及相关

        private bool _Disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _TodayValues = null;
                _LastValues = null;
                _Dictionary = null;
                _Prefix = null;
                _Converter = null;
                _LastDateKey = null;
                _LastValues = null;
                _LastValuesKey = null;
                Tag = null;

                _Disposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~TodayDataWrapper()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable接口及相关
    }

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
            if (increment > 0 && maxVal < currentVal || increment < 0 && maxVal > currentVal)  //若不向终值收敛
                Debug.WriteLine("不向终值收敛。");
            _LastValue = currentVal;
            LastDateTime = lastComputerDateTime;
            Delay = delay;
            Increment = increment;
            MaxValue = maxVal;
        }

        #region 属性及相关

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

        /// <summary>
        /// 多久计算一次。
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// 每次跳点的增量。
        /// </summary>
        public decimal Increment { get; set; }

        /// <summary>
        /// 一个记录额外信息的属性。本类成员不使用该属性。
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// 获取指示该渐变属性是否已经完成。会更新计算时间。
        /// </summary>
        public bool IsComplate => GetCurrentValueWithUtc() >= MaxValue;

        #endregion 属性及相关

        /// <summary>
        /// 获取当前值。自动修改<see cref="LastComputerDateTime"/>和<see cref="LastValue"/>属性。
        /// </summary>
        /// <param name="now">当前时间。返回时可能更改，如果没有正好到跳变时间，则会提前到上一次跳变的时间点。</param>
        /// <returns>更改后的值(<see cref="LastValue"/>)。</returns>
        public decimal GetCurrentValue(ref DateTime now)
        {
            var count = (long)Math.Round((decimal)(now - LastDateTime).Ticks / Delay.Ticks, MidpointRounding.ToNegativeInfinity);   //跳变次数,回调可能多跳一次
            LastDateTime += Delay * count;
            now = LastDateTime;
            if (_LastValue >= MaxValue)  //若已经结束
            {
                return _LastValue;
            }
            else //若尚未结束
            {
                _LastValue = Math.Clamp(_LastValue + count * Increment, decimal.Zero, MaxValue);
            }
            return _LastValue;
        }

        /// <summary>
        /// 以当前<see cref="LastValue"/>为准预估完成时间点。
        /// </summary>
        /// <returns>预估完成时间。不会刷新计算最新值。</returns>
        public DateTime GetComplateDateTime()
        {
            if (_LastValue >= MaxValue)  //若已经结束
            {
                return LastDateTime;
            }

            var count = (long)Math.Round((MaxValue - _LastValue) / Increment, MidpointRounding.ToPositiveInfinity);  //到结束还需跳变多少次
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
                    if (!OwConvert.TryToDecimal(val, out var dec))
                    {
                        return false;
                    }

                    Increment = dec;
                    break;
                case 'd':
                    if (!OwConvert.TryToDecimal(val, out dec))
                    {
                        return false;
                    }

                    Delay = TimeSpan.FromSeconds((double)dec);
                    break;
                case 'm':
                    if (!OwConvert.TryToDecimal(val, out dec))
                    {
                        return false;
                    }

                    MaxValue = dec;
                    break;
                case 'c':   //当前刷新后的最后值
                    if (!OwConvert.TryToDecimal(val, out dec))
                    {
                        return false;
                    }

                    _LastValue = dec;
                    LastDateTime = DateTime.UtcNow;
                    break;
                case 'l':
                    if (!OwConvert.TryToDecimal(val, out dec))
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

    public static class GameHelper
    {
        /// <summary>
        /// 随机获取指定数量的元素。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <param name="random"></param>
        /// <param name="count">不可大于<paramref name="src"/>中元素数。</param>
        /// <returns></returns>
        public static IEnumerable<T> GetRandom<T>(IList<T> src, Random random, int count)
        {
            if (count > src.Count)
                throw new InvalidOperationException();
            else if (count == src.Count)
                return src;
            var tmp = count;
            var ary = ArrayPool<int>.Shared.Rent(count);
            for (int i = 0; i < count; i++)
            {
                ary[i] = random.Next(src.Count);
            }
            var result = new T[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = src[ary[i]];
            }
            return result;
        }
    }
}