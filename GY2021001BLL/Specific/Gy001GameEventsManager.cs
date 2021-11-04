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
            CharLevelUp(args);  //角色等级变化
            OnLineupChenged(args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        private void CharLevelUp(DynamicPropertyChangedCollection e)
        {
            foreach (var spcc in e.Where(c => c.Thing is GameChar)) //遍历针对角色的动态属性变化
            {
                foreach (var item in spcc)
                {
                    if (item.Name != World.PropertyManager.LevelPropertyName)   //若不是等级变化
                        continue;
                    var oldLv = item.HasOldValue ? (int)item.OldValue : 0;
                    var newLv = item.HasNewValue ? (int)item.NewValue : 0;
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
            //生成缓存数据
            var sep = new CharSpecificExpandProperty
            {
                CharLevel = (int)gc.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName),
                LastPvpScore = 1000,
                PvpScore = 1000,
                Id = gc.Id,
                GameChar = gc,
                FrinedCount = 0,
                FrinedMaxCount = 10,
                LastLogoutUtc = DateTime.UtcNow,
            };
            gc.SpecificExpandProperties = sep;
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
        /// 
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
            //加载扩展属性
            gameChar.SpecificExpandProperties = gameChar.DbContext.Set<CharSpecificExpandProperty>().Find(gameChar.Id);
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
    }

    public static class Gy001GameEventsManagerExtensions
    {
        public static void MountsCreated(this GameEventsManager manager, GameItem gameItem, GameThingBase parent, GameItemTemplate headTemplate, GameItemTemplate bodyTemplate)
        {
            manager.GameItemCreated(gameItem, null, parent is GameItem ? (GameItem)parent : null, parent is GameItem ? (Guid?)null : parent.Id,
                new Dictionary<string, object>() { { "ht", headTemplate }, { "bt", bodyTemplate } });
        }
    }
}
