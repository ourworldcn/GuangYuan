using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace OW.Game.Log
{
    /// <summary>
    /// 记录一些小型数据可表述的操作的数据结构类。
    /// </summary>
    public class SmallGameLog
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SmallGameLog()
        {

        }

        /// <summary>
        /// 发生的时间点。
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// 操作标识。
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// 操作的相关的参数或结果记录。
        /// </summary>
        public List<string> Params { get; set; } = new List<string>();

    }

    /// <summary>
    /// 用于记录一些操作结果的日志，可能影响到后续的操作条件，如购买物品在周期内的限定。
    /// </summary>
    public class SmallGameLogCollection : Collection<SmallGameLog>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str">空引用或空字符串将立即返回一个空对象。</param>
        /// <returns></returns>
        public static SmallGameLogCollection Parse([AllowNull] string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return new SmallGameLogCollection();
            return (SmallGameLogCollection)JsonSerializer.Deserialize(Uri.UnescapeDataString(str), typeof(SmallGameLogCollection));
        }

        /// <summary>
        /// 分析存储在字典中的数据以获取<see cref="SimpleGameLogCollection"/>对象。
        /// </summary>
        /// <param name="dic">字典，将记录在<see cref="Dictionary"/>中。</param>
        /// <param name="key">字典中的键值，将记录在<see cref="KeyName"/>中。</param>
        /// <returns></returns>
        public static SmallGameLogCollection Parse(IDictionary<string, object> dic, string key)
        {
            if (!dic.TryGetValue(key, out var obj) || !(obj is string str))
                str = null;
            var result = Parse(str);
            result.Dictionary = dic;
            result.KeyName = key;
            return result;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public SmallGameLogCollection()
        {
        }

        /// <summary>
        /// 绑定的字典。
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, object> Dictionary { get; set; }

        /// <summary>
        /// 字典中的键名。
        /// </summary>
        [JsonIgnore]
        public string KeyName { get; set; }

        /// <summary>
        /// 保存到指定的字典数据中。
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="keyName"></param>
        public virtual void Save(IDictionary<string, object> dic, string keyName)
        {
            //MemoryStream ms;
            //using (ms = new MemoryStream())
            //{
            //    using BrotliStream bs = new BrotliStream(ms, CompressionMode.Compress);
            //    using var u8w = new Utf8JsonWriter(bs);
            //    JsonSerializer.Serialize(u8w, this);
            //}
            //if (ms.TryGetBuffer(out var buffer))
            //    Convert.ToBase64String(buffer);
            dic[keyName] = Uri.EscapeDataString(JsonSerializer.Serialize(this));
        }

        /// <summary>
        /// 保存类内信息。需要正确设置<see cref="Dictionary"/>和<see cref="KeyName"/>属性。
        /// </summary>
        public void Save()
        {
            Save(Dictionary, KeyName);
        }

        public SmallGameLog Add(string action, Guid id, decimal count)
        {
            var tmp = new SmallGameLog() { Action = action, DateTime = DateTime.UtcNow };
            tmp.Params.Add(id.ToString());
            tmp.Params.Add(count.ToString());
            Add(tmp);
            return tmp;
        }

        public IEnumerable<SmallGameLog> Get(DateTime today)
        {
            return this.Where(c => c.DateTime.Date == today.Date);
        }
    }

    /// <summary>
    /// 当日刷新的数据帮助器类。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TodayDataLog<T> : IDisposable
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
        /// <param name="date"></param>
        /// <returns></returns>
        public static TodayDataLog<T> Create([NotNull] Dictionary<string, object> dic, [NotNull] string prefix, DateTime date)
        {
            return new TodayDataLog<T>(dic, prefix, date);
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="dic">保存在该对象的<see cref="SimpleExtendPropertyBase.Properties"/>属性中。</param>
        /// <param name="prefix">记录这些属性的前缀。</param>
        /// <param name="date">当前日期时间。</param>
        protected TodayDataLog([NotNull] Dictionary<string, object> dic, [NotNull] string prefix, DateTime date)
        {
            _Dictionary = dic;
            _Prefix = prefix;
            _Now = date;
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
        // ~TodayDataLog()
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
    /// 特定记录今日数据，及最后一次刷新数据的数据结构。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TodayTimeGameLog<T> : SmallGameLogCollection
    {
        /// <summary>
        /// 最后一次刷新结果的键名后缀。
        /// </summary>
        public const string LastValuesKeyName = "LastValues";

        /// <summary>
        /// 当日刷新的所有值的键名后缀。
        /// </summary>
        public const string TodayValuesKeyName = "TodayValues";

        /// <summary>
        /// 从指定字典的指定键的值中获取对象。
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key">若键不存在或不是字符串类型则返回空对象。</param>
        /// <returns></returns>
        public new static TodayTimeGameLog<T> Parse(IDictionary<string, object> dic, string key)
        {
            if (!dic.TryGetValue(key, out var obj) || !(obj is string str))
                str = null;
            var result = Parse(str);
            result.Dictionary = dic;
            result.KeyName = key;
            return result;
        }

        /// <summary>
        /// 从指定字符串中分析获取对象。
        /// </summary>
        /// <param name="str">空引用或空字符串将立即返回一个空对象。被转义后的字符串。</param>
        /// <returns></returns>
        public new static TodayTimeGameLog<T> Parse([AllowNull] string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return new TodayTimeGameLog<T>();
            return (TodayTimeGameLog<T>)JsonSerializer.Deserialize(Uri.UnescapeDataString(str), typeof(TodayTimeGameLog<T>));
        }

        static TypeConverter _Converter;
        /// <summary>
        /// 类型的转换器。
        /// </summary>
        [JsonIgnore]
        public static TypeConverter Converter
        {
            get
            {
                if (_Converter is null)
                    Interlocked.CompareExchange(ref _Converter, TypeDescriptor.GetConverter(typeof(T)), null);
                return _Converter;
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public TodayTimeGameLog()
        {

        }

        /// <summary>
        /// 获取今日数据的全部集合。
        /// </summary>
        /// <param name="date"></param>
        /// <returns>可能返回空集合，但不会返回空引用。</returns>
        public IEnumerable<T> GetTodayData(DateTime date)
        {
            return this.FirstOrDefault(c => c.DateTime.Date == date.Date && c.Action == TodayValuesKeyName)?.Params.Select(c => (T)Converter.ConvertFrom(c)) ?? Array.Empty<T>();
        }

        /// <summary>
        /// 获取今日最后一次的数据。
        /// </summary>
        /// <param name="date"></param>
        /// <returns>可能返回空集合，但不会返回空引用。</returns>
        public IEnumerable<T> GetLastData(DateTime date)
        {
            return this.FirstOrDefault(c => c.DateTime.Date == date.Date && c.Action == LastValuesKeyName)?.Params.Select(c => (T)Converter.ConvertFrom(c)) ?? Array.Empty<T>();
        }

        /// <summary>
        /// 追加最后一次的数据，同时加入指定日期的数据。
        /// </summary>
        /// <param name="data"></param>
        public void AddLastData(T data, DateTime date)
        {
            var todays = this.FirstOrDefault(c => c.DateTime.Date == date.Date && c.Action == TodayValuesKeyName);
            if (todays is null)
            {
                todays = new SmallGameLog() { DateTime = date, Action = TodayValuesKeyName };
                Add(todays);
            }
            var val = Converter.ConvertToString(data);
            todays.Params.Add(val);

            var lasts = this.FirstOrDefault(c => c.DateTime.Date == date.Date && c.Action == LastValuesKeyName);
            if (lasts is null)
            {
                lasts = new SmallGameLog() { DateTime = date, Action = LastValuesKeyName };
                Add(lasts);
            }
            lasts.Params.Add(val);
        }

        /// <summary>
        /// 从最后一次的数据中移除一个数据。
        /// </summary>
        /// <param name="data"></param>
        /// <param name="date"></param>
        public void RemoveLastData(T data, DateTime date)
        {
            var item= this.FirstOrDefault(c => c.DateTime.Date == date.Date && c.Action == LastValuesKeyName);
            if(null!=item)
            {
                var tmp = Converter.ConvertToString(data);
                item.Params.RemoveAll(c => c == tmp);
            }
        }

        /// <summary>
        /// 追加最后一次的数据，同时加入指定日期数据。
        /// </summary>
        /// <param name="data"></param>
        /// <param name="date"></param>
        public void AddLastDataRange(IEnumerable<T> data, DateTime date)
        {
            var todays = this.FirstOrDefault(c => c.DateTime.Date == date.Date && c.Action == TodayValuesKeyName);
            if (todays is null)
            {
                todays = new SmallGameLog() { DateTime = date, Action = TodayValuesKeyName };
                Add(todays);
            }
            var val = data.Select(c => Converter.ConvertToString(c));
            todays.Params.AddRange(val);

            var lasts = this.FirstOrDefault(c => c.DateTime.Date == date.Date && c.Action == LastValuesKeyName);
            if (lasts is null)
            {
                lasts = new SmallGameLog() { DateTime = date, Action = LastValuesKeyName };
                Add(lasts);
            }
            lasts.Params.AddRange(val);
        }

        /// <summary>
        /// 复位最后一次数据。
        /// </summary>
        /// <param name="date"></param>
        public void ResetLastData(DateTime date)
        {
            this.FirstOrDefault(c => c.DateTime.Date == date.Date && c.Action == LastValuesKeyName)?.Params.Clear();
        }

        /// <summary>
        /// 复位指定日期的所有数据，包括最后一次的数据。
        /// </summary>
        /// <param name="date"></param>
        public void ResetTodayData(DateTime date)
        {
            this.RemoveAll(c => c.DateTime.Date == date.Date);
        }
    }
}
