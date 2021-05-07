﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace OwGame
{
    public abstract class GuidKeyBase
    {
        public GuidKeyBase()
        {
            Id = Guid.NewGuid();
        }

        public GuidKeyBase(Guid id)
        {
            Id = id;
        }

        [Key, Column(Order = 0)]
        public Guid Id { get; set; }
    }

    public static class PocoLoadingExtensions
    {
        public static TRelated Load<TRelated>(
            this Action<object, string> loader,
            object entity,
            ref TRelated navigationField,
            [CallerMemberName] string navigationName = null)
            where TRelated : class
        {
            loader?.Invoke(entity, navigationName);

            return navigationField;
        }

    }

    public static class OwHelper
    {
        /// <summary>
        /// 中英文逗号数组。分割字符串常用此数组，避免生成新对象。
        /// </summary>
        public readonly static char[] CommaArrayWithCN = new char[] { ',', '，' };

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
            var coll = propStr.Trim(' ', '"').Replace(Environment.NewLine, " ").Split(CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries);
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
                if (item.Value is decimal)
                {
                    result.Append(item.Value.ToString()).Append(',');
                }
                else if (item.Value is decimal[])
                {
                    var ary = item.Value as decimal[];
                    result.Append(string.Join('|', ary.Select(c => c.ToString()))).Append(',');
                }
                else //字符串
                {
                    result.Append(item.Value as string).Append(',');
                }
            }
            if (result.Length > 0 && result[result.Length - 1] == ',')   //若尾部是逗号
                result.Remove(result.Length - 1, 1);
            return result.ToString();
        }

        /// <summary>
        /// 遍历一个树结构的所有子项。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="roots">多个根的节点集合。</param>
        /// <param name="children">从每个节点获取其所有子节点的委托。</param>
        /// <returns>一个可枚举集合，包含所有根下的所有节点。</returns>
        public static IEnumerable<T> GetAllSubItemsOfTree<T>(IEnumerable<T> roots, Func<T, IEnumerable<T>> children)
        {
            Stack<T> gameItems = new Stack<T>(roots);

            while (gameItems.TryPop(out T result))
            {
                foreach (var item in children(result))
                    gameItems.Push(item);
                yield return result;
            }
            yield break;
        }


    }

}
