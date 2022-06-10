using Game.Social;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using OW.Game;
using OW.Game.Item;
using OW.Game.PropertyChange;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
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
        /// 获取所有商品模板数据。
        /// </summary>
        /// <returns></returns>
        public IEnumerable<GameShoppingTemplate> GetAllShoppingTemplates()
        {
            return World.ItemTemplateManager.Id2Shopping.Values;
        }

        /// <summary>
        /// 获取当前用户可见的商城表，无效物品不会返回。
        /// </summary>
        /// <param name="datas"></param>
        public void GetList(GetListDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;

            IEnumerable<GameShoppingTemplate> coll; //不可刷新商品
            if (datas.Genus.Count == 0) //若显示全部页签商品
                coll = World.ItemTemplateManager.Id2Shopping.Values.Where(
                    c => !c.Genus.Contains("2001") && !c.Genus.Contains("2002") && !c.Genus.Contains("2003") && !c.Genus.Contains("2004") && !c.Genus.Contains("2005")).    //过滤掉工会商品
                    Where(c => IsValid(c, datas.Now) && c.GroupNumber is null);
            else //若显示指定页签商品
                coll = World.ItemTemplateManager.Id2Shopping.Values.Where(c => !c.Genus.Contains("2001") && !c.Genus.Contains("2002") && !c.Genus.Contains("2003") && !c.Genus.Contains("2004") && !c.Genus.Contains("2005")).Where(c => datas.Genus.Contains(c.Genus) && IsValid(c, datas.Now) && c.GroupNumber is null);
            //工会商品
            if (datas.Genus.Count == 0 || datas.Genus.Count > 0 && datas.Genus.Any(c => int.TryParse(c, out var tmp) && tmp >= 2000 && tmp < 3000)) //若需要工会商品
            {
                var guild = World.AllianceManager.GetGuild(datas.GameChar);
                if (guild != null)
                {
                    var lv = (int)guild.Properties.GetDecimalOrDefault(World.PropertyManager.LevelPropertyName);    //工会等级
                    var genus = (lv + 1 + 2000).ToString(); //对应的页签号
                    coll = coll.Concat(World.ItemTemplateManager.Id2Shopping.Values.Where(c => c.Genus == genus));  //追加当前公会等级的商城物品
                }
            }
            var view = new ShoppingSlotView(World, datas.GameChar, datas.Now);
            //可刷新商品
            List<GameShoppingTemplate> rg = new List<GameShoppingTemplate>();
            foreach (var item in view.RefreshInfos)  //遍历可刷新商品
            {
                var tmp = World.ItemTemplateManager.Id2Shopping.Values.Where(c => c.GroupNumber.HasValue && c.Genus == item.Key && (datas.Genus.Count == 0 || datas.Genus.Contains(c.Genus)) && c.GroupNumber == item.Value.GroupNumber && IsValid(c, datas.Now));
                rg.AddRange(tmp);
            }
            decimal tmpDec = 0;
            datas.ShoppingTemplates.AddRange(coll.Where(c => view.AllowBuy(c, 1) && AllowBuyWithConditional(datas.GameChar, c, datas.Now, ref tmpDec)));
            datas.ShoppingTemplates.AddRange(rg.Where(c => AllowBuyWithConditional(datas.GameChar, c, datas.Now, ref tmpDec)));
            var rInfos = from tmp in view.RefreshInfos
                         let tm = Math.Clamp(tmp.Value.RefreshCount, 0, tmp.Value.CostOfGold.Length - 1)  //有效次数
                         select (tmp.Value.Genus, tmp.Value.CostOfGold[tm]);
            datas.RefreshGold.AddRange(rInfos);
            view.Save();
            World.CharManager.NotifyChange(datas.GameChar.GameUser);
        }

        #region 计算允许购买的规则相关

        /// <summary>
        /// 获取在指定时间点是否可以购买。仅验证时效性。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="now"></param>
        /// <returns>true当前时间可以购买。false不可购买<see cref="VWorld.GetLastError"/>返回<see cref="ErrorCodes.ERROR_IMPLEMENTATION_LIMIT"/>。</returns>
        protected bool AllowBuy(GameShoppingTemplate template, DateTime now)
        {
            var start = template.GetStart(now);
            var end = template.GetEnd(now);
            var result = now >= start && now <= end;
            if (!result)
                VWorld.SetLastError(ErrorCodes.ERROR_IMPLEMENTATION_LIMIT);
            return result;
        }

        /// <summary>
        /// 获取是否可以购买，仅计算属性中指出的条件。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="template"></param>
        /// <param name="now"></param>
        /// <param name="count"></param>
        /// <returns>true允许购买，false不允许购买<see cref="VWorld.GetLastError"/>返回<see cref="ErrorCodes.ERROR_IMPLEMENTATION_LIMIT"/>。</returns>
        protected bool AllowBuyWithConditional(GameChar gameChar, GameShoppingTemplate template, DateTime now, ref decimal count)
        {
            foreach (var kv in template.Properties.Where(c => c.Key.StartsWith("rqgtq")))
            {
                if (!(kv.Value is string str))
                    continue;
                var ary = str.Split(OwHelper.SemicolonArrayWithCN);
                if (ary.Length != 2 || !decimal.TryParse(ary[1], out var dec))
                    continue;
                if (!(gameChar.Properties.GetDecimalOrDefault(ary[0]) >= dec))
                {
                    VWorld.SetLastError(ErrorCodes.ERROR_IMPLEMENTATION_LIMIT);
                    return false;
                }
            }
            return true;
        }

        protected bool AllowBuy(GameChar gameChar, GameShoppingTemplate template, DateTime now, ref decimal count)
        {
            if (!AllowBuy(template, now))
            {
                count = default;
                return false;
            }
            if (!AllowBuyWithConditional(gameChar, template, now, ref count))
                return false;
            return true;
        }
        #endregion 计算允许购买的规则相关

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
            var dec = 0m;
            //校验可购买性
            if (!view.AllowBuy(template, datas.Count) || !AllowBuyWithConditional(datas.GameChar, template, datas.Now, ref dec))    //若不可购买
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
            var items = World.ItemManager.ToGameItems(template.Properties, "").ToList();
            var gim = World.ItemManager;
            if (template.ItemTemplateId.HasValue)
            {
                var gi = new GameItem() { Count = datas.Count };
                World.EventsManager.GameItemCreated(gi, template.ItemTemplateId ?? Guid.Empty);
                items.Add(gi);
            }
            //移动物品
            if (template.AutoUse)    //若自动使用
            {
                foreach (var gi in items)
                {
                    gim.ForcedAdd(gi, datas.GameChar.GetShoppingSlot()); //暂存到商城槽
                    using var useItemsDatas = new UseItemsWorkDatas(World, datas.GameChar)
                    {
                        UserDbContext = datas.UserDbContext,
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
            }
            else //若非自动使用物品
            {
                List<GameItem> list = new List<GameItem>();
                List<GamePropertyChangeItem<object>> changes = new List<GamePropertyChangeItem<object>>();
                World.ItemManager.AddItems(datas.GameChar, items, list, changes);
                changes.CopyTo(datas.ChangeItems);
                if (list.Count > 0)    //若需要发送邮件
                {
                    var mail = new GameMail();
                    World.SocialManager.SendMail(mail, new Guid[] { datas.GameChar.Id }, SocialConstant.FromSystemId, list.Select(c => (c, World.EventsManager.GetDefaultContainer(c, datas.GameChar).TemplateId)));
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
                var tt = gitm.GetTemplateFromeId(template.ItemTemplateId ?? Guid.Empty);
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
                var tt = gitm.GetTemplateFromeId(template.ItemTemplateId ?? Guid.Empty);
                if (tt != null && tt.Properties.ContainsKey("bg"))    //若有基础模板金币售价
                {
                    result.Add((gameChar.GetJinbi(), -Math.Abs(tt.Properties.GetDecimalOrDefault("bg")) * count));
                }
            }
            if (template.Properties.ContainsKey("bmg"))  //若有公会币售价
            {
                result.Add((gameChar.GetGuildCurrency(), -Math.Abs(template.Properties.GetDecimalOrDefault("bmg")) * count));
            }
            else
            {
                var tt = gitm.GetTemplateFromeId(template.ItemTemplateId ?? Guid.Empty);
                if (tt != null && tt.Properties.ContainsKey("bmg"))    //若有基础模板公会币售价
                {
                    result.Add((gameChar.GetGuildCurrency(), -Math.Abs(tt.Properties.GetDecimalOrDefault("bmg")) * count));
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
        /// 获取指定时间点有效的卡池设置信息。
        /// </summary>
        /// <param name="nowUtc"></param>
        /// <returns></returns>
        public Dictionary<string, Dictionary<string, GameCardPoolTemplate[]>> GetCardTemplateDictionary(DateTime nowUtc)
        {
            var coll = from tmp in World.ItemTemplateManager.Id2CardPool.Values.Where(c => c.StartDateTime <= c.GetStart(nowUtc) && c.EndDateTime >= c.GetEnd(nowUtc))
                       group tmp by tmp.CardPoolGroupString into g
                       select (g.Key, g.GroupBy(c => c.SubCardPoolString));
            var result = coll.ToDictionary(c => c.Key, c =>
            {
                return c.Item2.ToDictionary(d => d.Key, d => d.ToArray());
            });
            return result;
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
            //获取适用的模板
            var templates = datas.Templates;   //奖池的模板
            if (templates.Count <= 0)    //若未找到卡池
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "没有找到指定的卡池。";
                return;
            }
            //校验资源
            var quan = datas.GameChar.GetChoujiangquan();
            if (quan is null || datas.LotteryTypeCount1 + datas.LotteryTypeCount10 * 10 > quan.Count.Value)
            {
                datas.ErrorCode = ErrorCodes.RPC_S_OUT_OF_RESOURCES;
                datas.ErrorMessage = "抽奖券不足。";
                return;
            }
            //实际抽奖
            Choujiang1(datas);
            Choujiang10(datas);
            //修改其他资源
            World.ItemManager.ForcedAddCount(quan, -(datas.LotteryTypeCount1 + datas.LotteryTypeCount10 * 10), datas.ChangeItems);
            ChangeItem.Reduce(datas.ChangeItems);
        }

        /// <summary>
        /// 矫正10抽卡池抽奖概率。
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="probs"></param>
        void AdjProb10(ChoujiangDatas datas, Dictionary<string, decimal> probs)
        {
            //计算概率
            var prob = probs.GetValueOrDefault("0");   //大奖概率
            var count = datas.GameChar.GetLotteryCount(null, false, datas.CardPoolId, "0");
            var adj = Math.Clamp(prob + (count - 70) * 0.1m, 0, 1);
            AdjustProb(probs, "0", adj);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="probs"></param>
        /// <param name="key"></param>
        /// <param name="prob">在[0,1]中</param>
        void AdjustProb(Dictionary<string, decimal> probs, string key, decimal prob)
        {
            Debug.Assert(prob >= 0 && prob <= 1);
            var old = probs.GetValueOrDefault(key); //获取旧值
            probs[key] = prob;  //设置新值
            var diff = old - prob;  //计算矫正值
            var coll = probs.Select(c =>
            {
                if (c.Key == key)
                    return (key, c.Value);
                var old = c.Value;
                diff -= AdjustProb(ref old, diff);
                return (c.Key, old);
            });
            var dic = coll.ToDictionary(c => c.Item1, c => c.Item2);
            probs.Clear();
            OwHelper.Copy(dic, probs);
        }

        /// <summary>
        /// 获取实际修正值。
        /// </summary>
        /// <param name="value">要修正的值。</param>
        /// <param name="diff">增量，可正可负。</param>
        /// <returns>实际矫正的值。</returns>
        decimal AdjustProb(ref decimal value, decimal diff)
        {
            var tmp = value + diff;
            var old = value;
            if (tmp > 1)
            {
                value = 1;
                return 1 - old;
            }
            else if (tmp < 0)
            {
                value = 0;
                return 0 - old;
            }
            else
            {
                value = tmp;
                return diff;
            }
        }
        /// <summary>
        /// 在确定命中某个模板后调用，以记录各种命中/未命中数据。
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="template"></param>
        void SetChoujiangCount(ChoujiangDatas datas, GameCardPoolTemplate template)
        {
            if (datas.Templates.TryGetValue("0", out var lv0))    //若有大奖卡池
            {
                if (lv0.Contains(template)) //若中大奖
                {
                    datas.GameChar.SetLotteryCount(null, false, datas.CardPoolId, "0", 0);
                }
                else //若未中大奖
                {
                    datas.GameChar.AddLotteryCount(null, false, datas.CardPoolId, "0", 1);
                }
            }
        }

        void Choujiang1(ChoujiangDatas datas)
        {
            if (datas.LotteryTypeCount1 <= 0)  //若不需要单抽
                return;
            var templates = datas.Templates;   //奖池的模板
            var hits = new List<GameCardPoolTemplate>(); //增加的物品列表
            //计算概率
            var probs = templates.Select(c => (c.Key, c.Value.First(d => d.Properties.ContainsKey("prob")).Properties.GetDecimalOrDefault("prob")));
            var probDenominator = probs.Sum(c => c.Item2);  //计算分母
            var probDic = probs.Select(c => (c.Key, c.Item2 / probDenominator)).ToDictionary(c => c.Key, c => c.Item2);    //加权后的概率
            for (int i = 0; i < datas.LotteryTypeCount1; i++)
            {
                var idProb = OwHelper.RandomSelect(probDic, c => c.Value, VWorld.WorldRandom.NextDouble()); //命中的奖池
                if (!templates.TryGetValue(idProb.Key, out var tts) || tts.Length <= 0)
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.ErrorMessage = "没有找到指定的奖池。";
                    return;
                }
                var tt = tts[VWorld.WorldRandom.Next(tts.Length)];    //概率命中的模板

                tt = ChoujiangRules(datas, tt);
                SetChoujiangCount(datas, tt);

                hits.Add(tt);
            }
            datas.ResultTemplateIds.AddRange(hits.Select(c => c.Id));
            UseCardPoolTemplates(datas, hits);
        }

        /// <summary>
        /// 10抽。
        /// </summary>
        /// <param name="datas"></param>
        void Choujiang10(ChoujiangDatas datas)
        {
            if (datas.LotteryTypeCount10 <= 0)  //若不需要10连抽
                return;
            //获取适用的模板
            var templates = datas.Templates;   //奖池的模板
            //计算概率
            var probs = templates.Select(c => (c.Key, c.Value.First(d => d.Properties.ContainsKey("prob")).Properties.GetDecimalOrDefault("prob")));
            var coll = GameMath.ToSum1(probs, c => c.Item2, (c, p) => (c.Key, p));  //规范化概率序列

            var probDic = coll.ToDictionary(c => c.Key, c => c.p);    //加权后的概率
            var hits = new List<GameCardPoolTemplate>(); //增加的物品列表
            for (int i = 0; i < datas.LotteryTypeCount10; i++)
            {
                hits.Clear();
                for (int j = 0; j < 10; j++)
                {
                    var tmp = new Dictionary<string, decimal>(probDic);
                    //AdjProb10(datas, tmp);
                    var idProb = OwHelper.RandomSelect(tmp, c => c.Value, VWorld.WorldRandom.NextDouble()); //命中的奖池
                    if (!templates.TryGetValue(idProb.Key, out var tts) || tts.Length <= 0)
                    {
                        datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                        datas.ErrorMessage = "没有找到指定的讲池。";
                        return;
                    }
                    var tt = tts[VWorld.WorldRandom.Next(tts.Length)];    //概率命中的模板

                    tt = ChoujiangRules(datas, tt);
                    SetChoujiangCount(datas, tt);
                    hits.Add(tt);
                }
                RulesAdj10(hits, datas.Templates);  //校验规则
                datas.ResultTemplateIds.AddRange(hits.Select(c => c.Id));
                UseCardPoolTemplates(datas, hits);
            }
        }

        /// <summary>
        /// 按10连抽规则校验中奖模板并修改。
        /// </summary>
        /// <param name="templates"></param>
        void RulesAdj10(List<GameCardPoolTemplate> templates, Dictionary<string, GameCardPoolTemplate[]> cardTemplates)
        {
            var lv0 = cardTemplates.GetValueOrDefault("0"); //大奖
            var lv1 = cardTemplates.GetValueOrDefault("1"); //二等奖
            var lv2 = cardTemplates.GetValueOrDefault("2"); //三等奖
            int lv0Count = 0;   //大奖数量
            templates.RemoveAll(c =>
            {
                if (lv0.Contains(c)) //若是大奖
                {
                    lv0Count++;
                    if (lv0Count > 2)
                    {
                        lv0Count = 2;
                        return true;
                    }
                }
                return false;
            });
            while (templates.Count < 10)    //补足10个奖品
                templates.Add(lv2[VWorld.WorldRandom.Next(lv2.Length)]);
            //规范二等奖
            int lv1Count = templates.Count(c => lv1.Contains(c)); //二等奖数量
            if (lv1Count <= 0) //若过少
            {
                var index = templates.Find(c => lv2.Contains(c)); //找一个末奖
                templates.Remove(index);
                templates.Add(lv1[VWorld.WorldRandom.Next(lv1.Length)]);
            }
            else if (lv1Count > 4) //若过多
            {
                lv1Count = 0;
                templates.RemoveAll(c =>
                {
                    if (lv1.Contains(c)) //若是二等奖
                    {
                        lv1Count++;
                        if (lv1Count > 4)
                        {
                            lv0Count = 4;
                            return true;
                        }
                    }
                    return false;
                }); //避免过多
                    //补足数量
                while (templates.Count < 10)    //补足10个奖品
                    templates.Add(lv2[VWorld.WorldRandom.Next(lv2.Length)]);
            }
        }

        /// <summary>
        /// 获取一组模板的物品。
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="templates"></param>
        void UseCardPoolTemplates(ChoujiangDatas datas, IEnumerable<GameCardPoolTemplate> templates)
        {
            var list = new List<GameItem>();
            var remainder = new List<GameItem>(); //无法放入的剩余物品
            var changes = new List<GamePropertyChangeItem<object>>();
            var bag = datas.GameChar.GetShoppingSlot();
            foreach (var tt in templates)
            {
                list.Clear();
                list.AddRange(World.ItemManager.ToGameItems(tt.Properties, "cp"));
                foreach (var item in list)
                {
                    if (tt.AutoUse)  //若需要自动使用
                    {
                        World.ItemManager.MoveItem(item, item.Count.Value, bag, remainder); //TO DO
                        World.ItemManager.UseItem(item, item.Count.Value, remainder, changes);
                    }
                    else //若无需自动使用
                    {
                        World.ItemManager.MoveItem(item, item.Count.Value, World.EventsManager.GetDefaultContainer(item, datas.GameChar), remainder, changes);
                    }
                }
            }
            datas.ResultItems.AddRange(remainder);
            datas.ResultItems.AddRange(changes.Where(c => c.IsCollectionAdded() && c.Object != bag && c.NewValue is GameItem).Select(c => c.NewValue as GameItem));
            datas.ResultItems.AddRange(changes.Where(c => !c.IsCollectionChanged() && c.Object != bag && c.PropertyName == World.PropertyManager.CountPropertyName && c.IsAdd()).
                Select(c => c.Object as GameItem));
            changes.CopyTo(datas.ChangeItems);
            if (remainder.Count > 0)   //若有需要发送邮件得物品
            {
                //发送邮件
                var mail = new GameMail()
                {
                };
                World.SocialManager.SendMail(mail, new Guid[] { datas.GameChar.Id },
                    SocialConstant.FromSystemId, remainder.Select(c => (c, World.EventsManager.GetDefaultContainer(c, datas.GameChar).TemplateId)));
            }
        }

        /// <summary>
        /// 执行矫正规则。
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        GameCardPoolTemplate ChoujiangRules(ChoujiangDatas datas, GameCardPoolTemplate template)
        {
            return template;
        }
        #endregion 卡池相关
    }

    /// <summary>
    /// 抽奖参数和返回值封装类。
    /// </summary>
    public class ChoujiangDatas : ChangeItemsWorkDatasBase
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

        DateTime _NowUtc = DateTime.UtcNow;
        /// <summary>
        /// 使用的时间点。
        /// </summary>
        public DateTime NowUtc
        {
            get => _NowUtc;

            set
            {
                if (_NowUtc != value)
                {
                    _NowUtc = value;
                    _Templates = null;
                }
            }
        }

        /// <summary>
        /// 本次抽奖命中的模板Id。
        /// </summary>
        public List<Guid> ResultTemplateIds { get; } = new List<Guid>();

        #region 内部使用属性

        public Dictionary<string, GameCardPoolTemplate[]> _Templates;

        /// <summary>
        /// 指定卡池指定时间点(<see cref="NowUtc"/>)有效的全部奖池模板。
        /// 键是奖池id,值模板数组。
        /// </summary>
        public Dictionary<string, GameCardPoolTemplate[]> Templates
        {
            get
            {
                if (_Templates is null)
                {
                    var coll = World.ItemTemplateManager.Id2CardPool.Values.Where(c => c.CardPoolGroupString == CardPoolId &&
                        NowUtc >= c.GetStart(NowUtc) && NowUtc <= c.GetEnd(NowUtc));
                    _Templates = coll.GroupBy(c => c.SubCardPoolString).ToDictionary(c => c.Key, c => c.ToArray());
                }
                return _Templates;
            }
        }

        List<GameItem> _ResultItems;
        public List<GameItem> ResultItems { get => _ResultItems ??= new List<GameItem>(); }

        #endregion 内部使用属性

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Templates = null;
                _ResultItems = null;
                base.Dispose(disposing);
            }
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
    public class GetListDatas : ComplexWorkGameContext
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
        public List<string> Genus { get; set; } = new List<string>();

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetShoppingSlot(this GameChar gameChar) =>
            gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ShoppingSlotTId);

        #region 卡池相关

        /// <summary>
        /// 获取抽奖券对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetChoujiangquan(this GameChar gameChar) =>
            gameChar.GetItemBag()?.Children?.FirstOrDefault(c => c.TemplateId == ProjectConstant.ChoujiangjuanTId);

        /// <summary>
        /// 抽奖相关的名称分隔符。
        /// </summary>
        const string SeparatorOfLottery = "_";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="lotteryType">null或空字符串表示所有抽法，1表示一抽，10表示10抽</param>
        /// <param name="isHit">true是命中，false是未命中。</param>
        /// <param name="cardPoolId">卡池的id。</param>
        /// <param name="subCardPoolId">子池id。</param>
        /// <returns></returns>
        public static int GetLotteryCount(this GameChar gameChar, string lotteryType, bool isHit, string cardPoolId, [AllowNull] string subCardPoolId = null)
        {
            var hit = isHit ? "h" : "uh";
            var name = $"cp{lotteryType ?? string.Empty}{SeparatorOfLottery}{hit}{SeparatorOfLottery}{cardPoolId ?? string.Empty}{SeparatorOfLottery}{subCardPoolId ?? string.Empty}{SeparatorOfLottery}";
            return (int)gameChar.Properties.GetDecimalOrDefault(name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="lotteryType">null或空字符串表示所有抽法，1表示一抽，10表示10抽</param>
        /// <param name="isHit">true是命中，false是未命中。</param>
        /// <param name="cardPoolId">卡池的id。</param>
        /// <param name="subCardPoolId">子池id。</param>
        /// <param name="count">次数。</param>
        public static void SetLotteryCount(this GameChar gameChar, string lotteryType, bool isHit, string cardPoolId, [AllowNull] string subCardPoolId, int count)
        {
            var hit = isHit ? "h" : "uh";
            var name = $"cp{lotteryType ?? string.Empty}{SeparatorOfLottery}{hit}{SeparatorOfLottery}{cardPoolId ?? string.Empty}{SeparatorOfLottery}{subCardPoolId ?? string.Empty}{SeparatorOfLottery}";
            gameChar.Properties[name] = (decimal)count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="lotteryType"></param>
        /// <param name="isHit"></param>
        /// <param name="cardPoolId"></param>
        /// <param name="subCardPoolId"></param>
        /// <param name="value">增量，可正可负，函数不校验该值。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddLotteryCount(this GameChar gameChar, string lotteryType, bool isHit, string cardPoolId, [AllowNull] string subCardPoolId, int value)
        {
            var i = gameChar.GetLotteryCount(lotteryType, isHit, cardPoolId, subCardPoolId) + value;
            gameChar.SetLotteryCount(lotteryType, isHit, cardPoolId, subCardPoolId, i);
        }

        #endregion 卡池相关

    }

}
