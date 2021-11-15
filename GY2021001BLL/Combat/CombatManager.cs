using Game.Social;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using OW.Game.Item;
using OW.Game.Mission;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GuangYuan.GY001.BLL
{
    /// <summary>
    /// 战斗管理器的配置类。
    /// </summary>
    public class CombatManagerOptions
    {
        public CombatManagerOptions()
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
    public class StartCombatData : GameCharWorkDataBase
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

        /// <summary>
        /// 调试信息。调试状态下返回时填写。
        /// </summary>
        public string DebugMessage { get; set; }
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
    public class CombatManager : GameManagerBase<CombatManagerOptions>
    {

        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public CombatManager() : base()
        {
            Initialize();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="serviceProvider"></param>
        public CombatManager(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Initialize();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="options"></param>
        public CombatManager(IServiceProvider serviceProvider, CombatManagerOptions options) : base(serviceProvider, options)
        {
            Initialize();
        }

        private void Initialize()
        {

        }

        #endregion 构造函数

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
                                   where tmp.Properties.ContainsKey("typ") && tmp.Properties.ContainsKey("mis") && tmp.Properties.ContainsKey("sec")
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
                                   let typ = Convert.ToInt32(tmp.Properties["typ"])
                                   let mis = Convert.ToInt32(tmp.Properties["mis"])
                                   group tmp by (typ, mis) into g
                                   let parent = g.First(c => Convert.ToInt32(c.Properties["sec"]) == -1)
                                   let children = g.Where(c => Convert.ToInt32(c.Properties["sec"]) != -1).OrderBy(c => Convert.ToInt32(c.Properties["sec"]))
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
        /// 
        /// </summary>
        /// <param name="data">终止请求的数据封装对象。</param>
        /// <returns>true正常结束，false发生错误。</returns>
        public void EndCombat(EndCombatData data)
        {
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
                var shouyiSlot = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShouyiSlotId);   //收益槽
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

                World.ItemManager.AddItems(data.GameItems.Select(c =>
                {
                    if (gim.IsMounts(c))
                    {

                        var mounts = new GameItem();
                        World.EventsManager.GameItemCreated(mounts, c.TemplateId, shouyiSlot, null,
                            new Dictionary<string, object>() { { "htid", gim.GetHeadTemplate(c).IdString }, { "btid", gim.GetBodyTemplate(c).IdString } });
                        mounts.Properties["neatk"] = c.Properties.GetDecimalOrDefault("neatk", 0);
                        mounts.Properties["nemhp"] = c.Properties.GetDecimalOrDefault("nemhp", 0);
                        mounts.Properties["neqlt"] = c.Properties.GetDecimalOrDefault("neqlt", 0);
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
                    var changes = new List<ChangeItem>();
                    //移动收益槽数据到各自背包。
                    //金币

                    gim.MoveItems(shouyiSlot, c => c.TemplateId == ProjectConstant.JinbiId, gameChar.GetCurrencyBag(), changes);
                    //野生怪物
                    var shoulan = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShoulanSlotId);
                    gim.MoveItems(shouyiSlot, c => c.TemplateId == ProjectConstant.ZuojiZuheRongqi, shoulan, changes);
                    //其他道具
                    var daojuBag = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.DaojuBagSlotId);   //道具背包
                    gim.MoveItems(shouyiSlot, c =>
                    {
                        return c.TemplateId != ProjectConstant.JinbiId && c.TemplateId != ProjectConstant.ZuojiZuheRongqi;
                    }, daojuBag, changes);
                    //压缩变化数据
                    ChangeItem.Reduce(changes);
                    data.ChangesItems.AddRange(changes);
                    //将剩余未能获取的收益放置于弃物槽中
                    var qiwu = gameChar.GetQiwuBag();
                    foreach (var item in shouyiSlot.Children)
                    {
                        data.ChangesItems.AddToRemoves(shouyiSlot.Id, item.Id);
                        gim.ForceMove(item, qiwu);
                        data.ChangesItems.AddToAdds(item);
                    }
                    //设置成就数据
                    if (data.IsWin && data.Template.Properties.GetDecimalOrDefault("typ") == 1) //若是推关
                    {
                        var mission = data.GameChar.GetRenwuSlot().Children.FirstOrDefault(c => c.TemplateId == ProjectMissionConstant.关卡成就);
                        if (null != mission)   //若找到成就对象
                        {
                            var oldVal = mission.Properties.GetDecimalOrDefault(ProjectMissionConstant.指标增量属性名);
                            mission.Properties[ProjectMissionConstant.指标增量属性名] = oldVal + 1m; //设置该成就的指标值的增量，原则上都是正值
                            World.MissionManager.ScanAsync(data.GameChar);
                        }
                    }
                    else if (data.Template.Properties.GetDecimalOrDefault("typ") == 1)   //若是塔防
                    {
                        var mission = data.GameChar.GetRenwuSlot().Children.FirstOrDefault(c => c.TemplateId == ProjectMissionConstant.累计塔防模式次数成就);
                        if (null != mission)   //若找到成就对象
                        {
                            var oldVal = mission.Properties.GetDecimalOrDefault(ProjectMissionConstant.指标增量属性名);
                            mission.Properties[ProjectMissionConstant.指标增量属性名] = oldVal + 1m; //设置该成就的指标值的增量，原则上都是正值
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

        public const string PvpRankName = "PVP排行";

        /// <summary>
        /// 获取指定用户的pvp排名。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public int GetPvpRank(GameChar gameChar)
        {
            var context = gameChar.GameUser.DbContext;
            var pvp = gameChar.GetPvpObject();
            var coll = from tmp in context.Set<GameExtendProperty>()
                       where tmp.Name == PvpRankName && (tmp.DecimalValue > pvp.Count || tmp.DecimalValue == pvp.Count && string.Compare(tmp.StringValue, gameChar.DisplayName) < 0)
                       select tmp;
            return coll.Count();
        }

        /// <summary>
        /// 用角色的最新信息更新pvp排行的缓存数据。
        /// </summary>
        /// <param name="gameChar"></param>
        public void UpdatePvpInfo(GameChar gameChar)
        {
            using var dwUser = World.CharManager.LockAndReturnDisposer(gameChar.GameUser);
            if (dwUser is null)
                return;
            var context = gameChar.GameUser.DbContext;
            var pvp = gameChar.GetPvpObject();
            var pvpInfo = gameChar.ExtendProperties.FirstOrDefault(c => c.Name == PvpRankName);
            bool isDirty = false;
            if (pvpInfo is null)
            {
                pvpInfo = new GameExtendProperty()
                {
                    Id = gameChar.Id,
                    Name = PvpRankName,
                    DecimalValue = pvp.Count,
                    StringValue = gameChar.DisplayName,
                };
                gameChar.ExtendProperties.Add(pvpInfo);
                isDirty = true;
            }
            else
            {
                pvpInfo.DecimalValue = pvp.Count;
                pvpInfo.StringValue = gameChar.DisplayName;
                isDirty = true;
            }
            if (isDirty)
                World.CharManager.NotifyChange(gameChar.GameUser);
        }

        /// <summary>
        /// 更新推关战力信息。
        /// </summary>
        /// <param name="gameChar"></param>
        public void UpdatePveInfo(GameChar gameChar)
        {
            var data = gameChar.ExtendProperties.FirstOrDefault(c => c.Name == ProjectConstant.ZhangLiName);
            if (data is null)
            {
                data = new GameExtendProperty()
                {
                    Id = gameChar.Id,
                    Name = ProjectConstant.ZhangLiName,
                    StringValue = gameChar.DisplayName,
                    DecimalValue = World.CombatManager.GetTotalAbility(gameChar),
                };
                gameChar.ExtendProperties.Add(data);
            }
            else
                data.DecimalValue = World.CombatManager.GetTotalAbility(gameChar);
        }

        /// <summary>
        /// pvp战斗结算。
        /// </summary>
        /// <param name="datats"></param>
        public void EndCombatPvp(EndCombatPvpWorkData datas)
        {
            var pTId = GetParent(datas.DungeonTemplate).Id; //大关卡模板Id
            //校验免战
            if (false)   //若免战
            {
            }
            using var dwUser = datas.LockAll();
            if (dwUser is null)
                return;
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
                datas.ErrorMessage = $"未知关卡模板Id={datas.DungeonId}";
                return;
            }
            if (!datas.HasError) //若成功
            {
                var pvp1 = datas.GameChar.GetOrAddPvpExtendProperty();
                var pvp2 = datas.OtherChar.GetOrAddPvpExtendProperty();
                datas.Combat.Properties["attackerRankBefore"] = GetPvpRank(datas.GameChar);   //进攻者排名
                datas.Combat.Properties["attackerScoreBefore"] = pvp1.DecimalValue;  //进攻者积分
                datas.Combat.Properties["defenderRankBefore"] = GetPvpRank(datas.OtherChar);
                datas.Combat.Properties["defenderScoreBefore"] = pvp2.DecimalValue;
                UpdatePvpInfo(datas.GameChar);
                UpdatePvpInfo(datas.OtherChar);
                datas.Combat.Properties["attackerRankAfter"] = GetPvpRank(datas.GameChar);   //进攻者排名
                datas.Combat.Properties["attackerScoreAfter"] = pvp1.DecimalValue;  //进攻者积分
                datas.Combat.Properties["defenderRankAfter"] = GetPvpRank(datas.OtherChar);
                datas.Combat.Properties["defenderScoreAfter"] = pvp2.DecimalValue;
                //记录信息
                var gim = World.ItemManager;
                var view = new WarNewspaperView(datas.Combat, World.Service);
                //攻击方信息
                var Me = gim.ToByteArray(gim.GetLineup(datas.GameChar, 2));
                datas.Combat.AttackerExInfo = Me;
                view.AttackerDisplayName = datas.GameChar.DisplayName;
                //防御方信息
                var other = gim.ToByteArray(gim.GetLineup(datas.OtherChar, 10000));
                datas.Combat.DefenserExInfo = other;
                view.DefenserDisplayName = datas.OtherChar.DisplayName;
                datas.Save();
            }
        }

        /// <summary>
        /// 主动pvp。
        /// </summary>
        /// <param name="datats"></param>
        public void Pvp(EndCombatPvpWorkData datas)
        {
            const string pvpChar = "PvpChar";   //PVP当日数据名的前缀
#pragma warning disable CS0219 // 变量“pvpRetaliation”已被赋值，但从未使用过它的值
            const string pvpRetaliation = "pvpRetaliation";   //反击字段
#pragma warning restore CS0219 // 变量“pvpRetaliation”已被赋值，但从未使用过它的值
            using var dwUsers = datas.LockAll();
            if (dwUsers is null) //若无法锁定对象。
            {
                datas.HasError = true;
                return;
            }
            var pvpObject = datas.GameChar.GetPvpObject();  //PVP对象
            using var todayData = TodayDataWrapper<Guid>.Create(pvpObject.Properties, pvpChar, datas.Now);
            if (!todayData.LastValues.Contains(datas.OtherChar.Id))  //若不能攻击
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "不可攻击的角色。";
                return;
            }
            datas.KeyTypes.Add((int)SocialKeyTypes.AllowPvpAttack);
            //var sr = datas.SocialRelationships.FirstOrDefault(c => c.Id2 == datas.OtherChar.Id);  //关系数据
            //if (sr is null) //若不准攻击
            //{
            //    datas.HasError = true;
            //    VWorld.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
            //    return;
            //}
            //更改数据
            var db = datas.UserContext;
            //datas.SocialRelationships.Remove(sr);
            //移除攻击权
            todayData.LastValues.Remove(datas.OtherCharId);
            GameItem pvpObj, otherPvpObj;
            //增加战报
            WarNewspaper pc = new WarNewspaper()
            {

            };
            //计算等级分
            if (datas.IsWin) //若需要计算等级分
            {
                decimal diff = 0;
                pvpObj = datas.GameChar.GetPvpObject();
                otherPvpObj = datas.OtherChar.GetPvpObject();
                diff = 1 + Math.Round((otherPvpObj.Count.Value - pvpObj.Count.Value) / 10, MidpointRounding.ToPositiveInfinity);
                diff = Math.Clamp(diff, 0, 6);
                pvpObj.Count += diff;
                otherPvpObj.Count -= diff;
                if (diff != 0)  //若等级分发生变化
                {
                    datas.World.CharManager.NotifyChange(datas.GameChar.GameUser);
                    datas.World.CharManager.NotifyChange(datas.OtherChar.GameUser);
                    datas.ChangeItems.AddToChanges(pvpObj);
                }
            }
            //计算收益
            pc.AttackerIds.Add(datas.GameChar.Id);
            pc.DefenserIds.Add(datas.OtherCharId);
            //设置复仇权力
            datas.Combat = pc;
            db.Add(pc);
            //移除攻击权
            todayData.LastValues.Remove(datas.GameChar.Id);
            todayData.Save();
            //设置战利品
            List<GameBooty> bootyOfAttacker = new List<GameBooty>();
            foreach (var item in datas.BootyOfAttacker) //进攻方战利品
            {
                var booty = new GameBooty()
                {
                    ParentId = pc.Id,
                    CharId = datas.GameChar.Id,
                    TemplateId = item.Item1,
                    Count = item.Item2,
                };
                booty.SetGameItems(World, datas.ChangeItems);   //设置物品实际增减
                datas.World.CharManager.NotifyChange(datas.GameChar.GameUser);
                bootyOfAttacker.Add(booty);
            }
            db.AddRange(bootyOfAttacker);

            List<GameBooty> bootyOfDefenser = new List<GameBooty>();
            if (!World.CharManager.IsOnline(datas.OtherCharId))    //若不在线
            {
                foreach (var item in datas.BootyOfDefenser) //防御方战利品
                {
                    var booty = new GameBooty()
                    {
                        ParentId = pc.Id,
                        CharId = datas.OtherCharId,
                        TemplateId = item.Item1,
                        Count = item.Item2,
                    };
                    booty.SetGameItems(World);   //设置物品实际增减
                    datas.World.CharManager.NotifyChange(datas.OtherChar.GameUser);
                    bootyOfDefenser.Add(booty);
                }
                db.AddRange(bootyOfDefenser);
            }
            //设置物品实际增减
            bootyOfAttacker.ForEach(c => c.SetGameItems(World, datas.ChangeItems));
            if (!World.CharManager.IsOnline(datas.OtherCharId))    //若不在线
                bootyOfDefenser.ForEach(c => c.SetGameItems(World));

            //发送奖励邮件
            var mail = new GameMail()
            {
            };
            mail.Properties["MailTypeId"] = ProjectConstant.PVP系统奖励.ToString();
            mail.Properties["CombatId"] = pc.Id.ToString();
            bootyOfAttacker.ForEach(c => c.FillToDictionary(World, mail.Properties));
            World.SocialManager.SendMail(mail, new Guid[] { datas.GameChar.Id }, SocialConstant.FromSystemId); //被攻击邮件
            //发送反击邮件
            mail = new GameMail()
            {
            };
            mail.Properties["MailTypeId"] = ProjectConstant.PVP反击邮件.ToString();
            mail.Properties["CombatId"] = pc.Id.ToString();
            bootyOfDefenser.ForEach(c => c.FillToDictionary(World, mail.Properties));
            World.SocialManager.SendMail(mail, new Guid[] { datas.OtherChar.Id }, SocialConstant.FromSystemId); //被攻击邮件
            //保存数据
            datas.Save();
            datas.HasError = false;
            datas.ErrorCode = 0;
            datas.ErrorMessage = null;
            //计算成就数据
            if (datas.IsWin)    //若进攻胜利
            {
                var mission = datas.GameChar.GetRenwuSlot().Children.FirstOrDefault(c => c.TemplateId == ProjectMissionConstant.PVP进攻成就);
                if (null != mission)   //若找到成就对象
                {
                    var oldVal = mission.Properties.GetDecimalOrDefault(ProjectMissionConstant.指标增量属性名);
                    mission.Properties[ProjectMissionConstant.指标增量属性名] = oldVal + 1m; //设置该成就的指标值的增量，原则上都是正值
                    World.MissionManager.ScanAsync(datas.GameChar);
                }
            }
            else //若防御剩余
            {
                var mission = datas.OtherChar.GetRenwuSlot().Children.FirstOrDefault(c => c.TemplateId == ProjectMissionConstant.PVP防御成就);
                if (null != mission)   //若找到成就对象
                {
                    var oldVal = mission.Properties.GetDecimalOrDefault(ProjectMissionConstant.指标增量属性名);
                    mission.Properties[ProjectMissionConstant.指标增量属性名] = oldVal + 1m; //设置该成就的指标值的增量，原则上都是正值
                    World.MissionManager.ScanAsync(datas.GameChar);
                }
            }
        }

        /// <summary>
        /// 反击pvp。
        /// </summary>
        /// <param name="datats"></param>
        private void PvpForRetaliation(EndCombatPvpWorkData datas)
        {
            using var dwUsers = datas.LockAll();
            if (dwUsers is null) //若无法锁定对象。
            {
                datas.HasError = true;
                datas.ErrorCode = VWorld.GetLastError();
                return;
            }
            var oldWar = datas.UserContext.Set<WarNewspaper>().FirstOrDefault(c => c.Id == datas.CombatId);  //原始战斗
            if (oldWar is null)
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "找不到指定的最初战斗。";
                return;
            }
            var oldView = new WarNewspaperView(oldWar, World.Service);

            if (!oldWar.DefenserIds.Contains(datas.GameChar.Id))    //若没有复仇权
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "没有复仇权";
                return;
            }
            if (oldView.Retaliationed)   //若已经反击过了
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "已经反击过了。";
                return;
            }
            var db = datas.UserContext;
            var world = datas.World;
            //更改数据
            //增加战斗记录
            WarNewspaper pc = new WarNewspaper()
            {

            };
            pc.AttackerIds.Add(datas.GameChar.Id);
            pc.DefenserIds.Add(datas.OtherCharId);
            datas.Combat = pc;
            db.Add(pc);
            //计算战利品
            if (datas.IsWin) //若反击胜利
            {
                List<GameBooty> booties = db.Set<GameBooty>().AsNoTracking().Where(c => c.ParentId == oldWar.Id && oldWar.AttackerIds.Contains(c.CharId) && c.Count != 0).ToList();  //原战斗的攻击者战利品
                if (booties.Count > 0) //若有战利品
                {
                    var attackerBooties = booties.Select(c => new GameBooty()
                    {
                        ParentId = pc.Id,
                        CharId = datas.GameChar.Id,
                        TemplateId = c.TemplateId,
                        Count = c.Count,
                    }).ToList(); //本战斗攻击者战利品
                    //设计：本次战斗防御者不丢失资源
                    //设置战利品
                    attackerBooties.ForEach(c => c.SetGameItems(world, datas.ChangeItems));
                    db.AddRange(attackerBooties);
                }
            }
            //发送邮件
            var mail = new GameMail();
            if (datas.IsWin) //反击得胜
            {
                //发送邮件
                mail.Properties["MailTypeId"] = ProjectConstant.PVP反击邮件_自己_胜利.ToString();
                mail.Properties["OldCombatId"] = oldWar.IdString;
                mail.Properties["CombatId"] = pc.IdString;
                oldView.IsCompleted = true; //反击得胜后不可再要求协助
            }
            else //反击失败
            {
                //发送邮件
                mail.Properties["MailTypeId"] = oldView.Assistanced ? ProjectConstant.PVP反击_自己_两项全失败.ToString() : ProjectConstant.PVP反击邮件_自己_失败.ToString();
                mail.Properties["OldCombatId"] = oldWar.IdString;
                mail.Properties["CombatId"] = pc.IdString;
            }
            World.SocialManager.SendMail(mail, new Guid[] { datas.GameChar.Id }, SocialConstant.FromSystemId); //被攻击邮件
            //保存数据
            oldView.Retaliationed = true;
            datas.Save();
        }

        /// <summary>
        /// 协助pvp。
        /// </summary>
        /// <param name="datats"></param>
        private void PvpForHelp(EndCombatPvpWorkData datas)
        {
            if (datas.Combat is null || datas.Combat.DefenserIds.Count <= 0)   //若找不到战报对象,或数据异常
                return;
            var world = datas.World;
            datas.OtherCharIds.AddRange(datas.Combat.DefenserIds);  //将原始战斗的防御方角色ID加入锁定范围
            using var dwUsers = datas.LockAll();    //锁定相关角色
            if (dwUsers is null)
            {
                (datas as IResultWorkData).FillErrorFromWorld();
                return;
            }
            var oldWar = datas.UserContext.Set<WarNewspaper>().FirstOrDefault(c => c.Id == datas.CombatId);  //原始战斗
            if (oldWar is null)
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "找不到指定的最初战斗。";
                return;
            }
            var oldView = new WarNewspaperView(oldWar, World.Service);
            var db = datas.UserContext;
            var assId = oldView.AssistanceId;
            if (assId != datas.GameChar.Id || !oldView.Assistancing || oldView.IsCompleted) //没有请求当前角色协助或已经结束
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = $"指定的战报对象没有请求此角色协助攻击或已经攻击过了。";
            }
            //更改数据
            var pc = new WarNewspaper  //本次战斗数据
            {
            };
            pc.AttackerIds.Add(datas.GameChar.Id);
            pc.DefenserIds.Add(datas.OtherCharId);
            datas.Combat = pc;
            db.Add(pc);
            //获取战利品
            if (datas.IsWin)    //若赢得战斗
            {
                var oriBooty = db.Set<GameBooty>().AsNoTracking().Where(c => c.ParentId == oldWar.Id && oldWar.AttackerIds.Contains(c.CharId) && c.Count != 0).ToList();     //原始战斗攻击方战利品
                var boo = oldWar.BootyOfAttacker(datas.UserContext);  //原始进攻方的战利品

                var newBooty = boo.Select(c => new GameBooty   //计算进攻方战利品
                {
                    ParentId = pc.Id,
                    CharId = datas.GameChar.Id,
                    TemplateId = c.TemplateId,
                    Count = Math.Round(c.Count * 0.3m, MidpointRounding.AwayFromZero),
                }).ToList();
                db.AddRange(newBooty);  //加入数据库
                newBooty.ForEach(c => c.SetGameItems(World, datas.ChangeItems));

                var oldBooties = boo.Select(c => new GameBooty   //原始被掠夺角色的战利品
                {
                    ParentId = pc.Id,
                    CharId = oldWar.DefenserIds.First(),
                    TemplateId = c.TemplateId,
                    Count = c.Count,
                }).ToList();
                oldBooties.ForEach(c => c.SetGameItems(world));
            }
            //保存数据
            //改写进攻权限
            oldView.Assistancing = false;
            oldView.Assistanced = true;
            oldView.IsCompleted = true && oldView.Retaliationed;
            datas.Save();
            datas.ErrorCode = ErrorCodes.NO_ERROR;
            //计算成就数据
            if (datas.IsWin)
            {
                var mission = datas.GameChar.GetRenwuSlot().Children.FirstOrDefault(c => c.TemplateId == ProjectMissionConstant.PVP助战成就);
                if (null != mission)   //若找到成就对象
                {
                    var oldVal = mission.Properties.GetDecimalOrDefault(ProjectMissionConstant.指标增量属性名);
                    mission.Properties[ProjectMissionConstant.指标增量属性名] = oldVal + 1m; //设置该成就的指标值的增量，原则上都是正值
                    World.MissionManager.ScanAsync(datas.GameChar);
                }
            }
            //发送邮件
            var mail = new GameMail()
            {
            };
            if (datas.IsWin) //若协助成功
            {
                mail.Properties["MailTypeId"] = ProjectConstant.PVP反击邮件_求助_胜利_求助者.ToString();
                mail.Properties["OldCombatId"] = oldWar.IdString;
                mail.Properties["CombatId"] = pc.IdString;
            }
            else
            {
                mail.Properties["MailTypeId"] = oldView.Retaliationed ? ProjectConstant.PVP反击_自己_两项全失败.ToString() : ProjectConstant.PVP反击邮件_求助_失败_求助者.ToString();
                mail.Properties["OldCombatId"] = oldWar.IdString;
                mail.Properties["CombatId"] = pc.IdString;
            }

            World.SocialManager.SendMail(mail, oldWar.DefenserIds, SocialConstant.FromSystemId); //被攻击邮件

        }

        /// <summary>
        /// 放弃协助或放弃自己被打的战斗。
        /// </summary>
        /// <param name="datas"></param>
        public void AbortPvp(AbortPvpDatas datas)
        {
            var db = datas.UserContext;
            var oldWar = db.Set<WarNewspaper>().Find(datas.CombatId);   //原始战斗
            if (oldWar.DefenserIds.Contains(datas.GameChar.Id))  //自己被打直接放弃
            {
                var getMails = new GameSocialManager.GetMailsDatas(Service, datas.GameChar);
                World.SocialManager.GetMails(getMails);
                if (getMails.HasError)
                {
                    datas.HasError = true;
                    datas.ErrorCode = getMails.ErrorCode;
                    datas.ErrorMessage = getMails.ErrorMessage;
                    return;
                }
                var mail = getMails.Mails.FirstOrDefault(c => c.Properties.GetGuidOrDefault("OldCombatId") == datas.CombatId && c.Properties.GetStringOrDefault("MailTypeId") == ProjectConstant.PVP系统奖励.ToString());
                if (null != mail)
                    db.Remove(mail);
                var view = new WarNewspaperView(oldWar, Service)
                {
                    IsCompleted = true
                };
                db.SaveChanges();
            }
            else
            {
                using EndCombatPvpWorkData endPvpDatas = new EndCombatPvpWorkData(World, datas.GameChar, oldWar.AttackerIds.First())
                {
                    UserContext = db,
                    CombatId = datas.CombatId,
                    DungeonId = new Guid("{2453A507-DA62-4B7E-8C07-FAE278B54B12}"),
                };
                World.CombatManager.EndCombatPvp(endPvpDatas);
                datas.HasError = endPvpDatas.HasError;
                datas.ErrorCode = endPvpDatas.ErrorCode;
                datas.ErrorMessage = endPvpDatas.ErrorMessage;
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
            _ = TimeSpan.FromSeconds(Convert.ToDouble(tm.Properties.GetValueOrDefault("tl", decimal.Zero)));   //最短时间
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
            if (dungeon.TryGetPropertyValue("gold", out var goldObj) && OwHelper.TryGetDecimal(goldObj, out var gold)) //若需要限定金币
            {
                var tmp = gameItems.Where(c => c.TemplateId == ProjectConstant.JinbiId).Sum(c => c.Count.Value);
                if (tmp > gold) //若超限
                {
                    errorString = $"金币最多允许掉落{gold}，实际掉落{tmp}。";
                    return false;
                }
            }
            if (dungeon.TryGetPropertyValue("wood", out var woodObj) && OwHelper.TryGetDecimal(woodObj, out var wood)) //若需要限定木材
            {
                var tmp = gameItems.Where(c => c.TemplateId == ProjectConstant.MucaiId).Sum(c => c.Count.Value);
                if (tmp > wood) //若超限
                {
                    errorString = $"木材最多允许掉落{wood}，实际掉落{tmp}。";
                    return false;
                }
            }
            var gim = World.ItemManager;
            var aryMounts = gameItems.Where(c => gim.IsMounts(c)).ToArray();
            if (dungeon.TryGetPropertyValue("aml", out var amlObj) && OwHelper.TryGetDecimal(amlObj, out var aml)) //若需要限定资质野怪的数量
            {
                var tmp = aryMounts.Length;
                if (tmp > aml) //若超限
                {
                    errorString = $"资质野怪最多允许掉落{aml}，实际掉落{tmp}。";
                    return false;
                }
                var mounts = gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ZuojiBagSlotId);    //坐骑包
                var lookup = mounts.Children.ToLookup(c => gim.GetMountsTIds(c));
                //var coll = aryMounts.Where(c => !lookup.Contains(gim.GetMountsTIds(c)));   //错误的资质怪类型
                //if (coll.Any())
                //{
                //    errorString = $"至少有一个资质怪的类型尚不存在对应坐骑。";
                //    return false;
                //}
            }
            if (dungeon.TryGetPropertyValue("mne", out var mneObj) && OwHelper.TryGetDecimal(mneObj, out var mne)) //若需要限定资质野怪的最高资质
            {
                var tmp = aryMounts.FirstOrDefault(c => GetTotalNe(c.Properties) > mne);
                if (null != tmp)   //若资质超限
                {
                    errorString = $"资质野怪资质总和只允许{mne},但至少有一个资质怪自制总和是{GetTotalNe(tmp.Properties)}。";
                    return false;
                }
            }
            var gimt = World.ItemTemplateManager;
            var aryShenwen = gameItems.Where(c => gimt.GetTemplateFromeId(c.TemplateId).GenusCode >= 15 && gimt.GetTemplateFromeId(c.TemplateId).GenusCode <= 17).ToArray();
            if (dungeon.TryGetPropertyValue("mt", out var mtObj) && OwHelper.TryGetDecimal(mtObj, out var mt)) //若需要限定神纹数量上限
            {
                var tmp = aryShenwen.Sum(c => c.Count ?? 1);
                if (tmp > mt)   //若神纹道具数量超限
                {
                    errorString = $"神纹道具只允许{mt}个,但掉落了{tmp}个";
                    return false;
                }
            }
            var items = gameItems.Where(c => c.TemplateId != ProjectConstant.JinbiId && c.TemplateId != ProjectConstant.MucaiId && c.TemplateId != ProjectConstant.ZuojiZuheRongqi).Except(aryShenwen);  //道具
            if (dungeon.TryGetPropertyValue("idt", out var idtObj) && OwHelper.TryGetDecimal(idtObj, out var idt)) //若需要限定其他道具数量上限
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
            var bodyGid = (int)((GameItemTemplate)body.Template).GId; //身体Id
            //计算本体属性
            var head = gim.GetHead(gameItem);
            if (null == head)
                return false;
            double atk = 0, qlt = 0, mhp = 0;
            atk += (double)head.GetDecimalOrDefault("atk", decimal.Zero);
            atk += (double)body.GetDecimalOrDefault("atk", decimal.Zero);

            mhp += (double)head.GetDecimalOrDefault("mhp", decimal.Zero);
            mhp += (double)body.GetDecimalOrDefault("mhp", decimal.Zero);

            qlt += (double)head.GetDecimalOrDefault("qlt", decimal.Zero);
            qlt += (double)body.GetDecimalOrDefault("qlt", decimal.Zero);

            //计算资质加成
            var neatk = gameItem.GetDecimalOrDefault("neatk", decimal.Zero);
            var nemhp = gameItem.GetDecimalOrDefault("nemhp", decimal.Zero);
            var neqlt = gameItem.GetDecimalOrDefault("nemhp", decimal.Zero);
            atk *= (double)(100 + neatk) / 100;
            mhp *= (double)(100 + nemhp) / 100;
            qlt *= (double)(100 + neqlt) / 100;
            //计算其他加成，如时装
            var ary = gameItem.Children.Where(c => c.Id != head.Id && c.Id != body.Id).ToArray();
            atk += (double)ary.Sum(c => c.GetDecimalOrDefault("atk", decimal.Zero));
            mhp += (double)ary.Sum(c => c.GetDecimalOrDefault("mhp", decimal.Zero));
            qlt += (double)ary.Sum(c => c.GetDecimalOrDefault("qlt", decimal.Zero));

            var gc = gim.GetChar(gameItem);
            if (null == gc)
                return false;
            //获取对应神纹
            var slotShenwen = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ShenWenSlotId);
            if (null == slotShenwen)
                return false;
            var shenwen = slotShenwen.Children.FirstOrDefault(c =>
            {
                if (!c.TryGetPropertyValue("body", out var bodyObj) || !OwHelper.TryGetDecimal(bodyObj, out var bodyDec))
                    return false;
                return bodyDec == bodyGid;
            });
            //计算神纹加成
            atk += (double)shenwen.GetDecimalOrDefault("atk", decimal.Zero);
            mhp += (double)shenwen.GetDecimalOrDefault("mhp", decimal.Zero);
            qlt += (double)shenwen.GetDecimalOrDefault("qlt", decimal.Zero);
            dic["atk"] = atk;
            dic["mhp"] = mhp;
            dic["qlt"] = qlt;
            //计算主动技能等级
            var ssc = shenwen.GetDecimalOrDefault("sscatk", decimal.Zero) + shenwen.GetDecimalOrDefault("sscmhp", decimal.Zero) + shenwen.GetDecimalOrDefault("sscqlt", decimal.Zero);
            int lvZhudong = Array.FindLastIndex(_aryTupo, c => ssc >= c); //主动技能等级
            //计算被动技能等级
            var lv = (int)gameItem.GetDecimalOrDefault("lv", decimal.Zero);
            var lvBeidong = Array.FindLastIndex(_aryLvZuoqi, c => ssc >= c);  //被动技能等级

            var abi = mhp + atk * 10 + qlt * 30;    //战力
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
            var dic = new Dictionary<string, double>();
            var bag = gc.GetZuoqiBag();
            var gim = World.ItemManager;
            var gis = bag.Children.Where(c => gim.IsChunzhongMounts(c));
            decimal result = 0;
            foreach (var gi in gis)
            {
                World.CombatManager.UpdateAbility(gi, dic);
                result += (decimal)dic.GetValueOrDefault("abi");
                dic.Clear();
            }
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
                dic[name] = OwHelper.TryGetDecimal(thing.Properties.GetValueOrDefault(name, 0m), out var tmp) ? (float)tmp : 0f;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCombatProperties(GameItemBase thing, IReadOnlyDictionary<string, float> dic)
        {
            thing.Properties["atk"] = (float)thing.Properties.GetDecimalOrDefault("atk") + dic.GetValueOrDefault("atk");
            thing.Properties["mhp"] = (float)thing.GetDecimalOrDefault("mhp") + dic.GetValueOrDefault("mhp");
            thing.Properties["qlt"] = (float)thing.GetDecimalOrDefault("qlt") + dic.GetValueOrDefault("qlt");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MultCombatProperties(GameItemBase thing, IReadOnlyDictionary<string, float> dic)
        {
            thing.Properties["atk"] = (float)thing.GetDecimalOrDefault("atk") * dic.GetValueOrDefault("atk");
            thing.Properties["mhp"] = (float)thing.GetDecimalOrDefault("mhp") * dic.GetValueOrDefault("mhp");
            thing.Properties["qlt"] = (float)thing.GetDecimalOrDefault("qlt") * dic.GetValueOrDefault("qlt");
        }

        /// <summary>
        /// 获取指定的战斗对象。
        /// </summary>
        /// <param name="datas"></param>
        public void GetCombat(GetCombatDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            var idstring = datas.GameChar.IdString;
            datas.CombatObject = datas.UserContext.Set<WarNewspaper>().AsNoTracking().FirstOrDefault(c => c.Id == datas.CombatId /*&& (c.AttackerIdString.Contains(idstring) || c.DefenserIdString.Contains(idstring))*/);
            if (datas.CombatObject is null)
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "找不到指定战斗对象。";
            }
            return;
        }
    }

    /// <summary>
    /// 获取战斗对象的工作数据对象。
    /// </summary>
    public class GetCombatDatas : ComplexWorkDatasBase
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

        public WarNewspaper CombatObject { get; set; }
    }

    public static class CombatManagerExtensions
    {
        public static GameExtendProperty GetOrAddPvpExtendProperty(this GameChar gameChar)
        {
            var pvpEp = gameChar.ExtendProperties.FirstOrDefault(c => c.Name == CombatManager.PvpRankName);
            if (pvpEp is null)
            {
                var pvp = gameChar.GetPvpObject();
                pvpEp = new GameExtendProperty()
                {
                    Id = gameChar.Id,
                    Name = CombatManager.PvpRankName,
                    DecimalValue = pvp.Count,
                    StringValue = gameChar.DisplayName,
                };
                gameChar.ExtendProperties.Add(pvpEp);
            }
            return pvpEp;
        }

    }

    /// <summary>
    /// 战斗对象的视图类。
    /// </summary>
    public class WarNewspaperView
    {
        private readonly WarNewspaper _WarNewspaper;
        private readonly IServiceProvider _Service;

        public WarNewspaperView([NotNull] WarNewspaper warNewspaper, IServiceProvider service)
        {
            _WarNewspaper = warNewspaper;
            _Service = service;
        }

        /// <summary>
        /// 获取或设置是否正在请求协助。
        /// </summary>
        public bool Assistancing
        {
            get { return _WarNewspaper.Properties.GetBooleanOrDefaut(nameof(Assistancing), false); }
            set { _WarNewspaper.Properties[nameof(Assistancing)] = value.ToString(); }
        }

        /// <summary>
        /// 获取或设置是否已经协助完毕。
        /// </summary>
        public bool Assistanced
        {
            get { return _WarNewspaper.Properties.GetBooleanOrDefaut(nameof(Assistanced), false); }
            set { _WarNewspaper.Properties[nameof(Assistanced)] = value.ToString(); }
        }

        /// <summary>
        /// 是否已经反击。
        /// </summary>
        public bool Retaliationed
        {
            get
            {
                return _WarNewspaper.Properties.GetBooleanOrDefaut("Retaliationed", false);
            }
            set
            {
                _WarNewspaper.Properties["Retaliationed"] = value.ToString();
            }
        }

        public IEnumerable<GameItem> GetAttackerMounts()
        {
            var gim = _Service.GetRequiredService<GameItemManager>();
            return gim.ToGameItems(_WarNewspaper.AttackerExInfo);
        }

        public IEnumerable<GameItem> GetDefenserMounts()
        {
            var gim = _Service.GetRequiredService<GameItemManager>();
            return gim.ToGameItems(_WarNewspaper.DefenserExInfo);
        }

        public string AttackerDisplayName
        {
            get { return _WarNewspaper.Properties.GetStringOrDefault("AttackerDisplayName"); }
            set { _WarNewspaper.Properties["AttackerDisplayName"] = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string DefenserDisplayName
        {
            get { return _WarNewspaper.Properties.GetStringOrDefault("DefenserDisplayName"); }
            set { _WarNewspaper.Properties["DefenserDisplayName"] = value; }
        }

        /// <summary>
        /// 设置或获取协助者的角色Id。
        /// </summary>
        public Guid? AssistanceId
        {
            get
            {
                if (!_WarNewspaper.Properties.ContainsKey(nameof(AssistanceId)))
                    return null;
                return _WarNewspaper.Properties.GetGuidOrDefault(nameof(AssistanceId));
            }
            set
            {
                if (value is null)
                    _WarNewspaper.Properties.Remove("AssistanceId");
                else
                    _WarNewspaper.Properties["AssistanceId"] = value.Value.ToString();
            }
        }

        /// <summary>
        /// 获取或设置战斗结果，true进攻方胜利，false进攻方失败。null无胜负。
        /// </summary>
        public bool? IsAttckerWin
        {
            get => _WarNewspaper.Properties.Get3State();
            set => _WarNewspaper.Properties.Set3State(value);
        }

        /// <summary>
        /// 获取或设置该流程是否已经结束。
        /// </summary>
        public bool IsCompleted
        {
            get { return _WarNewspaper.Properties.GetBooleanOrDefaut(nameof(IsCompleted), false); }
            set { _WarNewspaper.Properties[nameof(IsCompleted)] = value; }
        }

    }

    public class AbortPvpDatas : ComplexWorkDatasBase
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
