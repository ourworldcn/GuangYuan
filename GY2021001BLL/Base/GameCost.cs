using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace GuangYuan.GY001.BLL.Base
{
    /// <summary>
    /// 用于分析计算游戏内耗损的类。
    /// </summary>
    public struct GameCost
    {
        public static bool TryParse(IReadOnlyDictionary<string, object> propertyBag, GameChar gameChar, string prefix, ICollection<GameCost> result)
        {
            var coll = propertyBag.GetValuesWithoutPrefix(prefix);  //获取一组变更数据
            var tid2gi = gameChar.AllChildren.ToLookup(c => c.TemplateId);

            foreach (var item in coll)
            {
                var tidVt = item.FirstOrDefault(c => c.Item1 == "tid");
                if (!OwConvert.TryToGuid(tidVt.Item2, out var tid)) //若没有模板id
                    return false;
                var countVt = item.FirstOrDefault(c => c.Item1 == "count");
                if (countVt.Item1 != "count" || OwConvert.TryToDecimal(countVt.Item2, out var count))   //若没有指定数量
                    return false;
                var ptidVt = item.FirstOrDefault(c => c.Item1 == "ptid");
                GameItem gi;
                if (OwConvert.TryToGuid(ptidVt.Item2, out var ptid))  //若限定容器
                {
                    gi = tid2gi[ptid].SelectMany(c => c.Children).FirstOrDefault(c => c.TemplateId == tid);
                }
                else //若未限定容器
                {
                    gi = tid2gi[tid].FirstOrDefault();
                }
                if (null != gi)
                    result.Add(new GameCost(gi, count));
                else
                    return false;
            }
            return true;
        }

        public GameCost(GameItem item, decimal count)
        {
            Item = item;
            Count = count;
        }

        /// <summary>
        /// 获取该结构是否为空。
        /// </summary>
        public readonly bool IsEmpty => Item is null;

        /// <summary>
        /// 耗损的对象。
        /// </summary>
        public GameItem Item { get; set; }

        /// <summary>
        /// 耗损数量。
        /// </summary>
        public decimal Count { get; set; }
    }

    public struct GameQuery
    {
        //query1=gtq;{sdjksj};{dsjl};sds;1 gt;count;2,set1=count;1

        public static bool TryParse(string str, out GameQuery result)
        {
            result = new GameQuery();
            return true;
        }

        public GamePropertyRef GameRef { get; set; }

        public string Operator { get; set; }

        public object Value { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class GamePropertyRef
    {
        public static bool TryParse(string str, out GamePropertyRef result)
        {
            var ary = str.Split(OwHelper.SemicolonArrayWithCN);
            if (3 == ary.Length)
            {
                if (!Guid.TryParse(ary[0], out var ptid) || !Guid.TryParse(ary[1], out var tid) || !(ary[1] is string pn))
                {
                    result = default;
                    return false;
                }
                result = new GamePropertyRef()
                {
                    PropertyName = pn,
                    TemplateId = tid,
                    ParentTemplateId = ptid,
                };
            }
            else if (2 == ary.Length)
            {
                if (!Guid.TryParse(ary[0], out var ptid) || !(ary[1] is string pn))
                {
                    result = default;
                    return false;
                }
                result = new GamePropertyRef()
                {
                    PropertyName = pn,
                    TemplateId = ptid,
                };
            }
            else
            {
                result = default;
                return false;
            }
            return true;
        }

        public Guid? ParentTemplateId { get; set; }

        public Guid TemplateId { get; set; }

        public string PropertyName { get; set; }

        public object GetValue(GameChar gameChar)
        {
            GameItem gi;
            var tid = TemplateId;
            if (ParentTemplateId.HasValue)  //如指定了父容器
            {
                var ptid = ParentTemplateId.Value;
                gi = gameChar.AllChildren.FirstOrDefault(c => c.TemplateId == ptid).Children.FirstOrDefault(c => c.TemplateId == TemplateId);
            }
            else
            {
                gi = gameChar.AllChildren.FirstOrDefault(c => c.TemplateId == TemplateId);
            }
            return gi.Properties.GetValueOrDefault(PropertyName);
        }

        public void SetValut(GameChar gameChar, object value)
        {
            GameItem gi;
            var tid = TemplateId;
            if (ParentTemplateId.HasValue)  //如指定了父容器
            {
                var ptid = ParentTemplateId.Value;
                gi = gameChar.AllChildren.FirstOrDefault(c => c.TemplateId == ptid).Children.FirstOrDefault(c => c.TemplateId == tid);
            }
            else
            {
                gi = gameChar.AllChildren.FirstOrDefault(c => c.TemplateId == tid);
            }
            gi.Properties[PropertyName] = value;
        }
    }
}
