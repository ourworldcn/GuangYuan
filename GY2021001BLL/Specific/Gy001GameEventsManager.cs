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

        public Gy001GameEventsManager()
        {
        }

        public Gy001GameEventsManager(IServiceProvider service) : base(service)
        {
        }

        public Gy001GameEventsManager(IServiceProvider service, GameEventsManagerOptions options) : base(service, options)
        {
        }

        public override void Clone(GameChar src, GameChar dest)
        {
            base.Clone(src, dest);
            var bag = dest.GetHomeland().Children.First(c => c.TemplateId == ProjectConstant.HomelandPlanBagTId);   //方案背包
            var ep = bag.ExtendProperties.FirstOrDefault(c => c.Name == ProjectConstant.HomelandPlanPropertyName);
            if (ep != null)
            {
                bag.ExtendProperties.Remove(ep);
            }

        }
        public override void OnDynamicPropertyChanged(DynamicPropertyChangedCollection args)
        {
            base.OnDynamicPropertyChanged(args);
            var mrs = args.Where(c => c.Thing.TemplateId == ProjectConstant.MainControlRoomSlotId && c.Thing is GameItem); //主控室
            foreach (var item in mrs)
            {
                var gi = item.Thing as GameItem;
                foreach (var sunItem in item.Items.Where(c => c.Name == ProjectConstant.LevelPropertyName)) //若主控室升级了
                {
                    var newLv = gi.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);
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
                var gc = gi.GameChar;
                var hl = gc.GetHomeland();
                if (!hl.AllChildren.Contains(spcc.Thing))   //若不是家园建筑物
                    continue;
                foreach (var item in spcc)
                {
                    if (item.Name != World.PropertyManager.LevelPropertyName)    //若不是等级变化
                        continue;
                    //计算经验值增加量
                    var seq = gi.Template.GetSequenceProperty<decimal>("lut");
                    var time = seq[Convert.ToInt32(item.OldValue ?? 0)];
                    var exp = Math.Round(time / 60, MidpointRounding.ToZero);
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

                    if (lst != null && item.Name == "exp")
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
                    if (item.Name != World.PropertyManager.LevelPropertyName)   //若不是等级变化
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
                                World.CombatManager.UpdatePvpInfo(spcc.Thing as GameChar);
                                break;
                            default:
                                break;
                        }
                    }
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
            var chars = args.Where(c => IsLineup0(c)).Select(c => c.Thing).OfType<GameItem>().Select(c => c.GameChar).Distinct();
            Dictionary<string, double> dic = new Dictionary<string, double>();
            foreach (var gc in chars)   //遍历每个角色
            {
                decimal zhanli = 0;
                var gis = World.ItemManager.GetLineup(gc, 0);
                foreach (var gi in gis)
                {
                    World.CombatManager.UpdateAbility(gi, dic);
                    zhanli += (decimal)dic.GetValueOrDefault("abi");
                }
                var ep = gc.ExtendProperties.FirstOrDefault(c => c.Name == ProjectConstant.ZhangLiName);
                if (ep is null)
                {
                    ep = new GameExtendProperty()
                    {
                        Name = ProjectConstant.ZhangLiName,
                        DecimalValue = zhanli,
                        StringValue = gc.DisplayName,
                    };
                    gc.ExtendProperties.Add(ep);
                }
                else
                    ep.DecimalValue = zhanli;
            }
        }

        /// <summary>
        /// 是否是一个推关阵营的变化信息。
        /// </summary>
        /// <param name="coll"></param>
        /// <returns></returns>
        private bool IsLineup0(SimplePropertyChangedCollection coll)
        {
            return coll.Any(c => c.Name.StartsWith(ProjectConstant.ZhenrongPropertyName) && int.TryParse(c.Name[ProjectConstant.ZhenrongPropertyName.Length..], out var ln) && ln == 0);
        }

        #endregion 阵容相关

        public override void OnGameItemAdd(IEnumerable<GameItem> gameItems, Dictionary<string, object> parameters)
        {
            base.OnGameItemAdd(gameItems, parameters);
        }

        #region 创建后初始化

        public override void GameUserCreated(GameUser user, string loginName, string pwd, [AllowNull] DbContext context, [AllowNull] IReadOnlyDictionary<string, object> parameters)
        {
            base.GameUserCreated(user, loginName, pwd, context, parameters);
            var gc = new GameChar();
            user.GameChars.Add(gc);
            user.CurrentChar = gc;
            var gt = World.ItemTemplateManager.GetTemplateFromeId(ProjectConstant.CharTemplateId);
            GameCharCreated(gc, gt, user, parameters?.GetValueOrDefault(nameof(GameChar.DisplayName)) as string, new Dictionary<string, object>());
        }

        public override void GameCharCreated(GameChar gameChar, GameItemTemplate template, [AllowNull] GameUser user, [AllowNull] string displayName, [AllowNull] IReadOnlyDictionary<string, object> parameters)
        {
            base.GameCharCreated(gameChar, template, user, displayName, parameters);
            var gitm = World.ItemTemplateManager;
            var gim = World.ItemManager;
            var db = gameChar.GameUser.DbContext;
            if (string.IsNullOrWhiteSpace(gameChar.DisplayName))    //若没有指定昵称
            {
                string tmp;
                for (tmp = CnNames.GetName(VWorld.IsHit(0.5)); db.Set<GameChar>().Any(c => c.DisplayName == tmp); tmp = CnNames.GetName(VWorld.IsHit(0.5)))
                    ;
                gameChar.DisplayName = tmp;
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
        }

        /// <summary>
        /// 创建物品/道具。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="template"></param>
        /// <param name="parent"></param>
        /// <param name="ownerId"></param>
        /// <param name="parameters">针对坐骑，htid,btid都存在则自动创建相应的头和身体。</param>
        public override void GameItemCreated(GameItem gameItem, GameItemTemplate template, [AllowNull] GameItem parent, Guid? ownerId, [AllowNull] IReadOnlyDictionary<string, object> parameters = null)
        {
            base.GameItemCreated(gameItem, template, parent, ownerId, parameters);
            if (template.Id == ProjectConstant.ZuojiZuheRongqi && null != parameters)   //若是生物且可能有相应的初始化参数
            {
                var gitm = World.ItemTemplateManager;
                var htid = parameters.GetGuidOrDefault("htid");
                var headTemplate = gitm.GetTemplateFromeId(htid);
                if (headTemplate is null)
                    return;
                var btid = parameters.GetGuidOrDefault("btid");
                var bodyTemplate = gitm.GetTemplateFromeId(btid);
                if (bodyTemplate is null)
                    return;
                var head = new GameItem();
                GameItemCreated(head, headTemplate, gameItem, null, null);
                gameItem.Children.Add(head);

                var body = new GameItem();
                GameItemCreated(body, bodyTemplate, gameItem, null, null);
                gameItem.Children.Add(body);
            }
        }

        /// <summary>
        /// 创建物品/道具。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="parameters">理解tid,ptid,count,htid,btid,neatk,nemhp,neqlt属性，若htid,btid都存在则自动创建相应的头和身体。</param>
        public override void GameItemCreated([NotNull] GameItem gameItem, [NotNull] IReadOnlyDictionary<string, object> parameters)
        {
            base.GameItemCreated(gameItem, parameters);
            if (gameItem.TemplateId == ProjectConstant.ZuojiZuheRongqi)   //若是生物且可能有相应的初始化参数
            {
                var gitm = World.ItemTemplateManager;
                //获取头模板
                var htid = parameters.GetGuidOrDefault("htid");
                var headTemplate = gitm.GetTemplateFromeId(htid);
                if (headTemplate is null)
                    return;
                //获取身体模板
                var btid = parameters.GetGuidOrDefault("btid");
                var bodyTemplate = gitm.GetTemplateFromeId(btid);
                if (bodyTemplate is null)
                    return;
                //创建头对象
                var head = new GameItem() { };
                GameItemCreated(head, headTemplate, gameItem, null, null);
                head.Count = 1;
                gameItem.Children.Add(head);
                //创建身体对象
                var body = new GameItem();
                GameItemCreated(body, bodyTemplate, gameItem, null, null);
                body.Count = 1;
                gameItem.Children.Add(body);
                //处理资质数值
                if (parameters.TryGetValue("neatk", out var neatkObj) && OwConvert.TryGetDecimal(neatkObj, out var neatk))    //若指定了攻击资质
                    gameItem.Properties["neatk"] = neatk;
                if (parameters.TryGetValue("nemhp", out var nemhpObj) && OwConvert.TryGetDecimal(nemhpObj, out var nemhp))    //若指定了血量资质
                    gameItem.Properties["nemhp"] = nemhp;
                if (parameters.TryGetValue("neqlt", out var neqltObj) && OwConvert.TryGetDecimal(neqltObj, out var neqlt))    //若指定了质量资质
                    gameItem.Properties["neqlt"] = neqlt;
            }
        }
        #endregion 创建后初始化

        /// <summary>
        /// 未发送给客户端的数据保存在<see cref="GameThingBase.ExtendProperties"/>中使用的属性名称。
        /// </summary>
        public const string ChangesItemExPropertyName = "{BAD410C8-6393-44B4-9EB1-97F91ED11C12}";

        public override void GameCharLoaded(GameChar gameChar)
        {
            base.GameCharLoaded(gameChar);
            //未发送给客户端的数据
            var exProp = gameChar.ExtendProperties.FirstOrDefault(c => c.Name == ChangesItemExPropertyName);
            if (null != exProp)    //若有需要反序列化的对象
            {
                var tmp = JsonSerializer.Deserialize<List<ChangesItemSummary>>(exProp.Text);
                gameChar.ChangesItems.AddRange(ChangesItemSummary.ToChangesItem(tmp, gameChar));
            }
            //清除锁定属性槽内物品，放回道具背包中
            var gim = World.ItemManager;
            var daojuBag = gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.DaojuBagSlotId); //道具背包
            var slot = gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockAtkSlotId); //锁定槽
            gim.MoveItems(slot, c => true, daojuBag);
            slot = gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockMhpSlotId); //锁定槽
            gim.MoveItems(slot, c => true, daojuBag);
            slot = gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockQltSlotId); //锁定槽
            gim.MoveItems(slot, c => true, daojuBag);
            //挂接升级回调
            var hl = gameChar.GetHomeland();
            foreach (var item in hl.AllChildren)
            {
                if (!item.Name2FastChangingProperty.TryGetValue(ProjectConstant.UpgradeTimeName, out var fcp))
                    continue;

                var dt = fcp.GetComplateDateTime();
                var now = DateTime.UtcNow;
                TimeSpan ts;
                if (now >= dt)   //若已经超时
                    ts = TimeSpan.Zero;
                else
                    ts = dt - now;
                var tm = new Timer(World.BlueprintManager.LevelUpCompleted, ValueTuple.Create(gameChar.Id, item.Id), ts, Timeout.InfiniteTimeSpan);
            }
        }

        public override void GameUserLoaded(GameUser user, DbContext context)
        {
            base.GameUserLoaded(user, context);
            user.CurrentChar = user.GameChars[0];   //项目特定:一个用户有且仅有一个角色
            GameCharLoaded(user.CurrentChar);
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
