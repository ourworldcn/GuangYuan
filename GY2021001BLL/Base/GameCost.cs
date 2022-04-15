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
        //public GameQuery()
        //{
        ////string str = "";
        //    //str.AsSpan().Split()
        //}
    }
}
