/*
 * 商城相关的辅助代码文件
 */

using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using OW.Game;
using OW.Game.Store;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace GuangYuan.GY001.BLL
{

    /// <summary>
    /// 记述商品的购买信息。
    /// </summary>
    public class GoodsInfoItem
    {
        public GoodsInfoItem()
        {

        }

        /// <summary>
        /// 商品模板Id。
        /// </summary>
        public Guid ShoppingId { get; set; }

        /// <summary>
        /// 最后一个周期内购买的总计数量。
        /// </summary>
        public decimal BuyCount { get; set; }

        /// <summary>
        /// 最后一次购买发生的时间。
        /// </summary>
        public DateTime BuyLastDateTime { get; set; }
    }

    /// <summary>
    /// 可刷新商品信息。
    /// </summary>
    public class RefreshInfoItem
    {
        /// <summary>
        /// 属名。
        /// </summary>
        public string Genus { get; set; }

        /// <summary>
        /// 当前使用的组号。
        /// </summary>
        public int GroupNumber { get; set; }

        /// <summary>
        /// 最后刷新时间。
        /// </summary>
        public DateTime RefreshLastDateTime { get; set; }

        /// <summary>
        /// 今日已经刷新次数。
        /// </summary>
        public int RefreshCount { get; set; }

        /// <summary>
        /// 刷新用的金币价格。
        /// </summary>
        [JsonIgnore]
        public decimal[] CostOfGold { get; set; }

    }

    /// <summary>
    /// 可刷新商品的数据结构。
    /// </summary>
    public class ShoppingSlotView : GameCharGameContext
    {
        public ShoppingSlotView([NotNull] VWorld world, [NotNull] GameChar gameChar, DateTime now) : base(world, gameChar)
        {
            _Now = now;
        }

        private Dictionary<string, GoodsInfoItem> _GoodsInfos;
        /// <summary>
        /// 获取购买商品信息。
        /// 键是商品模板Id的字符串形式，值购买信息。
        /// </summary>
        public Dictionary<string, GoodsInfoItem> GoodsInfos
        {
            get
            {
                if (_GoodsInfos is null)
                {
                    _GoodsInfos = OwConvert.FromUriString<Dictionary<string, GoodsInfoItem>>(ShoppingSlot.GetSdpStringOrDefault(nameof(GoodsInfos)));
                }
                return _GoodsInfos;
            }
        }

        private Dictionary<string, RefreshInfoItem> _RefreshInfos;
        /// <summary>
        /// 商品刷新信息。全部的刷新信息被初始化。键是商品的属，值该属的刷新信息。
        /// 自动刷新今日未刷新的物品。
        /// </summary>
        public Dictionary<string, RefreshInfoItem> RefreshInfos
        {
            get
            {
                if (_RefreshInfos is null)
                {
                    _RefreshInfos = OwConvert.FromUriString<Dictionary<string, RefreshInfoItem>>(ShoppingSlot.GetSdpStringOrDefault(nameof(RefreshInfos)));
                    var dic = World.ShoppingManager.Genus2GroupNumbers;
                    var addGenus = dic.Keys.Except(_RefreshInfos.Keys);   //需要添加的品类
                    var rnd = VWorld.WorldRandom;
                    var template = ShoppingSlot.GetTemplate();
                    const string prefix = "msg";   //TO DO
                    foreach (var item in addGenus)
                    {
                        _RefreshInfos[item] = new RefreshInfoItem()
                        {
                            RefreshCount = 0,  //已经刷新次数
                            Genus = item,   //属
                            GroupNumber = rnd.Next(dic[item].Length),   //新的组号
                            RefreshLastDateTime = Now, //最后刷新时间
                            CostOfGold = template.GetSequenceProperty<decimal>($"{prefix}{item}"),
                        };
                    }
                    foreach (var item in _RefreshInfos) //写入刷新代价
                    {
                        item.Value.CostOfGold = template.GetSequenceProperty<decimal>($"{prefix}{item.Key}");
                    }
                    foreach (var item in _RefreshInfos.Where(c => c.Value.RefreshLastDateTime.Date != Now.Date))  //自动刷新今日未刷新的物品
                    {
                        //TO DO 未定义同组的刷新周期问题
                        item.Value.GroupNumber = rnd.Next(item.Value.CostOfGold.Length);
                        item.Value.RefreshLastDateTime = Now;
                        item.Value.RefreshCount = 0;
                    }
                }
                return _RefreshInfos;
            }
        }

        public GameItem ShoppingSlot => GameChar.GetShoppingSlot();

        private DateTime _Now;
        /// <summary>
        /// 获取或设置当前时间。
        /// </summary>
        public DateTime Now { get => _Now; set => _Now = value; }

        /// <summary>
        /// 增加购买记录。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="count">不管是否超过可购买上限。</param>
        /// <returns>false 原有购买记录，true 新增了购买记录,并更新了统计信息。</returns>
        public bool AddItem(GameShoppingTemplate template, decimal count)
        {
            var goodsInfo = GetOrAddGoodsInfo(template);
            var lastStart = template.GetStart(goodsInfo.BuyLastDateTime); //上次购买所处周期的开始时间
            var start = template.GetStart(Now); //此周期开始时间
            if (lastStart != start)    //若已经跨越周期
            {
                goodsInfo.BuyCount = count;
            }
            else //若未跨越周期
            {
                goodsInfo.BuyCount += count;
            }
            goodsInfo.BuyLastDateTime = Now;
            //增加购买记录
            var gar = new GameActionRecord()
            {
                DateTimeUtc = Now,
                ParentId = GameChar.Id,
                ActionId = BuyRecordActionId,
            };
            gar.SetSdp("ShoppingId", template.IdString);
            gar.SetSdp("BuyCount", count);
            World.AddToUserContext(new object[] { gar });   //异步增加购买记录
            return true;
        }

        /// <summary>
        /// 指定周期内指定物品已购买数量。考虑到了可刷新商品的因素,不可购买的一律返回0。
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public decimal GetCountOfBuyed(GameShoppingTemplate template)
        {
            decimal result;
            if (template.GroupNumber.HasValue)   //若是可刷新物品
            {
                var di = RefreshInfos[template.Genus];
                if (di.GroupNumber != template.GroupNumber.Value)  //若当前不可购买
                    result = decimal.Zero;
                else //若当前可购买
                    result = GetBuyedCount(template);
            }
            else //若非可刷新物品
                result = GetBuyedCount(template);
            return result;
        }

        /// <summary>
        /// 获取已经购买的数量。未购买，不可购买，新周期都返回0.仅看购买统计信息，不考虑其他因素。
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        private decimal GetBuyedCount(GameShoppingTemplate template)
        {
            if (!GoodsInfos.TryGetValue(template.IdString, out var goodsInfo))  //若没有初始化购买信息
                return decimal.Zero;
            var lastStart = template.GetStart(goodsInfo.BuyLastDateTime); //此周期开始时间
            var start = template.GetStart(Now); //此周期开始时间
            if (lastStart != start)    //若已经跨越周期
                return decimal.Zero;
            return goodsInfo.BuyCount;
        }

        /// <summary>
        /// 在指定时间是否可以购买指定数量的商品。不计算购买资源是否足够问题。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="count">购买数量，可以是0，表示不考虑数量因素。</param>
        /// <returns>true可以购买，false不能购买，<see cref="VWorld.GetLastError"/>可获得详细信息。<see cref="ErrorCodes.ERROR_IMPLEMENTATION_LIMIT"/> 是可购买数量不足或无效时间。</returns>
        public bool AllowBuy(GameShoppingTemplate template, decimal count)
        {
            if (!IsValid(template)) //若不是有效的销售时间
                return false;
            if (template.GroupNumber.HasValue)   //若是可刷新商品
            {
                var di = RefreshInfos[template.Genus];
                if (di.GroupNumber != template.GroupNumber.Value)  //若当前不可购买
                {
                    OwHelper.SetLastError(ErrorCodes.ERROR_IMPLEMENTATION_LIMIT);
                    return false;
                }
            }
            var info = GetOrAddGoodsInfo(template); //购买信息
            var lastStart = template.GetStart(info.BuyLastDateTime);    //最近购买的周期
            var start = template.GetStart(Now);    //当前购买周期
            bool result;
            if (start != lastStart)    //若跨周期
                result = template.MaxCount >= count;
            else //若同周期
                result = -1 == template.MaxCount || template.MaxCount >= count + info.BuyCount;
            if (!result)
                OwHelper.SetLastError(ErrorCodes.ERROR_IMPLEMENTATION_LIMIT);

            return result;
        }

        /// <summary>
        /// 获取或初始化购买统计信息。
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        private GoodsInfoItem GetOrAddGoodsInfo(GameShoppingTemplate template)
        {
            if (!GoodsInfos.TryGetValue(template.IdString, out var result))  //若没有初始化购买信息
            {
                result = new GoodsInfoItem()
                {
                    ShoppingId = template.Id,
                    BuyCount = 0,
                    BuyLastDateTime = Now,
                };
                GoodsInfos[template.IdString] = result;
            }
            return result;
        }

        /// <summary>
        /// 在当前指定时间是否可以购买。
        /// </summary>
        /// <param name="template"></param>
        /// <returns>true当前时间可以购买。false不可购买<see cref="VWorld.GetLastError"/>返回<see cref="ErrorCodes.ERROR_IMPLEMENTATION_LIMIT"/>。</returns>
        public bool IsValid(GameShoppingTemplate template)
        {
            var start = template.GetStart(Now);
            var end = template.GetEnd(Now);
            var result = Now >= start && Now <= end;
            if (!result)
                OwHelper.SetLastError(ErrorCodes.ERROR_IMPLEMENTATION_LIMIT);
            return result;
        }

        private const string BuyRecordActionId = "商城购买";

        public override void Save()
        {
            if (null != _RefreshInfos)    //若可能发生变化
            {
                ShoppingSlot.SetSdp(nameof(RefreshInfos), OwConvert.ToUriString(_RefreshInfos));
            }
            if (null != _GoodsInfos)    //若可能发生变化
            {
                ShoppingSlot.SetSdp(nameof(GoodsInfos), OwConvert.ToUriString(_GoodsInfos));
            }
            base.Save();
        }

    }


}