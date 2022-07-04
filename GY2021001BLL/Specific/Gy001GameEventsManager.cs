using Game.Social;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;

namespace OW.Game
{
    /// <summary>
    /// 
    /// </summary>
    public class ChangeData
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ChangeData()
        {
        }

        /// <summary>
        /// 行为Id，1增加，2更改，4删除
        /// </summary>
        public int ActionId { get; set; }

        /// <summary>
        /// 变化的对象Id。
        /// </summary>
        public Guid ObjectId { get; set; }

        /// <summary>
        /// 变化对象的模板Id。
        /// </summary>
        /// <remarks>{7396db31-1d02-43d3-af05-c14f4ca2a5fc}好友位模板Id表示好友。
        /// {0C741F97-12EC-4463-85B0-C1782656E853}邮件槽模板Id表示邮件。
        /// 0CF39269-6301-470B-8527-07AF29C5EEEC角色的模板Id表示角色。
        /// 其它是成就的模板Id,如{25FFBEE1-F617-49BD-B0DE-32B3E3E975CB}表示 玩家等级成就。
        /// </remarks>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 变化的属性名。
        /// 暂时未实现。
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// 变化之前的值。
        /// 暂时未实现。
        /// </summary>
        public object OldValue { get; set; }

        /// <summary>
        /// 变化之后的值。
        /// 暂时未实现。
        /// </summary>
        public object NewValue { get; set; }

        /// <summary>
        /// 附属数据。如用户等级变化时，这里有类似{"exp",12360}的指出变化后的经验值。
        /// </summary>
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// 创建此条数据的Utc时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 
    /// </summary>
    public class Gy001GameEventsManagerOptions : GameEventsManagerOptions
    {

    }

    /// <summary>
    /// 全局事件管理器。
    /// </summary>
    public class Gy001GameEventsManager : GameEventsManager
    {
        #region 构造函数及相关

        public Gy001GameEventsManager()
        {
            Initialize();
        }

        public Gy001GameEventsManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public Gy001GameEventsManager(IServiceProvider service, GameEventsManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        void Initialize()
        {
        }
        #endregion 构造函数及相关

        #region 物品相关

        /// <summary>
        /// 获取指定物品归属到指定角色时的首选默认容器。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="gChar"></param>
        /// <returns></returns>
        public override GameThingBase GetDefaultContainer(GameItem gameItem, GameChar gChar)
        {
            var result = base.GetDefaultContainer(gameItem, gChar);
            if (null != result && result.TemplateId != ProjectConstant.ZuojiBagSlotId)  //若有指定容器且不是坐骑槽
                return result;
            else if (null != result || gameItem.TemplateId == ProjectConstant.ZuojiZuheRongqi)    //若有默认容器且是坐骑槽
            {
                result = World.ItemManager.IsExistsMounts(gameItem, gChar) ? gChar.GetShoulanBag() : gChar.GetZuojiBag();
                return result;
            }
            else if (null != result)    //若已经有指定的默认容器
                return result;
            var template = World.ItemTemplateManager.GetTemplateFromeId(gameItem.TemplateId);
            switch (template.CatalogNumber)
            {
                case 0:
                    if (gameItem.TemplateId == ProjectConstant.ZuojiZuheRongqi) //若是坐骑/野兽
                    {
                        result = World.ItemManager.IsExistsMounts(gameItem, gChar) ? gChar.GetShoulanBag() : gChar.GetZuojiBag();
                    }
                    else
                        result = null;
                    break;
                case 10:
                    result = gChar.GetShenwenBag();
                    break;
                case 15:    //神纹强化道具
                case 16:    //神纹强化道具
                case 17:    //神纹强化道具
                case 18:    //道具
                    result = gChar.GetItemBag();
                    break;
                case 26:    //时装
                    result = gChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ShizhuangBagSlotId);
                    break;
                case 40:    //炮塔
                case 41:    //陷阱
                case 42:    //水晶
                case 43:    //木材仓库
                case 31:   //抓捕网，主控室，玉米田，木材田
                    result = gChar.GetHomelandBuildingBag();    //建筑背包
                    break;
                case 99:    //货币
                    result = gChar.GetCurrencyBag();
                    break;
                case 30:
                    result = gChar.GetTujianBag();
                    break;
                default:    //不认识物品
                    if (gameItem.IsDikuai())
                        result = gChar.GetHomeland();
                    else
                        result = gChar.GetItemBag();
                    break;
            }
            return result;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="gItem"></param>
        /// <returns></returns>
        public override bool IsAllowZero(GameItem gItem)
        {
            if (World.PropertyManager.IsStc(gItem, out _) && gItem.Parent != null && gItem.Parent.TemplateId == ProjectConstant.CurrencyBagTId)  //若是货币
                return true;
            return base.IsAllowZero(gItem);
        }

        #endregion 物品相关

        public override void Clone(GameChar src, GameChar dest)
        {
            base.Clone(src, dest);
            var bag = dest.GetHomeland().Children.First(c => c.TemplateId == ProjectConstant.HomelandStyleBagTId);   //方案背包
        }

        public override void OnDynamicPropertyChanged(DynamicPropertyChangedCollection args)
        {
            base.OnDynamicPropertyChanged(args);
            var mrs = args.Where(c => c.Thing.TemplateId == ProjectConstant.MainControlRoomSlotId && c.Thing is GameItem); //主控室
            foreach (var item in mrs)
            {
                var gi = item.Thing as GameItem;
                foreach (var sunItem in item.Where(c => c.PropertyName == World.PropertyManager.LevelPropertyName)) //若主控室升级了
                {
                    var newLv = gi.Properties.GetDecimalOrDefault(World.PropertyManager.LevelPropertyName);
                    var oldLv = Convert.ToDecimal(sunItem.OldValue);
                    if (newLv == oldLv + 1)
                    {

                    }
                    break;
                }
            }
            OnCharLevelUp(args);  //角色等级变化
            OnItemLevelUp(args);    //物品等级变化
            OnLineupChenged(args);
        }

        private void OnItemLevelUp(DynamicPropertyChangedCollection args)
        {
            foreach (var spcc in args.Where(c => c.Thing is GameItem)) //遍历针对角色的动态属性变化
            {
                var gi = (GameItem)spcc.Thing;
                var gc = gi.GetGameChar();
                var hl = gc.GetHomeland();
                if (!hl.GetAllChildren().Contains(spcc.Thing))   //若不是家园建筑物
                    continue;
                foreach (var item in spcc)
                {
                    if (item.PropertyName != World.PropertyManager.LevelPropertyName)    //若不是等级变化
                        continue;
                    //计算经验值增加量
                    var seq = gi.GetTemplate().GetSequenceProperty<decimal>("lut");
                    var time = seq[Convert.ToInt32(item.OldValue ?? 0)];
                    var exp = Math.Round(time / 600, MidpointRounding.ToPositiveInfinity);   //TO DO建筑升级结束时增加玩家经验值,应属性化控制
                    World.CharManager.AddExp(gc, exp);  //增加经验值
                }
            }
        }

        /// <summary>
        /// 角色升级。
        /// </summary>
        /// <param name="e"></param>
        private void OnCharLevelUp(DynamicPropertyChangedCollection e)
        {
            foreach (var spcc in e.Where(c => c.Thing is GameChar)) //遍历针对角色的动态属性变化
            {
                foreach (var item in spcc)
                {
                    var gc = (GameChar)spcc.Thing;
                    var lst = World.CharManager.GetChangeData(gc);  //通知数据对象

                    if (lst != null && item.PropertyName == "exp")
                    {
                        var np = new ChangeData()
                        {
                            ActionId = 2,
                            NewValue = item.NewValue,
                            ObjectId = gc.Id,
                            OldValue = item.OldValue,
                            PropertyName = "exp",
                            TemplateId = ProjectConstant.CharTemplateId,
                        };
                        np.Properties.Add("exp", gc.Properties.GetDecimalOrDefault("exp"));
                        lst.Add(np);
                    }
                    else
                    {
                        //TO DO
                    }
                    if (item.PropertyName != World.PropertyManager.LevelPropertyName)   //若不是等级变化
                        continue;
                    var oldLv = item.HasOldValue ? Convert.ToInt32(item.OldValue) : 0;
                    var newLv = item.HasNewValue ? Convert.ToInt32(item.NewValue) : 0;
                    if (oldLv >= newLv)    //若不是等级增加了，容错
                        continue;
                    for (int i = oldLv; i < newLv; i++) //遍历每个增加的等级
                    {
                        switch (i)
                        {
                            case 3: //玩家等级5（游戏流程第3天)，PVP模式，好友系统
                                //TODO World.CombatManager.UpdatePvpInfo(spcc.Thing as GameChar);
                                break;
                            default:
                                break;
                        }
                    }
                    gc.ExtraString = newLv.ToString("D10");
                    //生成通知数据。
                    if (lst != null)
                    {
                        var np = new ChangeData()
                        {
                            ActionId = 2,
                            NewValue = newLv,
                            ObjectId = gc.Id,
                            OldValue = oldLv,
                            PropertyName = World.PropertyManager.LevelPropertyName,
                            TemplateId = ProjectConstant.CharTemplateId,
                        };
                        lst.Add(np);
                    }
                    else
                    {
                        //TO DO
                    }
                }
            }
        }

        #region 阵容相关
        private void OnLineupChenged(DynamicPropertyChangedCollection args)
        {
        }

        #endregion 阵容相关

        #region 创建后初始化

        public override void OnGameItemAdd(IEnumerable<GameItem> gameItems, Dictionary<string, object> parameters)
        {
            base.OnGameItemAdd(gameItems, parameters);
        }

        public override void GameUserCreated(GameUser user, string loginName, string pwd, [AllowNull] DbContext context, [AllowNull] IReadOnlyDictionary<string, object> parameters)
        {
            base.GameUserCreated(user, loginName, pwd, context, parameters);
            var gc = new GameChar();
            user.GameChars.Add(gc);
            user.CurrentChar = gc;
            var gt = World.ItemTemplateManager.GetTemplateFromeId(ProjectConstant.CharTemplateId);
            GameCharCreated(gc, gt, user, parameters?.GetValueOrDefault(nameof(GameChar.DisplayName)) as string, new Dictionary<string, object>());
        }

        public override void GameCharCreated(GameChar gameChar, GameItemTemplate template, [AllowNull] GameUser user, [AllowNull] string displayName,
            [AllowNull] IReadOnlyDictionary<string, object> parameters)
        {
            base.GameCharCreated(gameChar, template, user, displayName, parameters);
            var gitm = World.ItemTemplateManager;
            var gim = World.ItemManager;
            var db = gameChar.GameUser.DbContext;
            if (string.IsNullOrWhiteSpace(gameChar.DisplayName))    //若没有指定昵称
            {
                //string tmp;
                //for (tmp = CnNames.GetName(VWorld.IsHit(0.5)); db.Set<GameChar>().Any(c => c.DisplayName == tmp); tmp = CnNames.GetName(VWorld.IsHit(0.5)))
                gameChar.DisplayName = World.CharManager.GetNewDisplayName(gameChar.GameUser);
            }
            //修正木材存贮最大量
            //var mucai = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.MucaiId);
            //var stcMucai = mucai.GetStc();
            //if (stcMucai < decimal.MaxValue)
            //{
            //    var mucaiStore = gameChar.GetHomeland().Children.Where(c => c.TemplateId == ProjectConstant.MucaiStoreTId);
            //    var stcs = mucaiStore.Select(c => c.GetStc());
            //    if (stcs.Any(c => c == decimal.MaxValue))   //若有任何仓库是最大堆叠
            //        mucai.SetPropertyValue(ProjectConstant.StackUpperLimit, -1);
            //    else
            //        mucai.SetPropertyValue(ProjectConstant.StackUpperLimit, stcs.Sum() + stcMucai);
            //}
            //增加坐骑
            var mountsBagSlot = gameChar.GetZuojiBag();   //坐骑背包槽
            for (int i = 3001; i < 3002; i++)   //仅增加羊坐骑
            {
                var headTemplate = gitm.Id2Template.Values.FirstOrDefault(c => c.GId.GetValueOrDefault() == i);
                var bodyTemplate = gitm.Id2Template.Values.FirstOrDefault(c => c.GId.GetValueOrDefault() == 1000 + i);
                var mounts = gim.CreateMounts(headTemplate, bodyTemplate);
                gim.ForcedAdd(mounts, mountsBagSlot);
            }
            //将第一个坐骑放入家园展示
            var showMount = mountsBagSlot.Children.FirstOrDefault();
            if (null != showMount)  //若有坐骑
            {
                var dic = showMount?.Properties;
                if (dic != null)
                    dic["for10"] = 0;
                GameSocialRelationship gsr = new GameSocialRelationship()
                {
                    Id = gameChar.Id,
                    Id2 = gim.GetBody(showMount).TemplateId,
                    KeyType = SocialConstant.HomelandShowKeyType,
                };
                db.Add(gsr);
                //增加阵容数据 for0, for1, for2, for10 = 0
                showMount.Properties["for0"] = 0m;
                showMount.Properties["for1"] = 0m;
                showMount.Properties["for2"] = 0m;
            }
            //加入日志
            var ar = new GameActionRecord
            {
                ParentId = gameChar.Id,
                ActionId = "Created",
                PropertiesString = $"CreateBy=CreateChar",
            };
            World.AddToUserContext(new object[] { ar });
            //增加推关战力
            World.CombatManager.UpdatePveInfo(gameChar);
            //不可加入pvp排名信息
            //World.CombatManager.UpdatePvpInfo(gameChar);
            World.ItemManager.ComputeMucaiStc(gameChar);
        }

        private static readonly string[] _GameItemCreatedKeyNames = new string[] { "htid", "htt", "btid", "btt", "neatk", "nemhp", "neqlt" };
        /// <summary>
        /// 创建物品/道具。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="propertyBag">理解"htid", "htt", "btid", "btt", "neatk", "nemhp", "neqlt" 属性，
        /// htt优先于htid,btt优先于btid，头身都存在则自动创建相应的头和身体。</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void GameItemCreated([NotNull] GameItem gameItem, [NotNull] IReadOnlyDictionary<string, object> propertyBag)
        {
            base.GameItemCreated(gameItem, propertyBag);
            if (gameItem.TemplateId == ProjectConstant.ZuojiZuheRongqi)   //若是生物且可能有相应的初始化参数
            {
                var gitm = World.ItemTemplateManager;

                #region 处理资质数值
                if (propertyBag.TryGetDecimal("neatk", out var neatk))    //若指定了攻击资质
                    gameItem.Properties["neatk"] = neatk;
                if (propertyBag.TryGetDecimal("nemhp", out var nemhp))    //若指定了血量资质
                    gameItem.Properties["nemhp"] = nemhp;
                if (propertyBag.TryGetDecimal("neqlt", out var neqlt))    //若指定了质量资质
                    gameItem.Properties["neqlt"] = neqlt;
                #endregion 处理资质数值

                #region  处理随机资质数据
                var nneatk = 0m;
                bool b = propertyBag.TryGetDecimal("nneatk", out nneatk);
                var mneatk = 100m;
                b |= propertyBag.TryGetDecimal("mneatk", out mneatk);
                if (b)   //若需要随机资质值
                    gameItem.Properties["neatk"] = (decimal)VWorld.WorldRandom.Next((int)nneatk, (int)mneatk + 1);

                var nnemhp = 0m;
                b = propertyBag.TryGetDecimal("nnemhp", out nnemhp);
                var mnemhp = 100m;
                b |= propertyBag.TryGetDecimal("mnemhp", out mnemhp);
                if (b)   //若需要随机资质值
                    gameItem.Properties["nemhp"] = (decimal)VWorld.WorldRandom.Next((int)nnemhp, (int)mnemhp + 1);

                var nneqlt = 0m;
                b = propertyBag.TryGetDecimal("nneqlt", out nneqlt);
                var mneqlt = 100m;
                b |= propertyBag.TryGetDecimal("mneqlt", out mneqlt);
                if (b)   //若需要随机资质值
                    gameItem.Properties["neqlt"] = (decimal)VWorld.WorldRandom.Next((int)nneqlt, (int)mneqlt + 1);
                #endregion 处理随机资质数据

                #region 处理身体和头的数据
                //获取头模板
                GameItemTemplate htt = null;
                if (propertyBag.TryGetValue("htt", out var httObj))   //若直接找到了头模板
                    htt = httObj as GameItemTemplate;
                if (htt is null && propertyBag.TryGetGuid("htid", out var htid))   //若找到头模板id
                    htt = gitm.GetTemplateFromeId(htid);
                //获取身体模板
                GameItemTemplate btt = null;
                if (propertyBag.TryGetValue("htt", out var bttObj))   //若直接找到了头模板
                    btt = bttObj as GameItemTemplate;
                if (btt is null && propertyBag.TryGetGuid("btid", out var btid))   //若找到头模板id
                    btt = gitm.GetTemplateFromeId(btid);
                if (null != btt && null != htt)   //若头身模板都已找到
                {
                    var head = new GameItem() { };    //头对象
                    var body = new GameItem() { };    //身体对象
                    gameItem.Children.Add(head);
                    gameItem.Children.Add(body);
                    var subDic = new Dictionary<string, object>(propertyBag);   //属性包
                    foreach (var item in _GameItemCreatedKeyNames)  //去掉已经识别处理的属性
                        subDic.Remove(item);
                    subDic["parent"] = gameItem;    //指向自己
                    subDic["count"] = 1m;    //数量
                    //初始化头对象
                    subDic["tt"] = htt;
                    GameItemCreated(head, subDic);
                    //初始化身体对象
                    subDic["tt"] = btt;
                    GameItemCreated(body, subDic);
                }
                #endregion 处理身体和头的数据
            }
        }
        #endregion 创建后初始化

        #region 转换为字典属性包

        /// <summary>
        /// 将指定对象的主要属性提取到指定字典中，以备可以使用<see cref="GameItemCreated(GameItem, IReadOnlyDictionary{string, object})"/>进行恢复。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="propertyBag">额外可以处理 neatk，nemhp，neqlt属性。对生物额处理neqlt，btid</param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        public override void Copy(GameItem gameItem, IDictionary<string, object> propertyBag, string prefix = null, string suffix = null)
        {
            //"htid", "htt", "btid", "btt", "neatk", "nemhp", "neqlt"
            base.Copy(gameItem, propertyBag, prefix, suffix);
            if (World.ItemManager.IsMounts(gameItem))   //若是生物且可能有相应的初始化参数
            {
                prefix ??= string.Empty;
                suffix ??= string.Empty;
                var props = gameItem.Properties;
                //处理资质数值
                if (props.TryGetDecimal("neatk", out var neatk))    //若指定了攻击资质
                    propertyBag[$"{prefix}neatk{suffix}"] = neatk;
                if (props.TryGetDecimal("nemhp", out var nemhp))    //若指定了血量资质
                    propertyBag[$"{prefix}nemhp{suffix}"] = nemhp;
                if (props.TryGetDecimal("neqlt", out var neqlt))    //若指定了质量资质
                    propertyBag[$"{prefix}neqlt{suffix}"] = neqlt;
                propertyBag[$"{prefix}htid{suffix}"] = World.ItemManager.GetHeadTemplate(gameItem).IdString;
                propertyBag[$"{prefix}btid{suffix}"] = World.ItemManager.GetBodyTemplate(gameItem).IdString;
            }
        }
        #endregion 转换为字典属性包

        /// <summary>
        /// 未发送给客户端的数据保存在<see cref="GameThingBase.ExtendProperties"/>中使用的属性名称。
        /// </summary>
        public const string ChangesItemExPropertyName = "{BAD410C8-6393-44B4-9EB1-97F91ED11C12}";

        #region 加载对象相关

        public override void GameCharLoaded(GameChar gameChar)
        {
            var now = DateTime.UtcNow;
            base.GameCharLoaded(gameChar);
            //清除锁定属性槽内物品，放回道具背包中
            var gim = World.ItemManager;
            var daojuBag = gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.DaojuBagSlotId); //道具背包

            gim.ResetSlot(gameChar);
            //挂接升级回调
            var hl = gameChar.GetHomeland();
            foreach (var item in hl.GetAllChildren())
            {
                if (!item.Name2FastChangingProperty.TryGetValue(ProjectConstant.UpgradeTimeName, out var fcp))
                    continue;

                var dt = fcp.GetComplateDateTime();
                TimeSpan ts;
                if (now >= dt)   //若已经超时
                    ts = TimeSpan.Zero;
                else
                    ts = dt - now;
                var tm = new Timer(World.BlueprintManager.LevelUpCompleted, ValueTuple.Create(gameChar.Id, item.Id), ts, Timeout.InfiniteTimeSpan);
            }
            World.ItemManager.ComputeMucaiStc(gameChar);
            //复位角色级别缓存字符串
            var lv = (int)gameChar.Properties.GetDecimalOrDefault(World.PropertyManager.LevelPropertyName);
            gameChar.ExtraString = lv.ToString("D10");
        }

        public override void GameUserLoaded(GameUser user, DbContext context)
        {
            base.GameUserLoaded(user, context);
            user.CurrentChar = user.GameChars[0];   //项目特定:一个用户有且仅有一个角色
            GameCharLoaded(user.CurrentChar);
        }

        #endregion 加载对象相关

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="gameChar"></param>
        public override void GameCharLogined(GameChar gameChar)
        {
            var now = DateTime.UtcNow;
            base.GameCharLogined(gameChar);
            //复位pvp信息
            World.SocialManager.ResetPvpObject(gameChar, now);
            //复位塔防信息
            var td = World.ItemManager.GetOrCreateItem(gameChar.GetCurrencyBag(), ProjectConstant.PveTCounterTId);
            var vo = td.Properties.GetDateTimeOrDefault("ltlv");

            if (vo.Date != now.Date)    //若今日没有有数据
            {
                td.Count = 1;
                td.Properties["ltlv"] = now.ToString();
                World.ItemManager.SetLevel(td, 1);
                td.Properties[World.PropertyManager.LevelPropertyName] = 1m;
            }
        }
        #region Json反序列化
        public override void JsonDeserialized(GameUser gameUser)
        {
            gameUser.CurrentChar = gameUser.GameChars.FirstOrDefault();
            base.JsonDeserialized(gameUser);
        }
        #endregion Json反序列化
    }

    public static class Gy001GameEventsManagerExtensions
    {
        public static void MountsCreated(this GameEventsManager manager, GameItem gameItem, GameThingBase parent, GameItemTemplate headTemplate, GameItemTemplate bodyTemplate)
        {
            manager.GameItemCreated(gameItem, null, parent is GameItem item ? item : null, parent is GameItem ? (Guid?)null : parent.Id,
                new Dictionary<string, object>() { { "ht", headTemplate }, { "bt", bodyTemplate } });
        }
    }
}
