using AutoMapper;
using Game.Social;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using OW.DDD;
using OW.Game;
using OW.Game.Caching;
using OW.Game.Item;
using OW.Game.Log;
using OW.Game.Mission;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GuangYuan.GY001.BLL
{
    public class CombatProfile : Profile
    {
        public CombatProfile()
        {

        }
    }

    /// <summary>
    /// 战斗管理器的配置类。
    /// </summary>
    public class GameCombatManagerOptions
    {
        public GameCombatManagerOptions()
        {
        }

        /// <summary>
        /// 战斗开始时被调用。
        /// </summary>
        public Func<IServiceProvider, StartCombatData, bool> CombatStart { get; set; }

        /// <summary>
        /// 战斗结束时被调用。返回true表示
        /// </summary>
        public Func<IServiceProvider, EndCombatData, bool> CombatEnd { get; set; }
    }

    /// <summary>
    /// 请求结束战斗的数据封装类。
    /// </summary>
    public class EndCombatData
    {
        public EndCombatData()
        {
        }

        /// <summary>
        /// 角色对象。
        /// </summary>
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要终止的关卡模板。
        /// </summary>
        public GameItemTemplate Template { get; set; }

        /// <summary>
        /// 此关卡的收益。
        /// </summary>
        public List<GameItem> GameItems { get; } = new List<GameItem>();

        /// <summary>
        /// 获取或设置一个指示，当这个属性为true时，仅记录收益，并核准。不会试图结束当前关卡。此时忽略其他请求退出的属性。
        /// </summary>
        public bool OnlyMark { get; set; }

        /// <summary>
        /// 角色是否退出，true强制在结算后退出当前大关口，false试图继续(如果已经是最后一关则不起作用——必然退出)。
        /// </summary>
        public bool EndRequested { get; set; }

        /// <summary>
        /// 自动进入的下一关卡模板。null表示错误的请求或已经自然结束。返回时填写。
        /// </summary>
        public GameItemTemplate NextTemplate { get; set; }

        /// <summary>
        /// 返回时指示是否有错误。false表示正常计算完成，true表示规则校验认为有误。返回时填写。
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// 调试信息。调试状态下返回时填写。
        /// </summary>
        public string DebugMessage { get; set; }

        /// <summary>
        /// 获取变化物品的数据。仅当结算大关卡时这里才有数据。
        /// </summary>
        public List<ChangeItem> ChangesItems { get; set; } = new List<ChangeItem>();

        /// <summary>
        /// 是否赢了该关卡。最后一小关或大关结算时，此数据才有效。
        /// </summary>
        public bool IsWin { get; set; }
    }

    /// <summary>
    /// 请求开始战斗的数据封装类
    /// </summary>
    public class StartCombatData : GameCharGameContext
    {
        public StartCombatData([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public StartCombatData([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public StartCombatData([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 要启动的关卡。返回时可能更改为实际启动的小关卡（若指定了大关卡）。
        /// </summary>
        public GameItemTemplate Template { get; set; }

    }

    /// <summary>
    /// 用于存放关卡数据的类。
    /// </summary>
    public class DungeonsData
    {
        public DungeonsData()
        {

        }

        public GameItemTemplate Template { get; set; }

        public int Kind { get; set; }

        /// <summary>
        /// 关卡Id。
        /// </summary>
        public int DungeonsId { get; set; }

        /// <summary>
        /// 小关索引。
        /// </summary>
        public int GateIndex { get; set; }

    }

    /// <summary>
    /// 战斗管理器。
    /// </summary>
    public class GameCombatManager : GameManagerBase<GameCombatManagerOptions>
    {

        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameCombatManager() : base()
        {
            Initialize();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="serviceProvider"></param>
        public GameCombatManager(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Initialize();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="options"></param>
        public GameCombatManager(IServiceProvider serviceProvider, GameCombatManagerOptions options) : base(serviceProvider, options)
        {
            Initialize();
        }

        private void Initialize()
        {

        }

        #endregion 构造函数

        /// <summary>
        /// 所有关卡的集合。
        /// </summary>
        private List<GameItemTemplate> _Dungeons;

        /// <summary>
        /// 所有副本信息。
        /// </summary>
        public IReadOnlyList<GameItemTemplate> Dungeons
        {
            get
            {
                lock (ThisLocker)
                    if (null == _Dungeons)
                    {
                        var gitm = World.ItemTemplateManager;
                        var coll = from tmp in gitm.Id2Template.Values
                                   where tmp.TryGetSdp("typ", out _) && tmp.TryGetSdp("mis", out _) && tmp.TryGetSdp("sec", out _)
                                   select tmp;
                        _Dungeons = coll.ToList();
                    }
                return _Dungeons;
            }
        }

        private Dictionary<GameItemTemplate, GameItemTemplate[]> _Parent2Children;
        /// <summary>
        /// 键是大关卡的模板，值所包含小关卡的模板数组。
        /// </summary>
        public IReadOnlyDictionary<GameItemTemplate, GameItemTemplate[]> Parent2Children
        {
            get
            {
                lock (ThisLocker)
                    if (null == _Parent2Children)
                    {
                        var coll = from tmp in Dungeons
                                   let typ = (int)tmp.GetSdpDecimalOrDefault("typ")
                                   let mis = (int)tmp.GetSdpDecimalOrDefault("mis")
                                   group tmp by (typ, mis) into g
                                   let parent = g.First(c => c.GetSdpDecimalOrDefault("sec") == -1)
                                   let children = g.Where(c => c.GetSdpDecimalOrDefault("sec") != -1).OrderBy(c => c.GetSdpDecimalOrDefault("sec"))
                                   select new { key = parent, vals = children };
                        _Parent2Children = coll.ToDictionary(c => c.key, c => c.vals.ToArray());
                    }
                return _Parent2Children;
            }
        }

        private Dictionary<GameItemTemplate, GameItemTemplate> _Child2Parent;

        /// <summary>
        /// 键是小关卡模板，值所属的大关卡模板。
        /// </summary>
        public IReadOnlyDictionary<GameItemTemplate, GameItemTemplate> Child2Parent
        {
            get
            {
                lock (ThisLocker)
                    if (null == _Child2Parent)
                    {
                        var coll = from tmp in Parent2Children
                                   from child in tmp.Value
                                   select new { key = child, val = tmp.Key };
                        _Child2Parent = coll.ToDictionary(c => c.key, c => c.val);
                    }
                return _Child2Parent;
            }
        }

        /// <summary>
        /// 取下一关的模板。
        /// </summary>
        /// <param name="template">关卡模板，如果是大关则立即返回第一个小关的模板。</param>
        /// <returns>下一关的模板，null则说明是最后一关(没有下一关了)。</returns>
        public GameItemTemplate GetNext(GameItemTemplate template)
        {
            if (Parent2Children.TryGetValue(template, out GameItemTemplate[] children))  //若是大关
                return children[0];
            var parent = Child2Parent[template];    //取大关模板
            children = Parent2Children[parent]; //取关卡模板序列
            int index = Array.FindIndex(children, c => c == template);  //取索引
            if (index >= children.Length - 1)    //若是最后一关
                return null;
            return children[index + 1];
        }

        /// <summary>
        /// 获取指定关卡的大关卡。
        /// </summary>
        /// <param name="template">如果本身就是大关卡，则立即返回自身。</param>
        /// <returns>没有合适的关卡则返回null。</returns>
        public GameItemTemplate GetParent(GameItemTemplate template)
        {
            if (Parent2Children.ContainsKey(template))  //若本身就是大关卡
                return template;
            if (Child2Parent.TryGetValue(template, out GameItemTemplate result))    //若找到大关卡
                return result;
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="dungeon">场景。</param>
        /// <returns>true正常启动，false没有找到指定的场景或角色当前正在战斗。</returns>
        public void StartCombat(StartCombatData data)
        {
            using var dwUser = data.LockUser();
            if (dwUser is null)
            {
                data.DebugMessage = "令牌无效。";
                data.HasError = true;
                return;
            }
            var cm = World.CombatManager;
            var gameChar = data.GameChar;
            var parent = cm.GetParent(data.Template); //大关
            if (data.Template == parent)    //若指定了大关则变为第一小关
            {
                data.Template = Parent2Children[parent][0];
            }
            if (gameChar.CurrentDungeonId.HasValue && gameChar.CurrentDungeonId != data.Template.Id)
            {
                data.DebugMessage = "错误的关卡Id";
                data.HasError = true;
                return;
            }
            try
            {
                if (!Options?.CombatStart?.Invoke(Service, data) ?? true)
                {
                    data.HasError = true;
                    return;
                }
            }
            catch (Exception)
            {
            }
            gameChar.CurrentDungeonId = data.Template.Id;
            gameChar.CombatStartUtc = DateTime.UtcNow;
            data.DebugMessage = null;
            data.HasError = false;
            return;
        }

        /// <summary>
        /// 终止/结算退关副本。
        /// </summary>
        /// <param name="data">终止请求的数据封装对象。</param>
        /// <returns>true正常结束，false发生错误。</returns>
        public void EndCombat(EndCombatData data)
        {
            //Task.Run(() => throw new ArgumentNullException());
            //data.DebugMessage = "调测固定错误。";
            //data.HasError = true;
            //return;
            var gcm = World.CharManager;
            if (!gcm.Lock(data.GameChar.GameUser))
            {
                data.DebugMessage = "用户已经无效";
                data.HasError = true;
                return;
            }
            try
            {
                var gameChar = data.GameChar;

                //校验战斗状态
                if (!data.GameChar.CurrentDungeonId.HasValue)    //若不在战斗状态
                {
                    data.HasError = true;
                    data.DebugMessage = "不在战斗状态";
                    return;
                }
                else if (data.GameChar.CurrentDungeonId != data.Template.Id)    //若当前在战斗中且不是指定的战斗场景
                {
                    data.DebugMessage = "角色在另外一个场景中战斗。";
                    data.HasError = true;
                    return;
                }
                //规范化数据
                var gim = World.ItemManager;
                gim.Normalize(data.GameItems);
                data.GameItems.ForEach(c => gim.MergeProperty(c));
                if (!VerifyTime(data))  //若时间过短
                    return;

                //核准本次收益
                var succ = Verify(data.Template, data.GameItems, data.GameChar, out var errMsg);
                if (!succ)   //若本次收益不合法
                {
                    data.HasError = true;
                    data.DebugMessage = errMsg;
                    return;
                }
                //核准总收益
                var shouyiSlot = gameChar.GameItems.First(c => c.ExtraGuid == ProjectConstant.ShouyiSlotId);   //收益槽
                var totalItems = shouyiSlot.Children.Concat(data.GameItems);    //总计收益
                succ = Verify(GetParent(data.Template), totalItems, data.GameChar, out errMsg);
                if (!succ)   //若总收益不合法
                {
                    data.HasError = true;
                    data.DebugMessage = errMsg;
                    return;
                }
                //记录收益——改写收益槽数据
                List<GameItem> lst = new List<GameItem>();

                World.ItemManager.MoveItems(data.GameItems.Select(c =>
                {
                    if (gim.IsMounts(c))
                    {

                        var mounts = new GameItem();
                        World.EventsManager.GameItemCreated(mounts, c.ExtraGuid, shouyiSlot, null,
                            new Dictionary<string, object>() { { "htid", gim.GetHeadTemplate(c).IdString }, { "btid", gim.GetBodyTemplate(c).IdString } });
                        mounts.SetSdp("neatk", c.GetSdpDecimalOrDefault("neatk", 0));
                        mounts.SetSdp("nemhp", c.GetSdpDecimalOrDefault("nemhp", 0));
                        mounts.SetSdp("neqlt", c.GetSdpDecimalOrDefault("neqlt", 0));
                        return mounts;
                    }
                    else
                        return c;
                }), shouyiSlot, lst);
                Trace.WriteLineIf(lst.Count > 0, "大事不好东西没放进去。");   //目前是不可能地

                //判断大关卡是否要结束
                if (data.OnlyMark)   //若仅记录和校验收益
                {
                    World.CharManager.NotifyChange(data.GameChar.GameUser);
                    return;
                }
                //下一关数据
                data.NextTemplate = GetNext(data.Template);
                if (null == data.NextTemplate || data.EndRequested) //若大关卡已经结束
                {
                    if (GetParent(data.Template).Id == ProjectConstant.PvpTId)    //若是pvp
                    {

                    }
                    var changes = new List<GamePropertyChangeItem<object>>();
                    //移动收益槽数据到各自背包。
                    //金币
                    var gis = shouyiSlot.Children.Where(c => c.ExtraGuid == ProjectConstant.JinbiId).ToArray();
                    if (gis.Any())
                        gim.MoveItems(gis, gameChar.GetCurrencyBag(), null, changes);
                    //木材
                    gis = shouyiSlot.Children.Where(c => c.ExtraGuid == ProjectConstant.MucaiId).ToArray();
                    if (gis.Any())
                        gim.MoveItems(gis, gameChar.GetCurrencyBag(), null, changes);
                    //野生怪物
                    gis = shouyiSlot.Children.Where(c => c.ExtraGuid == ProjectConstant.ZuojiZuheRongqi).ToArray();
                    var shoulan = gameChar.GameItems.First(c => c.ExtraGuid == ProjectConstant.ShoulanSlotId);
                    if (gis.Any())
                        gim.MoveItems(gis, shoulan, null, changes);
                    //其他道具
                    var daojuBag = gameChar.GameItems.First(c => c.ExtraGuid == ProjectConstant.DaojuBagSlotId);   //道具背包
                    gis = shouyiSlot.Children.Where(c => c.ExtraGuid != ProjectConstant.JinbiId && c.ExtraGuid != ProjectConstant.MucaiId && c.ExtraGuid != ProjectConstant.ZuojiZuheRongqi).ToArray();
                    gim.MoveItems(gis, daojuBag, null, changes);
                    changes.CopyTo(data.ChangesItems);

                    //将剩余未能获取的收益放置于弃物槽中
                    var qiwu = gameChar.GetQiwuBag();
                    foreach (var item in shouyiSlot.Children.ToArray())
                    {
                        data.ChangesItems.AddToRemoves(shouyiSlot.Id, item.Id);
                        gim.ForcedMove(item, item.Count.Value, qiwu);
                        data.ChangesItems.AddToAdds(item);
                    }
                    //设置成就数据
                    if (data.IsWin && data.Template.GetSdpDecimalOrDefault("typ") == 1) //若是推关
                    {
                        var mission = data.GameChar.GetRenwuSlot().Children.FirstOrDefault(c => c.ExtraGuid == ProjectMissionConstant.关卡成就);
                        if (null != mission)   //若找到成就对象
                        {
                            var oldVal = mission.GetSdpDecimalOrDefault(ProjectMissionConstant.指标增量属性名);
                            mission.SetSdp(ProjectMissionConstant.指标增量属性名, oldVal + 1m); //设置该成就的指标值的增量，原则上都是正值
                            World.MissionManager.ScanAsync(data.GameChar);
                        }
                    }
                    else if (data.IsWin && data.Template.GetSdpDecimalOrDefault("typ") == 2)   //若是塔防
                    {
                        var mission = data.GameChar.GetRenwuSlot().Children.FirstOrDefault(c => c.ExtraGuid == ProjectMissionConstant.累计塔防模式次数成就);
                        if (null != mission)   //若找到成就对象
                        {
                            var oldVal = mission.GetSdpDecimalOrDefault(ProjectMissionConstant.指标增量属性名);
                            mission.SetSdp(ProjectMissionConstant.指标增量属性名, oldVal + 1m); //设置该成就的指标值的增量，原则上都是正值
                            World.MissionManager.ScanAsync(data.GameChar);
                        }
                    }
                }
                if (data.EndRequested)
                    data.NextTemplate = null;
                data.GameChar.CurrentDungeonId = data.NextTemplate?.Id;
                data.GameChar.CombatStartUtc = data.GameChar.CurrentDungeonId is null ? default(DateTime?) : DateTime.UtcNow;
                World.CharManager.NotifyChange(data.GameChar.GameUser);
            }
            finally
            {
                gcm.Unlock(data.GameChar.GameUser);
            }
            return;
        }

        #region PVP相关

        /// <summary>
        /// 获取指定用户的pvp排名。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public int GetPvpRank(GameChar gameChar)
        {
            var context = gameChar.GameUser.DbContext;
            var pvp = gameChar.GetPvpObject();
            //var coll = from tmp in context.Set<GameExtendProperty>()
            //           where tmp.Name == PvpRankName && (tmp.DecimalValue > pvp.Count || tmp.DecimalValue == pvp.Count && string.Compare(tmp.StringValue, gameChar.DisplayName) < 0)
            //           select tmp;
            var coll1 = from gi in context.Set<GameItem>()
                        join gc in context.Set<GameChar>()
                        on gi.Parent.OwnerId equals gc.Id
                        where gi.ExtraGuid == ProjectConstant.PvpObjectTId && (gi.ExtraDecimal > pvp.ExtraDecimal ||
                            gi.ExtraDecimal == pvp.ExtraDecimal && string.Compare(gameChar.DisplayName, gc.DisplayName) > 0)
                        orderby gi.ExtraDecimal, gc.DisplayName
                        select gi;
            var result = coll1.Count();
            return result;
        }

        /// <summary>
        /// 更新推关战力信息。
        /// </summary>
        /// <param name="gameChar"></param>
        public void UpdatePveInfo(GameChar gameChar)
        {
            var slot = gameChar.GetTuiguanObject();
            slot.ExtraDecimal = World.CombatManager.GetTotalAbility(gameChar);
        }

        /// <summary>
        /// 将体力消耗转化为经验增加。
        /// </summary>
        /// <param name="changes"></param>
        /// <returns></returns>
        bool Tili2Exp(GameChar gameChar, ICollection<GamePropertyChangeItem<object>> changes)
        {
            var tiliIncr = changes.FirstOrDefault(c =>
            {
                return c.Object is GameItem gi && gi.ExtraGuid == ProjectConstant.TiliId;
            });
            //体力变化
            if (tiliIncr != null)
            {
                if (!tiliIncr.HasOldValue || !OwConvert.TryToDecimal(tiliIncr.OldValue, out var ov))
                    ov = 0;
                if (!tiliIncr.HasNewValue || !OwConvert.TryToDecimal(tiliIncr.NewValue, out var nv))
                    nv = 0;
                if (ov - nv > 0)
                {
                    World.CharManager.AddExp(gameChar, ov - nv, changes);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 标记开始一场新的pvp战斗。
        /// </summary>
        /// <param name="datas"></param>
        public void StartCombatPvp(StartCombatPvpData datas)
        {
            var cache = World.GameCache;
            datas.GameChar.GetTili().RefreshFcp();
            if (datas.DungeonId == ProjectConstant.PvpDungeonTId) //若是正常pvp
            {
                var key = datas.CombatId.ToString();
                using var dwKey = cache.Lock(key);
                if (dwKey.IsEmpty)  //若锁定超时
                {
                    datas.FillErrorFromWorld();
                    return;
                }
                var thing = cache.GetOrLoad<VirtualThing>(key, c => c.Id == datas.CombatId);
                if (thing is null)
                {
                    datas.FillErrorFromWorld();
                    return;
                }
                cache.GetCacheEntry(key).SetSlidingExpiration(TimeSpan.FromMinutes(15));

                var combat = thing.GetJsonObject<GameCombat>();
                var OtherCharId = combat.Defensers.FirstOrDefault()?.CharId ?? combat.Others.First().CharId;
                using var dwUsers = combat.LockAll(World);
                if (dwUsers is null)
                {
                    datas.FillErrorFromWorld();
                    return;
                }
                var OtherChar = World.CharManager.GetCharFromId(OtherCharId);

                var tt = World.ItemTemplateManager.GetTemplateFromeId(datas.DungeonId); //关卡模板
                if (!World.ItemManager.DecrementCount(datas.GameChar, tt.Properties, "cost", datas.PropertyChanges))
                {
                    datas.FillErrorFromWorld();
                    return;
                }
                Tili2Exp(datas.GameChar, datas.PropertyChanges);    //体力消耗转换为经验

                //设置信息
                combat.StartUtc = DateTime.UtcNow;

                var attcker = combat.CreateSoldier();
                combat.SetAttacker(datas.GameChar, attcker, World);

                var defener = combat.CreateSoldier();
                combat.SetDefener(OtherChar, defener, World);
            }
            else if (datas.DungeonId == ProjectConstant.PvpForHelpDungeonTId) //若是协助pvp
            {
                var tt = World.ItemTemplateManager.GetTemplateFromeId(datas.DungeonId); //关卡模板
                if (!World.ItemManager.DecrementCount(datas.GameChar, tt.Properties, "cost", datas.PropertyChanges))
                {
                    datas.FillErrorFromWorld();
                    return;
                }
                Tili2Exp(datas.GameChar, datas.PropertyChanges);    //体力消耗转换为经验
                //获取旧战斗
                var oldKey = datas.OldCombatId.Value.ToString();
                using var dwOld = GetAndLockCombat(datas.OldCombatId.Value, out var oldCombat);
                if (dwOld.IsEmpty)
                {
                    datas.FillErrorFromWorld();
                    return;
                }
                //if (oldCombat.Defensers.All(c => c.CharId != datas.GameChar.Id))
                //{
                //    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                //    datas.DebugMessage = "指定角色不可协助。";
                //    return;
                //}
                if (oldCombat.Assistanced)    //若已协助
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.DebugMessage = "该战斗已经协助过了。";
                    return;
                }
                var combat = GameCombat.CreateNew(World);
                //设置超时
                var key = combat.Thing.IdString;
                var entry = cache.GetCacheEntry(key);
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(15));

                //设置信息
                combat.StartUtc = DateTime.UtcNow;
                combat.OldCombatId = oldCombat.Id;
                combat.MapTId = datas.DungeonId;

                var attcker = combat.CreateSoldier();
                combat.SetAttacker(datas.GameChar, attcker, World);

                var defener = combat.CreateSoldier();
                var otherId = oldCombat.Attackers.First().CharId;
                var OtherChar = World.CharManager.GetCharFromId(otherId);
                combat.SetDefener(OtherChar, defener, World);
                datas.CombatId = combat.Id;
            }
            else if (datas.DungeonId == ProjectConstant.PvpForRetaliationDungeonTId)   //若是反击pvp
            {
                var tt = World.ItemTemplateManager.GetTemplateFromeId(datas.DungeonId); //关卡模板
                if (!World.ItemManager.DecrementCount(datas.GameChar, tt.Properties, "cost", datas.PropertyChanges))
                {
                    datas.FillErrorFromWorld();
                    return;
                }
                Tili2Exp(datas.GameChar, datas.PropertyChanges);    //体力消耗转换为经验
                //获取旧战斗
                var oldKey = datas.OldCombatId.Value.ToString();
                using var dwOld = GetAndLockCombat(datas.OldCombatId.Value, out var oldCombat);
                if (dwOld.IsEmpty)
                {
                    datas.FillErrorFromWorld();
                    return;
                }
                if (oldCombat.Defensers.All(c => c.CharId != datas.GameChar.Id))
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.DebugMessage = "指定角色没有反击权限。";
                    return;
                }
                if (oldCombat.Retaliationed)    //若已反击
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.DebugMessage = "指定角色已经反击。";
                    return;
                }
                var combat = GameCombat.CreateNew(World);
                //设置超时
                var key = combat.Thing.IdString;
                var entry = cache.GetCacheEntry(key);
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(15));

                //设置信息
                combat.StartUtc = DateTime.UtcNow;
                combat.OldCombatId = oldCombat.Id;

                var attcker = combat.CreateSoldier();
                combat.SetAttacker(datas.GameChar, attcker, World);

                var defener = combat.CreateSoldier();
                var otherId = oldCombat.Attackers.First().CharId;
                var OtherChar = World.CharManager.GetCharFromId(otherId);
                combat.SetDefener(OtherChar, defener, World);

                combat.MapTId = ProjectConstant.PvpForRetaliationDungeonTId;
                datas.CombatId = combat.Id;
            }
            else
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = $"未知关卡模板Id={datas.DungeonId}";
                return;
            }

            //GameCombat combat = GameCombat.CreateNew(World);
            //combat.MapTId = datas.DungeonId;
            //datas.CombatId = combat.Id;
            //World.GameCache.SetDirty(combat.Thing.IdString, true);
        }

        /// <summary>
        /// pvp战斗结算。
        /// </summary>
        /// <param name="datats"></param>
        public void EndCombatPvp(EndCombatPvpWorkData datas)
        {
            using var dwKey = GetAndLockCombat(datas.CombatId, out var combat); //获取到当前战斗对象
            if (dwKey.IsEmpty)
            {
                datas.FillErrorFromWorld();
                return;
            }


            var mapTt = World.ItemTemplateManager.GetTemplateFromeId(combat.MapTId) ?? World.ItemTemplateManager.GetTemplateFromeId(combat.MapTId);
            var pTId = GetParent(mapTt).Id; //大关卡模板Id

            //校验免战
            if (false)   //若免战
            {

            }
            using var dwUser = combat.LockAll(World);
            if (dwUser is null)
                return;
            var pvp1 = datas.GameChar.GetPvpObject();
            GameItem pvp2;
            using (var dw = combat.GetOtherChar(World, out var OtherChar))
                if (dw is null)
                {
                    datas.FillErrorFromWorld();
                    return;
                }
                else
                    pvp2 = OtherChar.GetPvpObject();

            var oldScopeAtt = pvp1.ExtraDecimal;  //进攻者积分
            var oldScopeDef = pvp2.ExtraDecimal; //防御者积分
            //分不同情况调用
            if (pTId == ProjectConstant.PvpDungeonTId) //若是正常pvp
            {
                Pvp(datas);
            }
            else if (pTId == ProjectConstant.PvpForHelpDungeonTId) //若是协助pvp
            {
                PvpForHelp(datas);
            }
            else if (pTId == ProjectConstant.PvpForRetaliationDungeonTId)   //若是反击pvp
            {
                PvpForRetaliation(datas);
            }
            else
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = $"未知关卡模板Id={mapTt}";
                return;
            }
            if (!datas.HasError) //若成功
            {
                var gim = World.ItemManager;
                //攻击方信息
                //var Me = gim.GetLineup(datas.GameChar, 2);
                //datas.Combat.SetAttackerMounts(Me);
                //datas.Combat.AttackerDisplayName = datas.GameChar.DisplayName;
                //防御方信息
                //var other = gim.GetLineup(datas.OtherChar, 10000);
                //datas.Combat.SetDefenserMounts(other);
                //datas.Combat.DefenserDisplayName = datas.OtherChar.DisplayName;
                datas.Save();
            }
        }

        /// <summary>
        /// 主动pvp。
        /// </summary>
        /// <param name="datats"></param>
        public void Pvp(EndCombatPvpWorkData datas)
        {
            using var dwKey = GetAndLockCombat(datas.CombatId, out var combat); //获取到当前战斗对象
            if (dwKey.IsEmpty)
            {
                datas.FillErrorFromWorld();
                return;
            }
            using var dwUsers = combat.LockAll(World);
            if (dwUsers is null) //若无法锁定对象。
            {
                datas.HasError = true;
                return;
            }
            if (combat.Attackers.All(c => c.CharId != datas.GameChar.Id)/* || combat.Defensers.All(c => c.CharId != datas.OtherCharId)*/)
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = $"指定的战斗id不正确。";
                return;
            }
            if (combat.EndUtc.HasValue)
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = $"指定的战斗id不正确,已经结算过该场战斗。";
                return;
            }
            combat.EndUtc = DateTime.UtcNow;    //记录结算时间

            //设置超时
            var cache = World.GameCache;
            var key = combat.Thing.IdString;
            var entry = cache.GetCacheEntry(key);
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(15));


            var otherChar = World.CharManager.GetCharFromId(combat.Defensers.FirstOrDefault()?.CharId ?? combat.Others.First().CharId);

            var attacker = combat.Attackers.First();    //攻击方
            var defenser = combat.Defensers.First();    //防御方

            //设置战利品
            List<GameItem> bootyOfAttacker = ComputeAttackerBooty();
            var gim = World.ItemManager;
            gim.Normalize(datas.Booty);
            datas.Booty.ForEach(c => gim.MergeProperty(c));
            attacker.Booties.AddRange(datas.Booty.Select(c => gim.Clone(c)));

            gim.MoveItems(datas.Booty, datas.GameChar, null, datas.PropertyChanges);

            List<GameItem> bootyOfDefenser = ComputeDefenerBooty();
            //设置物品实际增减
            List<GameItem> rem = new List<GameItem>();  //无法放入物品
            attacker.Booties.AddRange(bootyOfAttacker); //计入战报
            World.ItemManager.MoveItems(bootyOfAttacker, datas.GameChar, null, datas.PropertyChanges);

            if (!World.CharManager.IsOnline(otherChar.Id))  //若不在线
            {
                defenser.Booties.AddRange(bootyOfDefenser); //计入战报
                World.ItemManager.MoveItems(bootyOfDefenser, otherChar, null, datas.PropertyChanges);    //防御方物品变动奖励
            }

            ComputeScore();
            #region 计算收益

            /// <summary>
            /// 计算进攻方收益。
            /// </summary>
            List<GameItem> ComputeAttackerBooty()
            {
                List<GameItem> result = new List<GameItem>();

                var xGold = (1 - datas.StoreOfWoodRhp) * 0.5m + (1 - datas.GoldRhp) * 0.5m;   //金币的系数
                var xWood = (1 - datas.StoreOfWoodRhp) * 0.5m + (1 - datas.WoodRhp) * 0.5m;    //木材的系数
                var resource = combat.Others.First(c => c.CharId == defenser.CharId);   //搜索时点的防御方资源快照

                var gold = resource.Resource.FirstOrDefault(c => c.ExtraGuid == ProjectConstant.YumitianTId)?.Count ?? 0;   //金币基数
                var wood = resource.Resource.FirstOrDefault(c => c.ExtraGuid == ProjectConstant.MucaiId)?.Count ?? 0;  //木材基数
                var shulin = resource.Resource.FirstOrDefault(c => c.ExtraGuid == ProjectConstant.MucaishuTId)?.Count ?? 0;  //树林基数

                var goldGi = new GameItem();    //金币
                World.EventsManager.GameItemCreated(goldGi, ProjectConstant.JinbiId);
                goldGi.Count = Math.Truncate((100 + gold * 0.5m) * xGold);

                var woodGi = new GameItem();    //木材
                World.EventsManager.GameItemCreated(woodGi, ProjectConstant.MucaiId);
                woodGi.Count = Math.Truncate((10 + wood * 0.2m + shulin * 0.5m) * xWood);

                result.Add(goldGi);
                result.Add(woodGi);
                return result;
            }

            /// <summary>
            /// 计算防御方收益。
            /// </summary>
            List<GameItem> ComputeDefenerBooty()
            {
                List<GameItem> result = new List<GameItem>();
                if (otherChar.CharType.HasFlag(CharType.Npc))   //若是npc被攻击则不损失资源
                    return result;
                if (datas.MainRoomRhp >= 1)    //若没有击溃主控室
                    return result;

                var xGold = (1 - datas.MainRoomRhp);   //金币的系数
                var xWood = (1 - datas.MainRoomRhp);    //木材的系数

                var gold = otherChar.GetHomeland().GetAllChildren().FirstOrDefault(c => c.ExtraGuid == ProjectConstant.YumitianTId)?.Count ?? 0;   //金币基数
                var wood = otherChar.GetCurrencyBag().Children.FirstOrDefault(c => c.ExtraGuid == ProjectConstant.MucaiId)?.Count ?? 0;  //木材基数
                var shulin = otherChar.GetHomeland().GetAllChildren().FirstOrDefault(c => c.ExtraGuid == ProjectConstant.MucaishuTId)?.Count ?? 0;  //树林基数

                var goldGi = new GameItem();    //金币
                World.EventsManager.GameItemCreated(goldGi, ProjectConstant.YumitianTId);
                goldGi.Count = Math.Truncate(-gold * 0.5m * xGold);

                var woodGi = new GameItem();    //木材
                World.EventsManager.GameItemCreated(woodGi, ProjectConstant.MucaiId);
                woodGi.Count = Math.Truncate(-wood * 0.2m * xWood);

                var woodShu = new GameItem();   //木材树
                World.EventsManager.GameItemCreated(woodShu, ProjectConstant.MucaishuTId);
                woodShu.Count = Math.Truncate(-shulin * 0.5m * xWood);

                result.Add(goldGi);
                result.Add(woodGi);
                result.Add(woodShu);
                return result;
            }

            /// <summary>
            /// 计算防分数变化。
            /// </summary>
            void ComputeScore()
            {
                var attacker = combat.Attackers.First();
                GameCombat.FillSoldierBefroeCombat(datas.GameChar, attacker, World);
                var defener = combat.Defensers.First();
                GameCombat.FillSoldierBefroeCombat(otherChar, defener, World);

                var pvpAttacker = datas.GameChar.GetPvpObject();
                var pvpDefenser = otherChar.GetPvpObject();
                if (datas.MainRoomRhp <= 0)    //若进攻方胜利
                {
                    var inc = Math.Clamp(1 + Math.Round((decimal)Math.Max(defener.ScoreBefore - attacker.ScoreBefore, 0) / 10, MidpointRounding.ToPositiveInfinity), 0, 6); //分值增量
                    attacker.ScoreAfter = (int)(pvpAttacker.ExtraDecimal + inc);
                    datas.PropertyChanges.ModifyAndAddChanged(pvpAttacker, nameof(pvpAttacker.ExtraDecimal), pvpAttacker.ExtraDecimal + inc);
                    if (!World.CharManager.IsOnline(otherChar.Id))  //若不在线
                    {
                        defener.ScoreAfter = (int)(pvpDefenser.ExtraDecimal - inc);
                        datas.PropertyChanges.ModifyAndAddChanged(pvpDefenser, nameof(pvpDefenser.ExtraDecimal), pvpDefenser.ExtraDecimal - inc);
                    }
                }
                else //若进攻方失败
                {
                    var defenerInc = Math.Clamp(1 + Math.Round((decimal)Math.Max(defener.ScoreBefore - attacker.ScoreBefore, 0) / 10, MidpointRounding.ToPositiveInfinity), 0, 6); //分值增量
                    decimal attackerInc;
                    if (attacker.ScoreBefore < 400)
                    {
                        defenerInc = -Math.Ceiling(defenerInc * 0.5m);
                    }
                    else if (attacker.ScoreBefore >= 400 && attacker.ScoreBefore <= 600)
                    {
                        defenerInc = -Math.Ceiling(defenerInc * 0.8m);
                    }
                    else
                    {
                        defenerInc = -Math.Ceiling(defenerInc);
                    }
                    var desCount = datas.DestroyCountOfWoodStore;
                    attackerInc = Math.Min(defenerInc + desCount, 0);
                    datas.PropertyChanges.ModifyAndAddChanged(pvpAttacker, nameof(pvpAttacker.ExtraDecimal), pvpAttacker.ExtraDecimal + attackerInc);
                    if (!World.CharManager.IsOnline(otherChar.Id))  //若不在线
                        datas.PropertyChanges.ModifyAndAddChanged(pvpDefenser, nameof(pvpDefenser.ExtraDecimal), pvpDefenser.ExtraDecimal - defenerInc);
                }
            }

            #endregion 计算收益

            var pvpObject = datas.GameChar.GetPvpObject();  //PVP对象
            //更改数据
            var db = datas.UserDbContext;
            GameItem pvpObj, otherPvpObj;
            //增加战报
            pvpObj = datas.GameChar.GetPvpObject();
            otherPvpObj = otherChar.GetPvpObject();
            //发送反击邮件
            combat.IsAttckerWin = datas.MainRoomRhp <= 0;
            if (!World.CharManager.IsOnline(otherChar.Id))  //若不在线
            {
                var mail = new GameMail()
                {
                };
                if (combat.IsAttckerWin ?? false)   //若攻击胜利
                {
                    mail.SetSdp("MailTypeId", ProjectConstant.PVP反击邮件.ToString());
                    mail.SetSdp("CombatId", combat.Thing.IdString);
                }
                else //若攻击失败
                {
                    mail.SetSdp("MailTypeId", ProjectConstant.PVP自己_防御_胜利.ToString());
                    mail.SetSdp("CombatId", combat.Thing.IdString);
                }
                if (mail.TryGetSdp("MailTypeId", out _))    //若有标志
                {
                    World.SocialManager.SendMail(mail, new Guid[] { otherChar.Id }, SocialConstant.FromSystemId); //被攻击邮件
                    datas.MailId = mail.Id;
                }
            }
            else //若在线
            {
                combat.IsCompleted = true;
            }
            //保存数据
            datas.Save();
            datas.HasError = false;
            datas.ErrorCode = 0;
            datas.DebugMessage = null;
            //计算成就数据
            if (datas.MainRoomRhp <= 0)    //若进攻胜利
            {
                var mission = datas.GameChar.GetRenwuSlot().Children.FirstOrDefault(c => c.ExtraGuid == ProjectMissionConstant.PVP进攻成就);
                if (null != mission)   //若找到成就对象
                {
                    var oldVal = mission.GetSdpDecimalOrDefault(ProjectMissionConstant.指标增量属性名);
                    mission.SetSdp(ProjectMissionConstant.指标增量属性名, oldVal + 1m); //设置该成就的指标值的增量，原则上都是正值
                    World.MissionManager.ScanAsync(datas.GameChar);
                }
            }
            else if (!World.CharManager.IsOnline(otherChar.Id)) //若防御胜利且计算成就(不在线)
            {
                var mission = otherChar.GetRenwuSlot().Children.FirstOrDefault(c => c.ExtraGuid == ProjectMissionConstant.PVP防御成就);
                if (null != mission)   //若找到成就对象
                {
                    var oldVal = mission.GetSdpDecimalOrDefault(ProjectMissionConstant.指标增量属性名);
                    mission.SetSdp(ProjectMissionConstant.指标增量属性名, oldVal + 1m); //设置该成就的指标值的增量，原则上都是正值
                    World.MissionManager.ScanAsync(otherChar);
                }
            }
            //计算战斗后信息
            GameCombat.FillSoldierAfterCombat(datas.GameChar, attacker, World);
            GameCombat.FillSoldierAfterCombat(otherChar, defenser, World);
            //设置战斗对象
            datas.Combat = combat;
            cache.SetDirty(combat.Thing.IdString);   //尽快保存
        }

        /// <summary>
        /// 反击pvp。
        /// </summary>
        /// <param name="datats"></param>
        private void PvpForRetaliation(EndCombatPvpWorkData datas)
        {
            using var dwCombat = GetAndLockCombat(datas.CombatId, out var combat);
            if (dwCombat.IsEmpty)
            {
                datas.FillErrorFromWorld();
                return;
            }
            using var dwOldCombat = GetAndLockCombat(combat.OldCombatId.Value, out var oldCombat);  //旧战斗对象
            if (dwCombat.IsEmpty)
            {
                datas.FillErrorFromWorld();
                return;
            }

            if (combat.Attackers.All(c => c.CharId != datas.GameChar.Id))    //若没有复仇权
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = "没有复仇权";
                return;
            }
            datas.Combat = combat;
            //计算战利品
            if (datas.MainRoomRhp <= 0) //若反击胜利
            {
                var oldBooty = oldCombat.Defensers.First().Booties; //失去的物品
                var gold = oldBooty.Where(c => c.ExtraGuid == ProjectConstant.JinbiId || c.ExtraGuid == ProjectConstant.YumitianTId).Sum(c => c.Count);   //夺回金币
                var wood = oldBooty.Where(c => c.ExtraGuid == ProjectConstant.MucaiId || c.ExtraGuid == ProjectConstant.MucaishuTId).Sum(c => c.Count);   //夺回木材

                var goldGi = new GameItem();
                World.EventsManager.GameItemCreated(goldGi, ProjectConstant.JinbiId);
                goldGi.Count = Math.Abs(gold ?? 0);

                var woodGi = new GameItem();
                World.EventsManager.GameItemCreated(woodGi, ProjectConstant.MucaiId);
                woodGi.Count = Math.Abs(wood ?? 0);

                List<GameItem> booty = new List<GameItem>();    //战利品
                booty.Add(goldGi);
                booty.Add(woodGi);

                combat.Attackers.First().Booties.AddRange(booty);
                //设置物品变化
                World.ItemManager.MoveItems(booty, datas.GameChar, null, datas.PropertyChanges);

            }
            //发送邮件
            var mail = new GameMail();
            if (datas.MainRoomRhp <= 0) //反击得胜
            {
                oldCombat.IsCompleted = true; //反击得胜后不可再要求协助
                //发送邮件
                mail.SetSdp("MailTypeId", ProjectConstant.PVP反击邮件_自己_胜利.ToString());
                mail.SetSdp("OldCombatId", oldCombat.Thing.IdString);
                mail.SetSdp("CombatId", combat.Thing.IdString);
            }
            else //反击失败
            {
                oldCombat.IsCompleted = oldCombat.Assistanced; //反击得胜后不可再要求协助
                //发送邮件
                mail.SetSdp("MailTypeId", oldCombat.Assistanced ? ProjectConstant.PVP反击_自己_两项全失败.ToString() : ProjectConstant.PVP反击邮件_自己_失败.ToString());
                mail.SetSdp("OldCombatId", oldCombat.Thing.IdString);
                mail.SetSdp("CombatId", combat.Thing.IdString);
            }
            World.SocialManager.SendMail(mail, new Guid[] { datas.GameChar.Id }, SocialConstant.FromSystemId); //被攻击邮件

            var attacker = combat.Attackers.FirstOrDefault();
            var defenser = combat.Defensers.FirstOrDefault();
            //计算战斗后信息
            //GameCombat.FillSoldierAfterCombat(datas.GameChar, attacker, World);
            //GameCombat.FillSoldierAfterCombat(otherChar, defenser, World);
            //保存数据
            oldCombat.Retaliationed = true;
            //datas.Save();
        }

        /// <summary>
        /// 结算协助pvp。
        /// </summary>
        /// <param name="datats"></param>
        private void PvpForHelp(EndCombatPvpWorkData datas)
        {
            using var dwCombat = GetAndLockCombat(datas.CombatId, out var combat);
            if (dwCombat.IsEmpty)
            {
                datas.FillErrorFromWorld();
                return;
            }
            using var dwOldCombat = GetAndLockCombat(combat.OldCombatId.Value, out var oldCombat);  //旧战斗对象
            if (dwCombat.IsEmpty)
            {
                datas.FillErrorFromWorld();
                return;
            }

            if (oldCombat.AssistanceId != datas.GameChar.Id || !oldCombat.Assistancing || oldCombat.IsCompleted) //没有请求当前角色协助或已经结束
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = $"指定的战报对象没有请求此角色协助攻击或已经攻击过了。";
                return;
            }

            var bootyMail = new List<(GameItem, Guid)>();    //战利品
                                                             //计算战利品
            if (datas.MainRoomRhp <= 0) //若协助胜利
            {
                var oldBooty = oldCombat.Defensers.First().Booties; //失去的物品
                var gold = oldBooty.Where(c => c.ExtraGuid == ProjectConstant.JinbiId || c.ExtraGuid == ProjectConstant.YumitianTId).Sum(c => c.Count);   //夺回金币
                var wood = oldBooty.Where(c => c.ExtraGuid == ProjectConstant.MucaiId || c.ExtraGuid == ProjectConstant.MucaishuTId).Sum(c => c.Count);   //夺回木材

                var goldGi = new GameItem();
                World.EventsManager.GameItemCreated(goldGi, ProjectConstant.JinbiId);
                goldGi.Count = Math.Round(Math.Abs(gold ?? 0) * .3m, MidpointRounding.ToNegativeInfinity);

                var woodGi = new GameItem();
                World.EventsManager.GameItemCreated(woodGi, ProjectConstant.MucaiId);
                woodGi.Count = Math.Round(Math.Abs(wood ?? 0) * .3m, MidpointRounding.ToNegativeInfinity);

                List<GameItem> booty = new List<GameItem>();    //战利品
                if (gold != 0) booty.Add(goldGi);
                if (wood != 0) booty.Add(woodGi);

                combat.Attackers.First().Booties.AddRange(booty);
                //设置物品变化
                World.ItemManager.MoveItems(booty, datas.GameChar, null, datas.PropertyChanges);

                //给求助者发还物品
                goldGi = new GameItem();
                World.EventsManager.GameItemCreated(goldGi, ProjectConstant.JinbiId);
                goldGi.Count = Math.Round(Math.Abs(gold ?? 0), MidpointRounding.ToNegativeInfinity);

                woodGi = new GameItem();
                World.EventsManager.GameItemCreated(woodGi, ProjectConstant.MucaiId);
                woodGi.Count = Math.Round(Math.Abs(wood ?? 0), MidpointRounding.ToNegativeInfinity);

                bootyMail.Add((goldGi, ProjectConstant.CurrencyBagTId));
                bootyMail.Add((woodGi, ProjectConstant.CurrencyBagTId));

                var mail2 = new GameMail();
                mail2.SetSdp("MailTypeId", ProjectConstant.PVP反击邮件_求助_胜利_求助者.ToString());
                mail2.SetSdp("OldCombatId", oldCombat.Thing.IdString);
                mail2.SetSdp("CombatId", combat.Thing.IdString);
                if (bootyMail.Count > 0)
                    World.SocialManager.SendMail(mail2, new Guid[] { oldCombat.Defensers.First().CharId }, SocialConstant.FromSystemId, bootyMail); //被攻击邮件
                else
                    World.SocialManager.SendMail(mail2, new Guid[] { oldCombat.Defensers.First().CharId }, SocialConstant.FromSystemId); //被攻击邮件
            }
            //保存数据
            combat.MapTId = ProjectConstant.PvpForHelpDungeonTId;
            combat.OldCombatId = oldCombat.Id;
            datas.Combat = combat;
            //oldCombat.Retaliationed = true;
            oldCombat.Assistancing = false;
            oldCombat.Assistanced = true;
            oldCombat.IsCompleted = oldCombat.Retaliationed || (datas.MainRoomRhp <= 0);

            //datas.Save();

            ////获取战利品
            //if (datas.MainRoomRhp <= 0)    //若赢得战斗
            //{
            //    var oriBooty = db.Set<VirtualThing>().AsNoTracking().Where(c => c.ParentId == oldWar.Id)     //原始战斗攻击方战利品
            //        .AsEnumerable().Select(c => c.GetJsonObject<GameBooty>()).Where(c => oldView.AttackerIds.Contains(c.CharId))
            //        .ToList();  //原战斗的攻击者战利品

            //    //TODO 未知
            //    //var boo = oldView.BootyOfAttacker(datas.UserDbContext);  //原始进攻方的战利品
            //    var boo = oriBooty;  //原始进攻方的战利品

            //    newBooty.RemoveAll(c => c is null); //去掉空引用

            //    newBooty.ForEach(c => c.SetGameItems(World, datas.ChangeItems));

            //    oldBooties.ForEach(c => c.SetGameItems(world));
            //}
            ////保存数据
            ////改写进攻权限
            //datas.Save();
            //datas.ErrorCode = ErrorCodes.NO_ERROR;
            //计算成就数据
            if (datas.MainRoomRhp <= 0)
            {
                var mission = datas.GameChar.GetRenwuSlot().Children.FirstOrDefault(c => c.ExtraGuid == ProjectMissionConstant.PVP助战成就);
                if (null != mission)   //若找到成就对象
                {
                    var oldVal = mission.GetSdpDecimalOrDefault(ProjectMissionConstant.指标增量属性名);
                    mission.SetSdp(ProjectMissionConstant.指标增量属性名, oldVal + 1m); //设置该成就的指标值的增量，原则上都是正值
                    World.MissionManager.ScanAsync(datas.GameChar);
                }
            }
            //发送邮件
            var mail = new GameMail()
            {
            };
            if (datas.MainRoomRhp <= 0) //若协助成功
            {
                //mail.SetSdp("MailTypeId", ProjectConstant.PVP反击邮件_求助_胜利_求助者.ToString());
                //mail.SetSdp("OldCombatId", oldCombat.Thing.IdString);
                //mail.SetSdp("CombatId", combat.Thing.IdString);
                //if (bootyMail.Count > 0)
                //    World.SocialManager.SendMail(mail, new Guid[] { oldCombat.Defensers.First().CharId }, SocialConstant.FromSystemId, bootyMail); //被攻击邮件
                //else
                //    World.SocialManager.SendMail(mail, new Guid[] { oldCombat.Defensers.First().CharId }, SocialConstant.FromSystemId); //被攻击邮件
            }
            else
            {
                mail.SetSdp("MailTypeId", oldCombat.Retaliationed ? ProjectConstant.PVP反击_自己_两项全失败.ToString() : ProjectConstant.PVP反击邮件_求助_失败_求助者.ToString());
                mail.SetSdp("OldCombatId", oldCombat.Thing.IdString);
                mail.SetSdp("CombatId", combat.Thing.IdString);
                World.SocialManager.SendMail(mail, new Guid[] { oldCombat.Defensers.First().CharId }, SocialConstant.FromSystemId); //被攻击邮件
            }

        }

        /// <summary>
        /// 放弃协助或放弃自己被打的战斗。
        /// </summary>
        /// <param name="datas"></param>
        public void AbortPvp(AbortPvpDatas datas)
        {
            using var dwCombat = GetAndLockCombat(datas.CombatId, out var oldCombat);
            if (dwCombat.IsEmpty)
            {
                datas.FillErrorFromWorld();
                return;
            }
            if (oldCombat.Defensers.Any(c => c.CharId == datas.GameChar.Id))  //自己被打直接放弃
            {
                var getMails = new GameSocialManager.GetMailsDatas(Service, datas.GameChar);
                World.SocialManager.GetMails(getMails);
                if (getMails.HasError)
                {
                    datas.HasError = true;
                    datas.ErrorCode = getMails.ErrorCode;
                    datas.DebugMessage = getMails.DebugMessage;
                    return;
                }
                var mail = getMails.Mails.FirstOrDefault(c => c.GetSdpGuidOrDefault("OldCombatId") == datas.CombatId && c.GetSdpStringOrDefault("MailTypeId") == ProjectConstant.PVP系统奖励.ToString());
                if (null != mail)
                    datas.UserDbContext.Remove(mail);
                oldCombat.Assistancing = false;
                oldCombat.Assistanced = true;
                oldCombat.IsCompleted = true;
            }
            else //若被邀请协助
            {
                oldCombat.Assistancing = false;
                oldCombat.Assistanced = true;
                oldCombat.IsCompleted = oldCombat.Retaliationed;
                var mail = new GameMail();
                mail.SetSdp("MailTypeId", oldCombat.IsCompleted ? ProjectConstant.PVP反击_自己_两项全失败.ToString() : ProjectConstant.PVP反击邮件_求助_失败_求助者.ToString());
                mail.SetSdp("OldCombatId", oldCombat.Thing.IdString);
                //mail.SetSdp("CombatId", oldCombat.Thing.IdString);
                World.SocialManager.SendMail(mail, new Guid[] { oldCombat.Defensers.First().CharId }, SocialConstant.FromSystemId);
                datas.FillErrorFromWorld();
            }
        }

        #endregion PVP相关

        /// <summary>
        /// 校验时间。
        /// </summary>
        /// <returns></returns>
        private bool VerifyTime(EndCombatData data)
        {
            var gameChar = data.GameChar;
            var tm = World.ItemTemplateManager.GetTemplateFromeId(gameChar.CurrentDungeonId.Value);    //关卡模板
            //校验时间
            DateTime dt = gameChar.CombatStartUtc.GetValueOrDefault(DateTime.UtcNow);
            var dtNow = DateTime.UtcNow;
            _ = TimeSpan.FromSeconds(Convert.ToDouble(tm.GetSdpDecimalOrDefault("tl", decimal.Zero)));   //最短时间
            TimeSpan lt = TimeSpan.FromSeconds(1);
            if (dtNow - dt < lt) //若时间过短
            {
                data.HasError = true;
                data.DebugMessage = "时间过短";
                return false;
            }
            return true;
        }

        /// <summary>
        /// 按指定关卡数据校验集合中物品是否合规。
        /// </summary>
        /// <param name="dungeon"></param>
        /// <param name="gameItems"></param>
        /// <param name="errorString"></param>
        /// <returns></returns>
        public bool Verify(GameItemTemplate dungeon, IEnumerable<GameItem> gameItems, GameChar gameChar, out string errorString)
        {
            errorString = string.Empty;
            //typ关卡类别=1普通管卡 mis大关数 sec=小关，gold=数金币掉落上限，aml=获得资质野怪的数量，mne=资质和上限，mt=神纹数量上限，wood=木头掉落上限，
            //tl = 通关最短时限，idt = 道具掉落上限,tdt=pve减少pveT的次数，minCE=最低战力要求，
            if (dungeon.TryGetSdpDecimal("gold", out var gold)) //若需要限定金币
            {
                var tmp = gameItems.Where(c => c.ExtraGuid == ProjectConstant.JinbiId).Sum(c => c.Count.Value);
                if (tmp > gold) //若超限
                {
                    errorString = $"金币最多允许掉落{gold}，实际掉落{tmp}。";
                    return false;
                }
            }
            if (dungeon.TryGetSdpDecimal("wood", out var wood)) //若需要限定木材
            {
                var tmp = gameItems.Where(c => c.ExtraGuid == ProjectConstant.MucaiId).Sum(c => c.Count.Value);
                if (tmp > wood) //若超限
                {
                    errorString = $"木材最多允许掉落{wood}，实际掉落{tmp}。";
                    return false;
                }
            }
            var gim = World.ItemManager;
            var aryMounts = gameItems.Where(c => gim.IsMounts(c)).ToArray();
            if (dungeon.TryGetSdpDecimal("aml", out var aml)) //若需要限定资质野怪的数量
            {
                var tmp = aryMounts.Length;
                if (tmp > aml) //若超限
                {
                    errorString = $"资质野怪最多允许掉落{aml}，实际掉落{tmp}。";
                    return false;
                }
                var mounts = gameChar.GameItems.FirstOrDefault(c => c.ExtraGuid == ProjectConstant.ZuojiBagSlotId);    //坐骑包
                var lookup = mounts.Children.ToLookup(c => gim.GetMountsTIds(c));
                //var coll = aryMounts.Where(c => !lookup.Contains(gim.GetMountsTIds(c)));   //错误的资质怪类型
                //if (coll.Any())
                //{
                //    errorString = $"至少有一个资质怪的类型尚不存在对应坐骑。";
                //    return false;
                //}
            }
            if (dungeon.TryGetSdpDecimal("mne", out var mne)) //若需要限定资质野怪的最高资质
            {
                var tmp = aryMounts.FirstOrDefault(c => GetTotalNe(c.Properties) > mne);
                if (null != tmp)   //若资质超限
                {
                    errorString = $"资质野怪资质总和只允许{mne},但至少有一个资质怪自制总和是{GetTotalNe(tmp.Properties)}。";
                    return false;
                }
            }
            var gimt = World.ItemTemplateManager;
            //var aryShenwen = gameItems.Where(c => gimt.GetTemplateFromeId(c.ExtraGuid).GenusCode >= 15 && gimt.GetTemplateFromeId(c.ExtraGuid).GenusCode <= 17).ToArray();
            //if (dungeon.TryGetSdpDecimal("mt", out var mt)) //若需要限定神纹数量上限
            //{
            //    var tmp = aryShenwen.Sum(c => c.Count ?? 1);
            //    if (tmp > mt)   //若神纹道具数量超限
            //    {
            //        errorString = $"神纹道具只允许{mt}个,但掉落了{tmp}个";
            //        return false;
            //    }
            //}
            var items = gameItems.Where(c => c.ExtraGuid != ProjectConstant.JinbiId && c.ExtraGuid != ProjectConstant.MucaiId && c.ExtraGuid != ProjectConstant.ZuojiZuheRongqi)/*.Except(aryShenwen)*/;  //道具
            if (dungeon.TryGetSdpDecimal("idt", out var idt)) //若需要限定其他道具数量上限
            {
                var tmp = items.Sum(c => c.Count ?? 1);
                if (tmp > idt)
                {
                    errorString = $"非神纹道具只允许{idt}个,但掉落了{tmp}个";
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="limits"></param>
        /// <param name="items"></param>
        /// <param name="errItem"></param>
        /// <returns></returns>
        public bool Verify<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> limits, IEnumerable<(TKey, TValue)> items, out (TKey, TValue) errItem) where TValue : IComparable<TValue>
        {
            foreach (var item in items) //遍历每一项
            {
                if (!limits.TryGetValue(item.Item1, out var count)) //若没有找到限定项
                {
                    errItem = item;
                    return false;
                }
                else if (count.CompareTo(item.Item2) < 0)  //若数量超限
                {
                    errItem = item;
                    return false;
                }
            }
            errItem = (default, default);
            return true;
        }

        /// <summary>
        /// 获取总资质，没找到的视同0。
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private decimal GetTotalNe(IReadOnlyDictionary<string, object> dic)
        {
            var neatk = Convert.ToDecimal(dic.GetValueOrDefault("neatk", 0));
            var nemhp = Convert.ToDecimal(dic.GetValueOrDefault("nemhp", 0));
            var neqlt = Convert.ToDecimal(dic.GetValueOrDefault("neqlt", 0));
            return neatk + nemhp + neqlt;
        }

        /// <summary>
        /// 突破次数,对应主动技能等级：1，2，3， 4， 5。
        /// </summary>
        private readonly int[] _aryTupo = new int[] { 0, 6, 12, 18, 24 };

        /// <summary>
        /// 坐骑等级，对应被动技能等级：1，2，3， 4， 5
        /// </summary>
        private readonly int[] _aryLvZuoqi = new int[] { 0, 4, 9, 14, 19 };

        /// <summary>
        /// 更新指定坐骑的战斗力属性。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="dic"></param>
        /// <returns>true成功计算了属性，false没有计算得到属性。</returns>
        public bool UpdateAbility(GameItem gameItem, IDictionary<string, double> dic)
        {
            var gim = World.ItemManager;
            var body = gim.GetBody(gameItem);   //身体对象
            if (body is null)
                return false;
            var bodyGid = (int)body.GetTemplate().GId; //身体Id
            //计算本体属性
            var head = gim.GetHead(gameItem);
            if (null == head)
                return false;
            double atk = 0, qlt = 0, mhp = 0;
            atk += (double)head.GetDecimalWithFcpOrDefault("atk", decimal.Zero);
            atk += (double)body.GetDecimalWithFcpOrDefault("atk", decimal.Zero);

            mhp += (double)head.GetDecimalWithFcpOrDefault("mhp", decimal.Zero);
            mhp += (double)body.GetDecimalWithFcpOrDefault("mhp", decimal.Zero);

            qlt += (double)head.GetDecimalWithFcpOrDefault("qlt", decimal.Zero);
            qlt += (double)body.GetDecimalWithFcpOrDefault("qlt", decimal.Zero);

            //计算资质加成
            var neatk = gameItem.GetDecimalWithFcpOrDefault("neatk", decimal.Zero);
            var nemhp = gameItem.GetDecimalWithFcpOrDefault("nemhp", decimal.Zero);
            var neqlt = gameItem.GetDecimalWithFcpOrDefault("nemhp", decimal.Zero);
            atk *= (double)(100 + neatk) / 100;
            mhp *= (double)(100 + nemhp) / 100;
            qlt *= (double)(100 + neqlt) / 100;
            //计算其他加成，如时装
            var ary = gameItem.Children.Where(c => c.Id != head.Id && c.Id != body.Id).ToArray();
            atk += (double)ary.Sum(c => c.GetDecimalWithFcpOrDefault("atk", decimal.Zero));
            mhp += (double)ary.Sum(c => c.GetDecimalWithFcpOrDefault("mhp", decimal.Zero));
            qlt += (double)ary.Sum(c => c.GetDecimalWithFcpOrDefault("qlt", decimal.Zero));

            var gc = gameItem.GetGameChar();
            if (null == gc)
                return false;
            //获取对应神纹
            var slotShenwen = gc.GetShenwenBag();
            if (slotShenwen != null)
            {
                var shenwen = slotShenwen.Children.FirstOrDefault(c =>
                {
                    if (!World.PropertyManager.TryGetDecimalWithFcp(c, "body", out var bodyDec))
                        return false;
                    return bodyDec == bodyGid;
                });
                //计算神纹加成
                atk += (double)shenwen.GetDecimalWithFcpOrDefault("atk", decimal.Zero);
                mhp += (double)shenwen.GetDecimalWithFcpOrDefault("mhp", decimal.Zero);
                qlt += (double)shenwen.GetDecimalWithFcpOrDefault("qlt", decimal.Zero);
                dic["atk"] = atk;
                dic["mhp"] = mhp;
                dic["qlt"] = qlt;
            }
            //计算主动技能等级
            //var ssc = shenwen.GetDecimalWithFcpOrDefault("sscatk", decimal.Zero) + shenwen.GetDecimalWithFcpOrDefault("sscmhp", decimal.Zero) + shenwen.GetDecimalWithFcpOrDefault("sscqlt", decimal.Zero);
            var lv = (int)body.GetDecimalWithFcpOrDefault("lv", decimal.Zero);
            int lvZhudong = Array.FindLastIndex(_aryLvZuoqi, c => lv >= c); //主动技能等级
            //计算被动技能等级
            var lvBeidong = Array.FindLastIndex(_aryLvZuoqi, c => lv >= c);  //被动技能等级

            var abi = mhp + atk * 10 + qlt * 10;    //战力
            abi += 500 + 100 * lvZhudong;   //合并主动技能战力
            abi += 500 + 100 * lvBeidong;   //合并被动技能战力
            dic["lvzhudong"] = lvZhudong;
            dic["lvbeidong"] = lvBeidong;
            dic["abi"] = abi;
            return true;
        }

        /// <summary>
        /// 获取纯种坐骑总战力。
        /// </summary>
        /// <param name="gc"></param>
        /// <returns></returns>
        public decimal GetTotalAbility(GameChar gc)
        {
            var dic = AutoClearPool<Dictionary<string, double>>.Shared.Get();
            var bag = gc.GetZuojiBag();
            var gim = World.ItemManager;
            var gis = bag.Children.Where(c => gim.IsChunzhongMounts(c));
            decimal result = 0;
            foreach (var gi in gis)
            {
                World.CombatManager.UpdateAbility(gi, dic);
                result += (decimal)dic.GetValueOrDefault("abi");
                dic.Clear();
            }
            AutoClearPool<Dictionary<string, double>>.Shared.Return(dic);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="dic"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetCombatProperties(GameItemBase thing, IEnumerable<string> propertyNames, IDictionary<string, float> dic)
        {
            foreach (var name in propertyNames)
            {
                dic[name] = OwConvert.TryToDecimal(thing.GetSdpValueOrDefault(name, 0m), out var tmp) ? (float)tmp : 0f;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCombatProperties(GameItemBase thing, IReadOnlyDictionary<string, float> dic)
        {
            thing.SetSdp("atk", (float)thing.GetSdpDecimalOrDefault("atk") + dic.GetValueOrDefault("atk"));
            thing.SetSdp("mhp", (float)thing.GetDecimalWithFcpOrDefault("mhp") + dic.GetValueOrDefault("mhp"));
            thing.SetSdp("qlt", (float)thing.GetDecimalWithFcpOrDefault("qlt") + dic.GetValueOrDefault("qlt"));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MultCombatProperties(GameItemBase thing, IReadOnlyDictionary<string, float> dic)
        {
            thing.SetSdp("atk", (float)thing.GetDecimalWithFcpOrDefault("atk") * dic.GetValueOrDefault("atk"));
            thing.SetSdp("mhp", (float)thing.GetDecimalWithFcpOrDefault("mhp") * dic.GetValueOrDefault("mhp"));
            thing.SetSdp("qlt", (float)thing.GetDecimalWithFcpOrDefault("qlt") * dic.GetValueOrDefault("qlt"));
        }

        /// <summary>
        /// 获取指定的战斗对象。
        /// </summary>
        /// <param name="datas"></param>
        public void GetCombat(GetCombatDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
            {
                datas.FillErrorFromWorld();
                return;
            }
            using var dw = World.CombatManager.GetAndLockCombat(datas.CombatId, out var combat);
            if (dw.IsEmpty)
            {
                datas.FillErrorFromWorld();
                return;
            }
            datas.Combat = combat;
            return;
        }

        /// <summary>
        /// 用指定id锁定战斗对象并返回(出参)。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="combat">在锁定键值的锁不为空的情况下返回战斗对象。</param>
        /// <returns>解除锁定键值的锁，配合使用using语法解锁，<see cref="DisposeHelper{T}.IsEmpty"/>可以测定锁定是否成功。
        /// </returns>
        public DisposeHelper<object> GetAndLockCombat(Guid id, out GameCombat combat)
        {
            var cache = World.GameCache;
            var key = id.ToString();
            var dwKey = cache.Lock(key);
            try
            {
                if (dwKey.IsEmpty)
                {
                    combat = default;
                    return DisposeHelper.Empty<object>();
                }
                var thing = cache.GetOrLoad<VirtualThing>(key, c => c.Id == id);
                if (thing is null)
                {
                    combat = default;
                    dwKey.Dispose();
                    return DisposeHelper.Empty<object>();
                }
                combat = thing.GetJsonObject<GameCombat>(); //获取到当前战斗对象
            }
            catch (Exception)
            {
                dwKey.Dispose();
                throw;
            }
            return dwKey;
        }
    }

    /// <summary>
    /// 获取战斗对象的工作数据对象。
    /// </summary>
    public class GetCombatDatas : ComplexWorkGameContext
    {
        public GetCombatDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public GetCombatDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public GetCombatDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        public Guid CombatId { get; set; }

        public GameCombat Combat { get; set; }
    }

    public static class GameCombatManagerExtensions
    {

    }

    public class AbortPvpDatas : ComplexWorkGameContext
    {
        public AbortPvpDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public AbortPvpDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public AbortPvpDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 要放弃的原始战斗Id。
        /// </summary>
        public Guid CombatId { get; set; }

    }

}
