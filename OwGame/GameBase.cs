/*
 * 文件放置游戏专用的一些基础类
 */
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace OW.Game
{
    public static class OwHelper
    {
        /// <summary>
        /// 中英文逗号数组。分割字符串常用此数组，避免生成新对象。
        /// </summary>
        public readonly static char[] CommaArrayWithCN = new char[] { ',', '，' };

        /// <summary>
        /// 中英文冒号数组。分割字符串常用此数组，避免生成新对象。
        /// </summary>
        public readonly static char[] ColonArrayWithCN = new char[] { ':', '：' };

        /// <summary>
        /// 中英文双引号。
        /// </summary>
        public readonly static char[] DoubleQuotesWithCN = new char[] { '"', '“', '”' };

        /// <summary>
        /// 路径分隔符。
        /// </summary>
        public readonly static char[] PathSeparatorChar = new char[] { '\\', '/' };

        /// <summary>
        /// 试图把对象转换为数值。
        /// </summary>
        /// <param name="obj">null导致立即返回false。</param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        static public bool TryGetDecimal(object obj, out decimal result)
        {
            if (obj is null)
            {
                result = default;
                return false;
            }
            bool succ;
            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                    result = Convert.ToDecimal(obj);
                    succ = true;
                    break;
                case TypeCode.Decimal:
                    result = (decimal)obj;
                    succ = true;
                    break;
                case TypeCode.String:
                    succ = decimal.TryParse(obj as string, out result);
                    break;
                case TypeCode.Char:
                case TypeCode.DateTime:
                case TypeCode.Empty:
                case TypeCode.Object:
                case TypeCode.DBNull:
                case TypeCode.Boolean:
                default:
                    result = decimal.Zero;
                    succ = false;
                    break;
            }
            return succ;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        static public bool TryGetFloat(object obj, out float result)
        {
            if (obj is null)
            {
                result = default;
                return false;
            }
            bool succ;
            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    result = Convert.ToSingle(obj);
                    succ = true;
                    break;
                case TypeCode.String:
                    succ = float.TryParse(obj as string, out result);
                    break;
                case TypeCode.Object:
                case TypeCode.Empty:
                case TypeCode.DBNull:
                case TypeCode.Char:
                case TypeCode.DateTime:
                default:
                    result = default;
                    succ = false;
                    break;
            }
            return succ;
        }

        /// <summary>
        /// 尽可能转换为Guid类型。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="result"></param>
        /// <returns>true成功转换，false未成功。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        static public bool TryGetGuid(object obj, out Guid result)
        {

            if (obj is Guid id)
            {
                result = id;
                return true;
            }
            else if (obj is string str && Guid.TryParse(str, out result))
            {
                return true;
            }
            else if (obj is byte[] ary && ary.Length == 16)
            {
                try
                {
                    result = new Guid(ary);

                }
                catch (Exception)
                {
                    result = default;
                    return false;
                }
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// 四舍五入取整。
        /// </summary>
        /// <param name="result"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundWithAwayFromZero(decimal result) => (int)Math.Round(result, MidpointRounding.AwayFromZero);

        /// <summary>
        /// 分割属性字符串。
        /// </summary>
        /// <param name="propStr">属性字符串。</param>
        /// <param name="stringProps">字符串属性。</param>
        /// <param name="numberProps">数值属性。</param>
        /// <param name="sequenceProps">序列属性。</param>
        public static void AnalysePropertiesString(string propStr, IDictionary<string, string> stringProps, IDictionary<string, float> numberProps,
            IDictionary<string, float[]> sequenceProps)
        {
            var coll = propStr.Trim(' ', '"').Replace(Environment.NewLine, " ").Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in coll)
            {
                var guts = item.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (2 != guts.Length)
                {
                    throw new InvalidCastException($"数据格式错误:'{guts}'");   //TO DO
                }
                var keyName = string.Intern(guts[0].Trim());
                var val = guts[1].Trim();
                if (val.Contains('|'))  //若是序列属性
                {
                    var seq = val.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    var ary = seq.Select(c => float.Parse(c.Trim())).ToArray();
                    sequenceProps[keyName] = ary;
                }
                else if (float.TryParse(val, out float num))   //若是数值属性
                {
                    numberProps[keyName] = num;
                }
                else //若是字符串属性
                {
                    stringProps[keyName] = val;
                }
            }
        }

        /// <summary>
        /// 用字串形式属性，填充游戏属性字典。
        /// </summary>
        /// <param name="propStr"></param>
        /// <param name="props"></param>
        public static void AnalysePropertiesString(string propStr, IDictionary<string, object> props)
        {
            if (string.IsNullOrWhiteSpace(propStr))
                return;
            var coll = propStr.Replace(Environment.NewLine, " ").Trim(' ', '"').Split(CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in coll)
            {
                var guts = item.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (2 != guts.Length)
                {
                    if (item.IndexOf('=') <= 0 || item.Count(c => c == '=') != 1)  //若是xxx= 格式，解释为xxx=null
                        throw new InvalidCastException($"数据格式错误:'{guts}'");   //TO DO
                }
                var keyName = string.Intern(guts[0].Trim());
                var val = guts.Length < 2 ? null : guts?[1]?.Trim();
                if (val is null)
                {
                    props[keyName] = null;
                }
                else if (val.Contains('|'))  //若是序列属性
                {
                    var seq = val.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    var ary = seq.Select(c => decimal.Parse(c.Trim())).ToArray();
                    props[keyName] = ary;
                }
                else if (decimal.TryParse(val, out decimal num))   //若是数值属性
                {
                    props[keyName] = num;
                }
                else //若是字符串属性
                {
                    props[keyName] = val;
                }
            }
        }

        /// <summary>
        /// 从游戏属性字典获取字符串表现形式。
        /// </summary>
        /// <param name="dic">可以是空字典，但不能是空引用。</param>
        /// <returns></returns>
        public static string ToPropertiesString(IDictionary<string, object> dic)
        {
            StringBuilder result = new StringBuilder();
            foreach (var item in dic)
            {
                result.Append(item.Key).Append('=');
                if (TryGetDecimal(item.Value, out _))   //如果可以转换为数字
                {
                    result.Append(item.Value.ToString()).Append(',');
                }
                else if (item.Value is decimal[])
                {
                    var ary = item.Value as decimal[];
                    result.AppendJoin('|', ary.Select(c => c.ToString())).Append(',');
                }
                else //字符串
                {
                    result.Append(item.Value?.ToString()).Append(',');
                }
            }
            if (result.Length > 0 && result[^1] == ',')   //若尾部是逗号
                result.Remove(result.Length - 1, 1);
            return result.ToString();
        }

        /// <summary>
        /// 遍历一个树结构的所有子项。深度优先算法遍历子树。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="roots">多个根的节点集合。</param>
        /// <param name="getChildren">从每个节点获取其所有子节点的委托。</param>
        /// <returns>一个可枚举集合，包含所有根下的所有节点。</returns>
        public static IEnumerable<T> GetAllSubItemsOfTree<T>(IEnumerable<T> roots, Func<T, IEnumerable<T>> getChildren)
        {
            Stack<T> gameItems = new Stack<T>(roots);
            while (gameItems.TryPop(out T result))
            {
                foreach (var item in getChildren(result))
                    gameItems.Push(item);
                yield return result;
            }
        }

        /// <summary>
        /// 遍历一个树结构的所有子项。广度优先算法遍历子树。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="getChildren">从每个节点获取其所有子节点的委托。</param>
        /// <param name="roots">多个根的节点对象。</param>
        /// <returns>一个可枚举集合，包含所有根下的所有节点(不含根节点)。</returns>
        public static IEnumerable<T> GetAllSubItemsOfTreeWithBfs<T>(Func<T, IEnumerable<T>> getChildren, params T[] roots)
        {
            Queue<T> gameItems = new Queue<T>(roots);
            while (gameItems.TryDequeue(out T result))
            {
                foreach (var item in getChildren(result))
                {
                    gameItems.Enqueue(item);
                    yield return item;
                }
            }
        }

        /// <summary>
        /// 分拣左右两个序列中的元素到三个集合中，三个集合的条件如下：仅左侧序列拥有的元素，两个序列都有的元素，仅右侧序列拥有的元素。
        /// 如果序列中有重复元素则分别计数。结果集合中元素顺序不稳定。
        /// </summary>
        /// <typeparam name="TLeft"></typeparam>
        /// <typeparam name="TRight"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="right"></param>
        /// <param name="getLeftKey"></param>
        /// <param name="getRightKey"></param>
        /// <param name="leftOnly">追加仅包含在左侧序列元素的集合，可以是null,则忽略。</param>
        /// <param name="boths">追加两个序列都包含的元素集合，可以是null,则忽略。</param>
        /// <param name="rightOnly">追加仅包含在右侧序列元素的集合，可以是null,则忽略。</param>
        public static void ApartWithWithRepeated<TLeft, TRight, TKey>(this IEnumerable<TLeft> source, IEnumerable<TRight> right, Func<TLeft, TKey> getLeftKey, Func<TRight, TKey> getRightKey,
            ICollection<TLeft> leftOnly, ICollection<(TLeft, TRight)> boths, ICollection<TRight> rightOnly)
        {
            bool b = source.Count() > right.Count();
            var leftDic = (from tmp in source
                           group tmp by getLeftKey(tmp) into g
                           select (g.Key, g.ToList())).ToDictionary(c => c.Key, c => c.Item2);
            foreach (var item in right)
            {
                var key = getRightKey(item);    //右序列元素的键
                if (leftDic.TryGetValue(key, out List<TLeft> leftLst))  //若两者皆有
                {
                    var tmp = leftLst[^1];
                    leftLst.RemoveAt(leftLst.Count - 1);
                    boths?.Add((tmp, item));
                    if (leftLst.Count <= 0)
                        leftDic.Remove(key);
                }
                else //仅右侧序列有
                {
                    rightOnly?.Add(item);
                }
            }
            //追加左侧序列独有元素
            if (null != leftOnly)
                foreach (var item in leftDic.SelectMany(c => c.Value))
                    leftOnly.Add(item);

        }

        /// <summary>
        /// 分解|分开的数组，并放入decimal数组中。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool AnalyseSequence(string str, out decimal[] result)
        {
            result = null;
            var ary = str.Split('|', StringSplitOptions.RemoveEmptyEntries);
            List<decimal> lst = new List<decimal>();
            foreach (var item in ary)
            {
                if (!decimal.TryParse(item, out decimal tmp))
                    return false;
                lst.Add(tmp);
            }
            result = lst.ToArray();
            return true;
        }

        /// <summary>
        /// 分析1|3|2类型序列添加到指定集合末尾。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="collection"></param>
        /// <returns>true成功添加，false遇到了一个不能转换为数字的元素，此时<paramref name="collection"/>中有不确定个元素被追加到末尾。</returns>
        public static bool AnalyseSequence(string str, ICollection<decimal> collection)
        {
            var ary = str.Split('|', StringSplitOptions.RemoveEmptyEntries);
            List<decimal> lst = new List<decimal>();
            foreach (var item in ary)
            {
                if (!decimal.TryParse(item, out decimal tmp))
                    return false;
                lst.Add(tmp);
            }
            return true;
        }

        /// <summary>
        /// 在一组相对概率中选择一个元素。
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="getProb">所有元素要是非负数。序列不可为空，不可全为0(此时行为未知)</param>
        /// <param name="rnd">随机数，要在区间[0,1)中。</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static TSource RandomSelect<TSource>(IEnumerable<TSource> source, Func<TSource, decimal> getProb, double rnd)
        {
            if (rnd < 0 || rnd >= 1)
                throw new ArgumentOutOfRangeException(nameof(rnd), "要在区间[0,1)中");
            decimal tmp = 0;
            bool hasNoneZero = false;
            var innerSeq = source.OrderByDescending(c => getProb(c)).Select(c =>
            {
                var tmpProb = getProb(c);
                if (tmpProb < 0)
                    throw new ArgumentOutOfRangeException(nameof(source), "所有元素要是非负数。");
                else if (tmpProb > 0)
                    hasNoneZero = true;
                tmp += tmpProb;
                return (Prob: tmp, Data: c);
            }).ToArray();
            if (!hasNoneZero)
                throw new ArgumentException("序列所有相对概率数都是0。", nameof(source));
            var seed = (decimal)rnd * innerSeq[^1].Prob;

            var (Prob, Data) = innerSeq.First(c => c.Prob >= seed);
            return Data;
        }

        //static public string SerializeToJson(object obj, Type type = null)
        //{
        //    type ??= obj.GetType();
        //    JsonSerializer.Serialize(obj, type);
        //    Assembly.GetAssembly()
        //}
    }

    public static class GameHelper
    {
        /// <summary>
        /// 用Base64编码Guid类型。
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToBase64String(this Guid guid)
        {
            return Convert.ToBase64String(guid.ToByteArray());
        }

        /// <summary>
        /// 从Base64编码转换获取Guid值。
        /// </summary>
        /// <param name="str">空引用，空字符串，空白导致返回<see cref="Guid.Empty"/></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid FromBase64String(string str)
        {
            return string.IsNullOrWhiteSpace(str) ? Guid.Empty : new Guid(Convert.FromBase64String(str));
        }

    }

    public static class StringObjectDictionaryExtensions
    {
        /// <summary>
        /// 获取指定键的值，并转换为Guid类型，如果没有指定键或不能转换则返回默认值。
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        static public Guid GetGuidOrDefault(this IReadOnlyDictionary<string, object> dic, string name, Guid defaultVal = default)
        {
            if (!dic.TryGetValue(name, out var obj))
                return defaultVal;
            if (obj is null)
                return defaultVal;
            else if (obj is string str && Guid.TryParse(str, out var guid))
                return guid;
            else if (obj is Guid val)
                return val;
            else
                return defaultVal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public decimal GetDecimalOrDefault(this IReadOnlyDictionary<string, object> dic, string name, decimal defaultVal = default) =>
            dic.TryGetValue(name, out var obj) && OwHelper.TryGetDecimal(obj, out var result) ? result : defaultVal;

        /// <summary>
        /// 获取指定键值，并尽可能转换为日期。
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public DateTime GetDateTimeOrDefault(this IReadOnlyDictionary<string, object> dic, string name, DateTime defaultVal = default)
        {
            if (!dic.TryGetValue(name, out var obj))
                return defaultVal;
            if (obj is DateTime result)
                return result;
            return obj is string str && DateTime.TryParse(str, out result) ? result : defaultVal;
        }

        /// <summary>
        /// 获取指定键值的值，或转换为字符串。
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        static public string GetStringOrDefault(this IReadOnlyDictionary<string, object> dic, string name, string defaultVal = default) =>
            dic.TryGetValue(name, out var obj) && obj is string result ? result : defaultVal;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float GetFloatOrDefalut(this IReadOnlyDictionary<string, object> dic, string key, float defaultVal = default)
        {
            if (!dic.TryGetValue(key, out var obj))
                return defaultVal;
            return OwHelper.TryGetFloat(obj, out var result) ? result : defaultVal;
        }


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
                if (_LastValue >= MaxValue)
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

    /// <summary>
    /// 初始化挂接接口。
    /// </summary>
    public interface IGameObjectInitializer
    {
        /// <summary>
        /// 在游戏对象创建后调用，以帮助特定项目初始化自己独有的数据。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>true初始化了数据，false没有进行特定该类型对象的初始化。</returns>
        bool Created(object obj);

        /// <summary>
        /// 游戏对象从后被存储加载到内存后调用。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>true初始化了数据，false没有进行特定该类型对象的初始化。</returns>
        bool Loaded(object obj, DbContext context);
    }
}