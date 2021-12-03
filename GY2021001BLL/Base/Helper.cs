using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.TemplateDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GuangYuan.GY001.BLL
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
                       let p3 = tmp.Key.Get3Phase(prefix)
                       group (p3.Item2, tmp.Value) by p3.Item3;
            return coll;
        }

        /// <summary>
        /// 获取十进制数字后缀。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetSuffixOfDigit(this string str)
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
        public static (string, string, string) Get3Phase(this string str, string prefix = null)
        {
            prefix ??= string.Empty;
            var suufix = GetSuffixOfDigit(str);   //后缀
            return (prefix, str[prefix.Length..^suufix.Length], suufix);
        }
    }
}

