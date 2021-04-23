using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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

    public class OwHelper
    {
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

    }

    public static class SpecialIds
    {
        static public Guid HeadId = Guid.Parse("{A06B7496-F631-4D51-9872-A2CC84A56EAB}");
        public static Guid BodyId = Guid.Parse("{7D191539-11E1-49CD-8D0C-82E3E5B04D31}");
        public static Guid MountsId = Guid.Parse("{6E179D54-5836-4E0B-B30D-756BD07FF196}");

    }

}
