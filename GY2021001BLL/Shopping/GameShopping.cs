using Game.Social;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using OW.Game;
using OW.Game.Item;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;

namespace GuangYuan.GY001.BLL
{
    public class GameShoppingManagerOptions
    {

    }

    /// <summary>
    /// 商城相关功能的服务类。
    /// </summary>
    public class GameShoppingManager : GameManagerBase<GameShoppingManagerOptions>
    {
        private Lazy<Dictionary<string, int[]>> _Genus2GroupNumbers;
        /// <summary>
        /// 键是刷新商品的属，值刷新商品的组号。
        /// </summary>
        public IReadOnlyDictionary<string, int[]> Genus2GroupNumbers => _Genus2GroupNumbers.Value;

        public GameShoppingManager()
        {
            Initializer();
        }

        public GameShoppingManager(IServiceProvider service) : base(service)
        {
            Initializer();
        }

        public GameShoppingManager(IServiceProvider service, GameShoppingManagerOptions options) : base(service, options)
        {
            Initializer();
        }

        /// <summary>
        /// 构造函数调用的的初始化函数。
        /// </summary>
        private void Initializer()
        {
            _Genus2GroupNumbers = new Lazy<Dictionary<string, int[]>>(() =>
            {
                var coll = from tmp in World.ItemTemplateManager.Id2Shopping.Values
                           where tmp.GroupNumber.HasValue
                           group tmp by tmp.Genus into g
                           select (g.Key, g.Select(c => c.GroupNumber.Value).Distinct().ToArray());
                return coll.ToDictionary(c => c.Key, c => c.Item2);

            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// 获取当前用户可见的商城表，不在有效时间范围内的不会返回。
        /// </summary>
        /// <param name="datas"></param>
        public void GetList(GetListDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            IEnumerable<GameShoppingTemplate> coll; //不可刷新商品
            if (string.IsNullOrWhiteSpace(datas.Genus))
                coll = World.ItemTemplateManager.Id2Shopping.Values.Where(c => IsValid(c, datas.Now) && c.GroupNumber is null);
            else
                coll = World.ItemTemplateManager.Id2Shopping.Values.Where(c => c.Genus == datas.Genus && IsValid(c, datas.Now) && c.GroupNumber is null);
            var view = new ShoppingSlotView(World, datas.GameChar, datas.Now);
            //可刷新商品
            List<GameShoppingTemplate> rg = new List<GameShoppingTemplate>();
            foreach (var item in view.RefreshInfos)  //遍历可刷新商品
            {
                var tmp = World.ItemTemplateManager.Id2Shopping.Values.Where(c => c.GroupNumber.HasValue && c.Genus == item.Key && c.GroupNumber == item.Value.GroupNumber && IsValid(c, datas.Now));
                rg.AddRange(tmp);
            }
            datas.ShoppingTemplates.AddRange(coll);
            datas.ShoppingTemplates.AddRange(rg);
            view.Save();
            World.CharManager.NotifyChange(datas.GameChar.GameUser);
        }

        /// <summary>
        /// 购买指定商品。
        /// </summary>
        /// <param name="datas">
        /// <paramref name="datas"/>
        /// </param>
        public void Buy(BuyDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            if (!World.ItemTemplateManager.Id2Shopping.TryGetValue(datas.ShoppingId, out var template))
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "找不到商品Id";
                return;
            }
            DateTime now = DateTime.UtcNow;   //当前时间
            var view = new ShoppingSlotView(World, datas.GameChar, datas.Now);
            var oldCount = view.GetCountOfBuyed(template, now);
            if (-1 != template.MaxCount && oldCount + datas.Count > template.MaxCount)
            {
                datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                datas.ErrorMessage = "指定商品剩余数量不足";
                return;
            }
            //校验可购买性
            if (template.GroupNumber.HasValue)   //若购买商品可刷新
            {
                if (view.RefreshInfos.TryGetValue(template.Genus, out var gpn) && template.GroupNumber != gpn.GroupNumber)
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.ErrorMessage = "指定商品不可购买。";
                    return;
                }
            }
            //修改数据
            //生成物品
            var gim = World.ItemManager;
            var gi = new GameItem();
            World.EventsManager.GameItemCreated(gi, template.ItemTemplateId);
            var container = gim.GetDefaultContainer(datas.GameChar, gi);
            if (template.AutoUse)    //若自动使用
            {
                gim.ForcedAdd(gi, datas.GameChar.GetItemBag()); //暂存到道具背包
                using var useItemsDatas = new UseItemsWorkDatas(World, datas.GameChar)
                {
                    UserContext = datas.UserContext,
                };
                useItemsDatas.ItemIds.Add((gi.TemplateId, gi.Count.Value));
                World.ItemManager.UseItems(useItemsDatas);
                if (useItemsDatas.HasError)
                {
                    datas.HasError = useItemsDatas.HasError;
                    datas.ErrorCode = useItemsDatas.ErrorCode;
                    datas.ErrorMessage = useItemsDatas.ErrorMessage;
                    return;
                }
                datas.ChangeItems.AddRange(useItemsDatas.ChangeItems);
            }
            else //若非自动使用物品
            {
                List<GameItem> list = new List<GameItem>();
                World.ItemManager.AddItem(gi, container, list, datas.ChangeItems);
                if (list.Count > 0)    //若需要发送邮件
                {
                    var mail = new GameMail();
                    World.SocialManager.SendMail(mail, new Guid[] { datas.GameChar.Id }, SocialConstant.FromSystemId, list.Select(c => (c, gim.GetDefaultContainer(datas.GameChar, c).TemplateId)));
                }
            }
            //计算价格，修改资源
            var cost = GetCost(template, datas.GameChar);
            foreach (var item in cost.Where(c => c.Item2 != 0))
            {
                item.Item1.Count += item.Item2;
                datas.ChangeItems.AddToChanges(item.Item1);
            }
            //改写购买记录数据
            view.AddItem(template, datas.Count, now);
            view.Save();
            World.CharManager.NotifyChange(datas.GameChar.GameUser);
        }

        /// <summary>
        /// 获取售价。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="gameChar"></param>
        /// <returns>售价的集合，可能是空集合，如签到礼包没有售价。</returns>
        private IEnumerable<(GameItem, decimal)> GetCost(GameShoppingTemplate template, GameChar gameChar)
        {
            List<(GameItem, decimal)> result = new List<(GameItem, decimal)>();
            var gitm = World.ItemTemplateManager;
            if (template.Properties.ContainsKey("bd"))   //若有钻石售价
            {
                result.Add((gameChar.GetZuanshi(), -Math.Abs(template.Properties.GetDecimalOrDefault("bd"))));
            }
            else
            {
                var tt = gitm.GetTemplateFromeId(template.ItemTemplateId);
                if (tt != null && tt.Properties.ContainsKey("bd"))    //若有基础模板钻石售价
                {
                    result.Add((gameChar.GetZuanshi(), -Math.Abs(tt.Properties.GetDecimalOrDefault("bd"))));
                }
            }
            if (template.Properties.ContainsKey("bg"))   //若有金币售价
            {
                result.Add((gameChar.GetJinbi(), -Math.Abs(template.Properties.GetDecimalOrDefault("bg"))));
            }
            else
            {
                var tt = gitm.GetTemplateFromeId(template.ItemTemplateId);
                if (tt != null && tt.Properties.ContainsKey("bg"))    //若有基础模板金币售价
                {
                    result.Add((gameChar.GetJinbi(), -Math.Abs(tt.Properties.GetDecimalOrDefault("bg"))));
                }
            }
            return result;
        }

        /// <summary>
        /// 刷新随机商品。
        /// 如果需要刷新多个属，则必须都能成功才可刷新，任意失败(如资源不够)则不能刷新。
        /// ErrorCodes.RPC_S_OUT_OF_RESOURCES 表示资源不足以刷新所有属。
        /// </summary>
        /// <param name="datas"></param>
        public void Refresh(RefreshDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            List<string> genus = new List<string>();
            if (string.IsNullOrWhiteSpace(datas.Genus))
            {
                genus.AddRange(Genus2GroupNumbers.Keys);
            }
            else if (!Genus2GroupNumbers.ContainsKey(datas.Genus))
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "找不到指定的商品属。";
            }
            else
                genus.Add(datas.Genus);
            var slot = datas.GameChar.GetShoppingSlot();
            var view = new ShoppingSlotView(World, datas.GameChar, datas.Now);
            var jinbi = datas.GameChar.GetJinbi();  //金币对象
            var totalCost = genus.Select(c =>   //总计金币代价
            {
                var data = view.RefreshInfos[c];
                var cost = Math.Abs(data.CostOfGold[Math.Clamp(0, data.CostOfGold.Length - 1, data.RefreshCount)]);
                return cost;
            }).Sum();
            if (jinbi.Count < totalCost)    //若资源不足以刷新所有属
            {
                datas.ErrorCode = ErrorCodes.RPC_S_OUT_OF_RESOURCES;
                return;
            }
            foreach (var item in genus) //逐个刷新
            {
                //改写金币数量
                var data = view.RefreshInfos[item];
                var cost = Math.Abs(data.CostOfGold[Math.Clamp(0, data.CostOfGold.Length - 1, data.RefreshCount)]);
                jinbi.Count -= cost;
                //改写商品数据
                Refresh(view, item, datas.Now);
            }
            if (0 < genus.Count)
                datas.ChangeItems.AddToChanges(jinbi);
            ChangeItem.Reduce(datas.ChangeItems);
            World.CharManager.NotifyChange(datas.GameChar.GameUser);
        }

        /// <summary>
        /// 若最后刷新日期已经变化，则刷新随机刷新商品。
        /// </summary>
        /// <param name="view"></param>
        /// <param name="genus"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        private bool RefreshIfDateChanged(ShoppingSlotView view, string genus, DateTime now)
        {
            if (view.RefreshInfos.GetValueOrDefault(genus).RefreshLastDateTime != now.Date)
                return Refresh(view, genus, now);
            return false;
        }

        /// <summary>
        /// 强制复位可刷新商品。
        /// </summary>
        /// <param name="view"></param>
        /// <param name="genus">商品的属。</param>
        /// <param name="now">当前时间。</param>
        private bool Refresh(ShoppingSlotView view, string genus, DateTime now)
        {
            if (!Genus2GroupNumbers.TryGetValue(genus, out var ary))    //若不是包含随机商品的属
                return false;
            var data = view.RefreshInfos[genus];
            data.RefreshLastDateTime = now;    //设置刷新时间
            data.GroupNumber = VWorld.WorldRandom.Next(ary.Length);
            data.RefreshCount++;
            return true;
        }

        /// <summary>
        /// 指定商品在指定时间是否可以销售。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public bool IsValid(GameShoppingTemplate template, DateTime now)
        {
            var start = template.GetStart(now);
            var end = template.GetEnd(now);
            return now >= start && now <= end;
        }
    }

    public class RefreshDatas : ChangeItemsWorkDatasBase
    {
        public RefreshDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public RefreshDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public RefreshDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 要刷新的页签(属)名。前端与数据约定好使用什么页签名即可。
        /// </summary>
        public string Genus { get; set; }

        /// <summary>
        /// 刷新的当前时间，
        /// </summary>
        public DateTime Now { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 购买商品的接口工作数据类。
    /// </summary>
    public class BuyDatas : ChangeItemsAndMailWorkDatsBase
    {
        public BuyDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public BuyDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public BuyDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 购买商品的模板Id。
        /// </summary>
        public Guid ShoppingId { get; set; }

        /// <summary>
        /// 购买的数量。
        /// </summary>
        public decimal Count { get; set; }

        public DateTime Now { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取商城数据的工作接口数据。
    /// </summary>
    public class GetListDatas : ComplexWorkDatasBase
    {
        public GetListDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public GetListDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public GetListDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 指定时间点。
        /// </summary>
        public DateTime Now { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 页签的名字，前端与数据协商好即可。如果设置了则仅返回指定页签(属)的商品。null会返回所有商品。
        /// </summary>
        public string Genus { get; set; }

        public List<GameShoppingTemplate> ShoppingTemplates { get; } = new List<GameShoppingTemplate>();
    }

    public static class GameShoppingManagerExtensions
    {
        public static readonly Guid ShoppingSlotTId = new Guid("{9FB9BA0E-244B-4CC3-A151-461AD18E2699}");

        /// <summary>
        /// 获取商城槽数据。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public static GameItem GetShoppingSlot(this GameChar gameChar) =>
            gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ShoppingSlotTId);
    }

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

        [JsonIgnore]
        public decimal[] CostOfGold { get; set; }

    }

    /// <summary>
    /// 可刷新商品的数据结构。
    /// </summary>
    public class ShoppingSlotView : GameCharWorkDataBase
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
                    _GoodsInfos = new Dictionary<string, GoodsInfoItem>();
                }
                return _GoodsInfos;
            }
        }

        private Dictionary<string, RefreshInfoItem> _RefreshInfos;
        /// <summary>
        /// 商品刷新信息。全部的刷新信息被初始化。
        /// </summary>
        public Dictionary<string, RefreshInfoItem> RefreshInfos
        {
            get
            {
                if (_RefreshInfos is null)
                {
                    _RefreshInfos = OwConvert.FromUriString<Dictionary<string, RefreshInfoItem>>(ShoppingSlot.Properties.GetStringOrDefault(nameof(RefreshInfos)));
                    var dic = World.ShoppingManager.Genus2GroupNumbers;
                    var addGenus = dic.Keys.Except(_RefreshInfos.Keys);   //需要添加的品类
                    var rnd = VWorld.WorldRandom;
                    var template = ShoppingSlot.Template;
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
                    foreach (var item in _RefreshInfos.Where(c => c.Value.RefreshLastDateTime.Date != Now.Date))  //自动刷新今日未刷新的物品
                    {
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
        public DateTime Now { get => _Now; set => _Now = value; }

        /// <summary>
        /// 增加购买记录。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="count"></param>
        /// <param name="now"></param>
        /// <returns>false 原有购买记录，true 新增了购买记录</returns>
        public bool AddItem(GameShoppingTemplate template, decimal count, DateTime now)
        {
            var gar = new GameActionRecord()
            {
                DateTimeUtc = now,
                ParentId = GameChar.Id,
                ActionId = BuyRecordActionId,
            };
            gar.Properties["ShoppingId"] = template.IdString;
            gar.Properties["BuyCount"] = count;
            UserContext.Add(gar);
            return true;
        }

        /// <summary>
        /// 指定周期内指定物品已购买数量。考虑到了可刷新商品的因素。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public decimal GetCountOfBuyed(GameShoppingTemplate template, DateTime now)
        {
            decimal result;
            if (template.GroupNumber.HasValue)   //若是可刷新物品
            {
                var di = RefreshInfos[template.Genus];
                if (di.GroupNumber != template.GroupNumber.Value)  //若当前不可购买
                    result = decimal.Zero;
                else //若当前可购买
                {
                    if (di.RefreshLastDateTime >= Now)    //若最近刷新过
                    {

                    }
                    var start = template.GetStart(now); //此周期开始时间
                    var end = template.GetEnd(now);   //此周期结束时间
                    var coll = UserContext.Set<GameActionRecord>().Where(c => c.DateTimeUtc >= start && c.DateTimeUtc < end && c.ParentId == GameChar.Id && c.ActionId == BuyRecordActionId); //此周期内购买的购买记录
                    var count = coll.AsEnumerable().Where(c => c.Properties.GetGuidOrDefault("ShoppingId") == template.Id).Sum(c => c.Properties.GetDecimalOrDefault("BuyCount"));    //已经购买的数量
                    result = count;
                }
            }
            else //若非可刷新物品
            {
                var start = template.GetStart(now); //此周期开始时间
                var end = template.GetEnd(now);   //此周期结束时间
                var coll = UserContext.Set<GameActionRecord>().Where(c => c.DateTimeUtc >= start && c.DateTimeUtc < end && c.ParentId == GameChar.Id && c.ActionId == BuyRecordActionId); //此周期内购买的购买记录
                var count = coll.AsEnumerable().Where(c => c.Properties.GetGuidOrDefault("ShoppingId") == template.Id).Sum(c => c.Properties.GetDecimalOrDefault("BuyCount"));    //已经购买的数量
                result = count;
            }
            return result;
        }

        private const string BuyRecordActionId = "商城购买";

        public void Save()
        {
            //if (null != _RefreshGoodsDatas)    //若可能发生变化
            //{
            //    ShoppingSlot.Properties[nameof(RefreshGoodsDatasDictionary)] = OwConvert.ToUriString(_RefreshGoodsDatas);
            //}
            UserContext?.SaveChanges();
        }

    }

}
