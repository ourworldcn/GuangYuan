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
            var rInfos = from tmp in view.RefreshInfos
                         let tm = Math.Clamp(tmp.Value.RefreshCount, 0, tmp.Value.CostOfGold.Length - 1)  //有效次数
                         select (tmp.Value.Genus, tmp.Value.CostOfGold[tm]);
            datas.RefreshGold.AddRange(rInfos);
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
            if (datas.Count < 0)   //若购买数量错误
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "购买数量必须大于0";
                return;
            }
            var view = new ShoppingSlotView(World, datas.GameChar, datas.Now);
            //校验可购买性
            if (!view.AllowBuy(template, datas.Count))    //若不可购买
            {
                datas.ErrorCode = VWorld.GetLastError();
                return;
            }
            //计算价格，修改资源
            var cost = GetCost(template, datas.Count, datas.GameChar);
            if (cost.Any(c => c.Item1.Count + c.Item2 < 0))
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "至少有一项货币不足。";
                return;
            }
            foreach (var item in cost.Where(c => c.Item2 != 0))
            {
                item.Item1.Count += item.Item2;
                datas.ChangeItems.AddToChanges(item.Item1);
            }
            //修改数据
            //生成物品
            var gim = World.ItemManager;
            var gi = new GameItem() { Count = datas.Count };
            World.EventsManager.GameItemCreated(gi, template.ItemTemplateId);
            var container = gim.GetDefaultContainer(datas.GameChar, gi);
            if (template.AutoUse)    //若自动使用
            {
                gim.ForcedAdd(gi, datas.GameChar.GetShoppingSlot()); //暂存到商城槽
                using var useItemsDatas = new UseItemsWorkDatas(World, datas.GameChar)
                {
                    UserContext = datas.UserContext,
                };
                useItemsDatas.ItemId = gi.Id;
                useItemsDatas.Count = (int)gi.Count.Value;
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
            //改写购买记录数据
            view.AddItem(template, datas.Count);
            view.Save();
            World.CharManager.NotifyChange(datas.GameChar.GameUser);
        }

        /// <summary>
        /// 获取售价。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="count">数量</param>
        /// <param name="gameChar"></param>
        /// <returns>售价的集合，可能是空集合，如签到礼包没有售价。</returns>
        private IEnumerable<(GameItem, decimal)> GetCost(GameShoppingTemplate template, decimal count, GameChar gameChar)
        {
            List<(GameItem, decimal)> result = new List<(GameItem, decimal)>();
            var gitm = World.ItemTemplateManager;
            if (template.Properties.ContainsKey("bd"))   //若有钻石售价
            {
                result.Add((gameChar.GetZuanshi(), -Math.Abs(template.Properties.GetDecimalOrDefault("bd")) * count));
            }
            else
            {
                var tt = gitm.GetTemplateFromeId(template.ItemTemplateId);
                if (tt != null && tt.Properties.ContainsKey("bd"))    //若有基础模板钻石售价
                {
                    result.Add((gameChar.GetZuanshi(), -Math.Abs(tt.Properties.GetDecimalOrDefault("bd")) * count));
                }
            }
            if (template.Properties.ContainsKey("bg"))   //若有金币售价
            {
                result.Add((gameChar.GetJinbi(), -Math.Abs(template.Properties.GetDecimalOrDefault("bg")) * count));
            }
            else
            {
                var tt = gitm.GetTemplateFromeId(template.ItemTemplateId);
                if (tt != null && tt.Properties.ContainsKey("bg"))    //若有基础模板金币售价
                {
                    result.Add((gameChar.GetJinbi(), -Math.Abs(tt.Properties.GetDecimalOrDefault("bg")) * count));
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
            var view = new ShoppingSlotView(World, datas.GameChar, datas.Now);
            var jinbi = datas.GameChar.GetJinbi();  //金币对象
            var totalCost = genus.Select(c =>   //总计金币代价
            {
                var data = view.RefreshInfos[c];
                var cost = Math.Abs(data.CostOfGold[Math.Clamp(data.RefreshCount, 0, data.CostOfGold.Length - 1)]);
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
                var cost = Math.Abs(data.CostOfGold[Math.Clamp(data.RefreshCount, 0, data.CostOfGold.Length - 1)]);
                jinbi.Count -= cost;
                //改写商品数据
                Refresh(view, item, datas.Now);
            }
            if (0 < genus.Count)
            {
                datas.ChangeItems.AddToChanges(jinbi);
                view.Save();
            }
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

        #region 卡池相关

        Dictionary<string, Dictionary<string, GameCardPoolTemplate[]>> _CardTemplates;

        /// <summary>
        /// 卡池Id 奖池Id两级分类的模板数据集合。
        /// </summary>
        public Dictionary<string, Dictionary<string, GameCardPoolTemplate[]>> CardTemplates
        {
            get
            {
                if (_CardTemplates is null)
                    lock (ThisLocker)
                        if (_CardTemplates is null)
                        {
                            var coll = from tmp in World.ItemTemplateManager.Id2CardPool.Values
                                       group tmp by tmp.CardPoolGroupString into g
                                       select (g.Key, g.GroupBy(c => c.SubCardPoolString));
                            _CardTemplates = coll.ToDictionary(c => c.Key, c =>
                                {
                                    return c.Item2.ToDictionary(d => d.Key, d => d.ToArray());
                                });

                        }
                return _CardTemplates;
            }
        }

        /// <summary>
        /// 获取指定时间点有效的卡池数据。
        /// 仅针对时间条件进行过滤，不考虑每个角色的不同情况。
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        public IEnumerable<GameCardPoolTemplate> GetCurrentCardPoolsCore(DateTime now)
        {
            var result = from tmp in World.ItemTemplateManager.Id2CardPool.Values
                         where tmp.GetStart(now) <= now && tmp.GetEnd(now) >= now
                         select tmp;
            return result;
        }

        /// <summary>
        /// 获取按日期过滤过的有效模板集合。
        /// </summary>
        /// <param name="templates"></param>
        /// <param name="nowUtc">在这个时间点有效的物品集合。</param>
        /// <returns></returns>
        public IEnumerable<GameCardPoolTemplate> GetTemplate(IEnumerable<GameCardPoolTemplate> templates, DateTime nowUtc)
        {
            var result = from tmp in World.ItemTemplateManager.Id2CardPool.Values
                         where tmp.GetStart(nowUtc) >= tmp.StartDateTime && tmp.GetEnd(nowUtc) <= tmp.EndDateTime
                         select tmp;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nowUtc"></param>
        /// <returns></returns>
        private Dictionary<string, ILookup<string, GameCardPoolTemplate>> GetCardPoolTemplate(DateTime nowUtc)
        {
            var coll = from tmp in GetCurrentCardPoolsCore(nowUtc)
                       group tmp by tmp.CardPoolGroupString;
            var result = coll.ToDictionary(c => c.Key, c =>
              {
                  return c.ToLookup(c => c.SubCardPoolString);
              });

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="datas">
        /// <see cref="ErrorCodes.RPC_S_OUT_OF_RESOURCES"/> 表示没有足够的抽奖卷。
        /// </param>
        public void Choujiang(ChoujiangDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null) //若无法锁定用户
                return;
            var coll = GetCurrentCardPoolsCore(DateTime.UtcNow).Where(c => c.CardPoolGroupString == datas.CardPoolId);
            //检查资源
            //实际抽奖
        }

        void Choujiang1(ChoujiangDatas datas)
        {
            var coll = GetCurrentCardPoolsCore(DateTime.UtcNow).Where(c => c.CardPoolGroupString == datas.CardPoolId);
            for (int i = 0; i < datas.LotteryTypeCount1; i++)
            {

            }
        }

        void Choujiang10(ChoujiangDatas datas, DateTime nowUtc)
        {
            var coll = from tmp in GetCurrentCardPoolsCore(DateTime.UtcNow)
                       where tmp.CardPoolGroupString == datas.CardPoolId
                       group tmp by tmp.SubCardPoolString;
            //计算概率
            var probs = coll.Select(c => (c.Key, c.First(c => c.Properties.ContainsKey("cpprob")).Properties.GetDecimalOrDefault("cpprob")));
            var probDenominator = probs.Sum(c => c.Item2);  //计算分母
            var probDic = probs.Select(c => (c.Key, c.Item2 / probDenominator)).ToDictionary(c => c.Key, c => c.Item2);    //加权后的概率
            //获取适用的模板
            var templates = CardTemplates;   //奖池的模板
            if (!templates.TryGetValue(datas.CardPoolId, out var subCardPool))    //若未找到卡池
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "没有找到指定的卡池。";
                return;
            }

            for (int i = 0; i < datas.LotteryTypeCount10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var idProb = OwHelper.RandomSelect(probDic, c => c.Value, VWorld.WorldRandom.NextDouble()); //命中的奖池
                    if(!subCardPool.TryGetValue(idProb.Key,out var tts))
                    {
                        datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                        datas.ErrorMessage = "没有找到指定的讲池。";
                        return;
                    }
                    var tt = tts.Skip(VWorld.WorldRandom.Next(tts.Count())).First();    //概率命中的模板
                    
                }
            }
        }

        #endregion 卡池相关
    }

    /// <summary>
    /// 
    /// </summary>
    public class ChoujiangDatas : ComplexWorkDatasBase
    {
        public ChoujiangDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public ChoujiangDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public ChoujiangDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 10连抽的次数。这个属性是1，代表一次10连抽，要消耗10个抽奖卷。
        /// </summary>
        public int LotteryTypeCount10 { get; set; }

        /// <summary>
        /// 单抽次数。
        /// </summary>
        public int LotteryTypeCount1 { get; set; }

        /// <summary>
        /// 卡池号。要在哪一个卡池内抽奖。
        /// </summary>
        public string CardPoolId { get; set; }

        #region 内部使用属性
        /// <summary>
        /// 使用的时间点。
        /// </summary>
        public DateTime NowUtc { get; set; } = DateTime.UtcNow;


        public Dictionary<string, Dictionary<string, GameCardPoolTemplate>> _Templates;
        public Dictionary<string, Dictionary<string, GameCardPoolTemplate>> Templates
        {
            get
            {
                if (_Templates is null)
                    lock (this)
                        if (_Templates is null)
                        {

                        }
                return _Templates;
            }
        }
        #endregion 内部使用属性

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

        List<(string, decimal)> _RefreshGold;
        /// <summary>
        /// 刷新信息。Item1是属名，Item2是刷新金币。
        /// </summary>
        public List<(string, decimal)> RefreshGold => _RefreshGold ??= new List<(string, decimal)>();
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

}
