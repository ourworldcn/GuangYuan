using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace System.Collections.Generic
{
    /// <summary>
    /// <see cref="Dictionary{String, Object}"/> 类型的辅助方法封装类。"🐂, 🐄, 🐆,"
    /// </summary>
    public static class StringObjectDictionaryExtensions
    {
        /// <summary>
        /// 针对字典中包含以下键值进行结构：mctid0=xxx;mccount0=1,mctid1=kn2,mccount=2。将其前缀去掉，数字后缀变为键，如{后缀,(去掉前后缀的键,值)}，注意后缀可能是空字符串即没有后缀
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="prefix">前缀，可以是空引用或空字符串，都表示没有前缀。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<IGrouping<string, (string, object)>> GetValuesWithoutPrefix(this IReadOnlyDictionary<string, object> dic, string prefix = null)
        {
            prefix ??= string.Empty;
            var coll = from tmp in dic.Where(c => c.Key.StartsWith(prefix)) //仅针对指定前缀的键值
                       let p3 = tmp.Key.Get3Segment(prefix)
                       group (p3.Item2, tmp.Value) by p3.Item3;
            return coll;
        }

        /// <summary>
        /// 获取十进制数字后缀。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetSuffixOfDigit([NotNull] this string str)
        {
            var suffixLen = Enumerable.Reverse(str).TakeWhile(c => char.IsDigit(c)).Count();   //最后十进制数字尾串的长度
            return str[^suffixLen..];
        }

        /// <summary>
        /// 分解字符串为三段，前缀，词根，数字后缀(字符串形式)。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="prefix">前缀，可以是空引用或空字符串，都表示没有前缀。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (string, string, string) Get3Segment(this string str, string prefix = null)
        {
            prefix ??= string.Empty;
            var suufix = GetSuffixOfDigit(str);   //后缀
            return (prefix, str[prefix.Length..^suufix.Length], suufix);
        }

        #region 获取指定类型的值或默认值

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetGuid(this IReadOnlyDictionary<string, object> dic, string name, out Guid result)
        {
            result = default;
            return dic.TryGetValue(name.ToString(), out var obj) && OwConvert.TryToGuid(obj, out result);
        }

        /// <summary>
        /// 获取指定键的值，并转换为Guid类型，如果没有指定键或不能转换则返回默认值。
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid GetGuidOrDefault(this IReadOnlyDictionary<string, object> dic, string name, Guid defaultVal = default) =>
            dic.TryGetGuid(name, out var result) ? result : defaultVal;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetDecimal(this IReadOnlyDictionary<string, object> dic, string name, out decimal result)
        {
            if (dic.TryGetValue(name, out var obj) && OwConvert.TryToDecimal(obj, out result))
                return true;
            else
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetDecimalOrDefault(this IReadOnlyDictionary<string, object> dic, string name, decimal defaultVal = default) =>
            dic.TryGetDecimal(name, out var result) ? result : defaultVal;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetDateTime(this IReadOnlyDictionary<string, object> dic, string name, out DateTime result)
        {
            result = default;
            return dic.TryGetValue(name, out var obj) && OwConvert.TryGetDateTime(obj, out result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetString(this IReadOnlyDictionary<string, object> dic, string name, out string result)
        {
            if (!dic.TryGetValue(name, out var obj))
            {
                result = default;
                return false;
            }
            result = obj switch
            {
                _ when obj is string => (string)obj,
                _ => obj?.ToString(),
            };
            return true;
        }

        /// <summary>
        /// 获取指定键值，并尽可能转换为日期。
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime GetDateTimeOrDefault(this IReadOnlyDictionary<string, object> dic, string name, DateTime defaultVal = default) =>
            dic.TryGetDateTime(name, out var result) ? result : defaultVal;

        /// <summary>
        /// 获取指定键值的值，或转换为字符串。
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetStringOrDefault(this IReadOnlyDictionary<string, object> dic, string name, string defaultVal = default) =>
            dic.TryGetString(name, out var obj) ? obj : defaultVal;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloatOrDefalut(this IReadOnlyDictionary<string, object> dic, string key, float defaultVal = default)
        {
            if (!dic.TryGetValue(key, out var obj))
                return defaultVal;
            return OwConvert.TryToFloat(obj, out var result) ? result : defaultVal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetBooleanOrDefaut(this IReadOnlyDictionary<string, object> dic, string key, bool defaultVal = default)
        {
            if (!dic.TryGetValue(key, out var obj))
                return defaultVal;
            return OwConvert.TryToBoolean(obj, out var result) ? result : defaultVal;
        }

        /// <summary>
        /// 获取三态bool类型值。
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? Get3State(this IReadOnlyDictionary<string, object> dic, [CallerMemberName] string key = null)
        {
            if (!dic.TryGetValue(key, out _))
                return null;
            return OwConvert.TryToBoolean(key, out var result) ? (bool?)result : null;
        }

        /// <summary>
        /// 设置三态bool类型的值。
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="val"></param>
        /// <param name="key"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set3State(this IDictionary<string, object> dic, bool? val, [CallerMemberName] string key = null)
        {
            if (val is null)
                dic.Remove(key);
            else
                dic[key] = val.Value.ToString();
        }

        #endregion 获取指定类型的值或默认值

    }

    public static class StringStringDictionaryExtensions
    {
        #region 获取指定类型的值或默认值

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetGuid(this IReadOnlyDictionary<string, string> dic, string name, out Guid result)
        {
            result = default;
            return dic.TryGetValue(name.ToString(), out var obj) && OwConvert.TryToGuid(obj, out result);
        }

        /// <summary>
        /// 获取指定键的值，并转换为Guid类型，如果没有指定键或不能转换则返回默认值。
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid GetGuidOrDefault(this IReadOnlyDictionary<string, string> dic, string name, Guid defaultVal = default) =>
            dic.TryGetGuid(name, out var result) ? result : defaultVal;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetDecimal(this IReadOnlyDictionary<string, string> dic, string name, out decimal result)
        {
            if (dic.TryGetValue(name, out var obj) && OwConvert.TryToDecimal(obj, out result))
                return true;
            else
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetDecimalOrDefault(this IReadOnlyDictionary<string, string> dic, string name, decimal defaultVal = default) =>
            dic.TryGetDecimal(name, out var result) ? result : defaultVal;

        #endregion 获取指定类型的值或默认值

    }
}

