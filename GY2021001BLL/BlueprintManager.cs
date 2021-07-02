using GY2021001DAL;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OwGame;
using OwGame.Expression;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GY2021001BLL
{
    public class GameManagerPropertyHelper : GameThingPropertyHelper
    {
        private readonly GameItemManager _Manager;

        public GameManagerPropertyHelper(GameItemManager manager)
        {
            _Manager = manager;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object GetValue(object obj, string propertyName, object defaultValue = null)
        {
            var gameItem = obj as GameItem;
            if (null == gameItem)
                return defaultValue;
            return _Manager.GetPropertyValue(gameItem, propertyName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool SetValue(object obj, string propertyName, object val)
        {
            var gameItem = obj as GameItem;
            if (null == gameItem)
                return false;
            return _Manager.SetPropertyValue(gameItem, propertyName, val);
        }
    }

    public class BlueprintData
    {
        IServiceProvider _Service;
        private readonly BlueprintTemplate _Template;

        public IServiceProvider Service { get => _Service; set => _Service = value; }

        public BlueprintTemplate Template => _Template;

        private List<FormulaData> _Formulas;
        public List<FormulaData> Formulas { get => _Formulas; }

        public BlueprintData(IServiceProvider service, BlueprintTemplate template)
        {
            _Service = service;
            _Template = template;
            _Formulas = template.FormulaTemplates.Select(c => new FormulaData(c, this)).ToList();
        }

        public void Apply(ApplyBlueprintDatas datas)
        {
            var world = Service.GetRequiredService<VWorld>();
            var formus = Formulas.OrderBy(c => c.Template.OrderNumber).ToArray();

            foreach (var item in formus)    //执行所有公式
            {
                if (!item.IsMatched) //若不可用
                    continue;
                if (!item.Template.ProbExpression.TryGetValue(item.RuntimeEnvironment, out var probObj) || !OwHelper.TryGetDecimal(probObj, out var prob))  //若无法得到命中概率
                    continue;
                if (!world.IsHit((double)prob)) //若未命中
                    continue;
                if (item.Apply(datas))   //若执行蓝图成功
                {
                    datas.FormulaIds.Add(item.Template.Id);
                    if (!item.Template.IsContinue)  //若无需继续
                        break;
                }
            }
            ChangesItem.Reduce(datas.ChangesItem);
            return;
        }

        public void Match(ApplyBlueprintDatas datas)
        {
            foreach (var item in Formulas)
            {
                item.Match(datas);
            }
        }
    }

    public class FormulaData
    {
        private readonly BpFormulaTemplate _Template;
        private readonly BlueprintData _Parent;

        public FormulaData(BpFormulaTemplate template, BlueprintData parent)
        {
            _Parent = parent;
            _Template = template;
            _Materials = template.BptfItemTemplates.Select(c => new MaterialData(c, this)).ToList();
            template.SetService(parent.Service);
        }

        public BpFormulaTemplate Template => _Template;

        public BlueprintData Parent => _Parent;

        /// <summary>
        /// 脚本的运行时环境。
        /// </summary>
        GameExpressionRuntimeEnvironment _RuntimeEnvironment;

        /// <summary>
        /// 脚本的运行时环境。每个公式独立。
        /// </summary>
        public GameExpressionRuntimeEnvironment RuntimeEnvironment => _RuntimeEnvironment ??= new GameExpressionRuntimeEnvironment(Template?.CompileEnvironment);

        private List<MaterialData> _Materials;
        public List<MaterialData> Materials { get => _Materials; }

        public bool IsMatched { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public bool Match(ApplyBlueprintDatas datas)
        {
            var tmpList = Materials.Where(c => !c.Template.IsNew) //排除新建物品
                .ToList();
            var coll = datas.GameItems.Concat(OwHelper.GetAllSubItemsOfTree(datas.GameChar.GameItems, c => c.Children)).ToList();    //要遍历的所有物品，确保指定物品最先被匹配
            RuntimeEnvironment.StartScope();
            try
            {
                while (tmpList.Count > 0)   //当还有未匹配的原料时
                {
                    var succ = false;
                    for (int i = tmpList.Count - 1; i >= 0; i--)
                    {
                        var item = tmpList[i];
                        if (item.Match(coll, out GameItem gameItem))
                        {
                            tmpList.RemoveAt(i);
                            while (coll.Remove(gameItem)) ;
                            succ = true;
                        }
                    }
                    if (!succ)   //若本轮没有任何一个原料匹配上
                        break;
                }
            }
            finally
            {
                IsMatched = tmpList.All(c => c.Template.IsNew || c.Template.AllowEmpty);  //所有非必要或已存在原料项都匹配了则说明成功
                RuntimeEnvironment.EndScope(IsMatched);
            }
            return IsMatched;
        }

        public bool Apply(ApplyBlueprintDatas datas)
        {
            bool succ = false;
            if (!IsMatched)
            {
                datas.DebugMessage = "未匹配的公式不能应用。";
                datas.HasError = true;
                return succ;
            }
            try
            {
                succ = Materials.Where(c => c.Template.IsNew).All(c => c.CreateNewItem());

                if (succ)
                {
                    datas.GameChar.GameUser.DbContext.AddRange(Materials.Where(c => c.Template.IsNew).Select(c => c.GetMatched()));
                    succ = Materials.OrderBy(c => c.Template.PropertiesChanges)   //TO DO
                        .All(c => c.Apply(datas));
                }
                else
                {
                    datas.DebugMessage = "至少有一个新建物品无法创建";
                    datas.HasError = true;
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message);   //TO DO
                datas.HasError = true;
            }
            return succ;
        }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class MaterialData
    {
        private readonly FormulaData _Parent;
        private readonly BpItemTemplate _Template;

        public MaterialData(BpItemTemplate template, FormulaData parent)
        {
            _Parent = parent;
            _Template = template;
        }

        public BpItemTemplate Template => _Template;

        public FormulaData Parent => _Parent;

        private decimal? _CountIncrement;

        /// <summary>
        /// 增量,仅计算一次，避免反复计算随机数。
        /// </summary>
        public decimal CountIncrement
        {
            get
            {
                if (_CountIncrement is null)    //若尚未计算
                {
                    GetIncrement(out var min, out var max);
                    _CountIncrement = (decimal)VWorld.WorldRandom.NextDouble() * (max - min) + min;
                }
                return _CountIncrement.Value;
            }
        }

        /// <summary>
        /// 数量变化的概率。
        /// </summary>
        public decimal CountIncrementProb
        {
            get
            {
                decimal result = decimal.Zero;
                if (!Template.CountProbExpression.TryGetValue(Parent.RuntimeEnvironment, out var resultObj) || !OwHelper.TryGetDecimal(resultObj, out result))
                    Debug.Fail($"无法获取增量发生的概率。原料Id={Template.Id}({Template.Remark})");
                return result;
            }
        }

        decimal? _Min, _Max;
        /// <summary>
        /// 最小和最大的增量值。仅计算一次，后续调用返回缓存，避免多次计算随机数。
        /// </summary>
        public bool GetIncrement(out decimal min, out decimal max)
        {
            if (_Min is null)
            {
                Debug.Assert(_Max is null);
                var env = Parent.RuntimeEnvironment;
                bool succ = Template.TryGetLowerBound(env, out var lower);
                Debug.Assert(succ);
                succ = Template.TryGetUpperBound(env, out var upper);
                Debug.Assert(succ);
                _Min = Math.Min(upper, lower);
                _Max = Math.Max(upper, lower);
            }
            min = _Min.Value;
            max = _Max.Value;
            return true;
        }

        /// <summary>
        /// 找到匹配的原料。
        /// </summary>
        /// <param name="gameItems">搜索的物品集合。</param>
        /// <param name="matchItem">返回true时，这个出参包含匹配的物品对象。</param>
        /// <returns></returns>
        public bool Match(IEnumerable<GameItem> gameItems, out GameItem matchItem)
        {
            bool result = false;
            matchItem = null;
            var coll = gameItems;
            var env = Parent.RuntimeEnvironment;
            env.StartScope();
            try
            {
                ConstGExpression constExpr;
                var id = Template.Id.ToString();
                if (env.Variables.TryGetValue(id, out var oRxpr))
                {
                    Debug.Assert(oRxpr is ConstGExpression);
                    constExpr = oRxpr as ConstGExpression;
                }
                else
                    env.Variables[id] = constExpr = new ConstGExpression();
                foreach (var item in coll)
                {
                    var _ = constExpr.SetValue(env, item);   //设置对象
                    Debug.Assert(_);
                    if (!Template.ConditionalExpression.TryGetValue(env, out var matchObj) || !(matchObj is bool isMatth) || !isMatth) //若不符合条件
                        continue;
                    if (OwHelper.TryGetDecimal(Template.CountProbExpression.GetValueOrDefault(env, 0), out var countProp) && countProp > 0) //若概率可能大于0 TO DO
                    {
                        //校验数量
                        if (OwHelper.TryGetDecimal(Template.CountLowerBoundExpression.GetValueOrDefault(env, 0), out var lower))
                            ;
                        if (OwHelper.TryGetDecimal(Template.CountUpperBoundExpression.GetValueOrDefault(env, 0), out var upper))
                            ;
                        if (Math.Min(lower, upper) + (item.Count ?? 0) < 0) //若数量不够
                            continue;
                    }
                    matchItem = item;
                    result = true;
                    break;
                }
            }
            finally
            {
                env.EndScope(result);
            }
            return result;
        }

        public bool Apply(ApplyBlueprintDatas datas)
        {
            var env = Parent.RuntimeEnvironment;
            GameItem gameItem;
            GameItemManager gim;
            //获取该原料对象
            if (!env.Variables.TryGetValue(Template.Id.ToString(), out var expr) || !expr.TryGetValue(env, out var obj) || !(obj is GameItem))
                return false || Template.AllowEmpty;
            else
                gameItem = obj as GameItem;
            //修改数量
            if (!Template.CountProbExpression.TryGetValue(env, out var countPropObj) || !OwHelper.TryGetDecimal(countPropObj, out var prob)) //若无法获取概率
                return false;
            var world = Parent.Parent.Service.GetRequiredService<VWorld>();
            gim = Parent.Parent.Service.GetService<GameItemManager>();
            decimal count = gameItem.Count.Value;
            if (world.IsHit((double)CountIncrementProb)) //若需要增量
            {
                var inc = CountIncrement;
                if (gameItem.Count + inc < 0)
                    return false;
                count = inc + gameItem.Count.Value;
                var _ = gim.SetPropertyValue(gameItem, "count", count);
                Debug.Assert(_);
            }
            if (!Template.PropertiesChangesExpression.TryGetValue(env, out _))
                return false;
            if (gameItem.Count.Value > 0) //若有剩余
                if (Template.IsNew)
                    datas.ChangesItem.AddToAdds(gameItem.ParentId ?? gameItem.OwnerId.Value, gameItem);
                else
                    datas.ChangesItem.AddToChanges(gameItem.ParentId ?? gameItem.OwnerId.Value, gameItem);
            else //若没有剩余
                datas.ChangesItem.AddToRemoves(gameItem.ParentId ?? gameItem.OwnerId.Value, gameItem.Id);
            return true;
        }

        private string GetDebuggerDisplay()
        {
            var str1 = string.IsNullOrWhiteSpace(Template?.DisplayName) ? Template?.Remark : Template?.DisplayName;
            return $"{{{str1},Matched={GetMatched()}}}";
        }

        /// <summary>
        /// 获取改原料对象当前匹配的对象。
        /// </summary>
        /// <returns></returns>
        public object GetMatched()
        {
            if (null == Template)
                return null;
            var env = Parent.RuntimeEnvironment;
            if (!env.TryGetVariableValue(Template.Id.ToString(), out var result))
                return null;
            return result;
        }

        /// <summary>
        /// 返回设置模板Id的表达式。
        /// </summary>
        /// <returns></returns>
        public BinaryGExpression GetSetTIdExpr()
        {
            var setTidExpr = (Template.PropertiesChangesExpression as BlockGExpression).Expressions.OfType<BinaryGExpression>().FirstOrDefault(c =>
            {
                if (c.Left is ReferenceGExpression refExpr && refExpr.Name == "tid" && refExpr.ObjectId == Template.Id.ToString() && c.Operator == "=")    //若是设置此条目的模板
                    return true;
                return false;
            });
            return setTidExpr;
        }

        /// <summary>
        /// 创建新建项。
        /// </summary>
        /// <returns>true成功创建，false该项不是新建物品或无法找到指定模板Id。</returns>
        public bool CreateNewItem()
        {
            var env = Parent.RuntimeEnvironment;

            var setTidExpr = GetSetTIdExpr();
            if (null == setTidExpr || !setTidExpr.Right.TryGetValue(env, out var tidObj) || !OwHelper.TryGetGuid(tidObj, out var tid))
            {
                //datas.DebugMessage = "未能找到新建物品的模板Id。";
                //datas.HasError = true;
                return false;
            }
            var gim = Parent.Parent.Service.GetRequiredService<GameItemManager>();
            var gameItem = gim.CreateGameItem(tid);
            var keyName = Template.Id.ToString();
            GameExpressionBase expr;
            if (env.Variables.TryGetValue(keyName, out expr) && expr is ConstGExpression)   //若已经存在该变量
                return expr.SetValue(env, gameItem);
            else
                env.Variables[keyName] = new ConstGExpression(gameItem);
            return true;
        }
    }

    /// <summary>
    /// 使用蓝图的数据。
    /// </summary>
    public class ApplyBlueprintDatas
    {
        public ApplyBlueprintDatas()
        {

        }

        /// <summary>
        /// 蓝图的模板。
        /// </summary>
        public BlueprintTemplate Blueprint { get; set; }

        /// <summary>
        /// 角色对象。
        /// </summary>
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要执行蓝图制造的对象集合。可以仅给出关键物品，在制造过成中会补足其他所需物品。
        /// </summary>
        public List<GameItem> GameItems { get; } = new List<GameItem>();

        /// <summary>
        /// 键是物料模板的Id,值对应的物料对象。
        /// </summary>
        public Dictionary<Guid, GameItem> Items { get; } = new Dictionary<Guid, GameItem>();

        /// <summary>
        /// 提前计算好每种原料的增量。
        /// 键是原料对象Id,值（最低增量,最高增量,用随机数计算得到的增量）的值元组。
        /// </summary>
        internal Dictionary<Guid, (decimal, decimal, decimal)> Incrementes { get; } = new Dictionary<Guid, (decimal, decimal, decimal)>();

        /// <summary>
        /// 要执行的次数。
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 获取或设置成功执行的次数。
        /// </summary>
        public int SuccCount { get; set; }

        /// <summary>
        /// 应用蓝图后，物品变化数据。
        /// </summary>
        public List<ChangesItem> ChangesItem { get; } = new List<ChangesItem>();

        /// <summary>
        /// 是否有错误。
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage { get; set; }

        /// <summary>
        /// 返回命中公式的Id集合。
        /// </summary>
        public List<Guid> FormulaIds { get; } = new List<Guid>();
    }

    /// <summary>
    /// 蓝图管理器配置数据。
    /// </summary>
    public class BlueprintManagerOptions
    {
        public BlueprintManagerOptions()
        {

        }

        public Func<IServiceProvider, ApplyBlueprintDatas, bool> DoApply { get; set; }
    }

    /// <summary>
    /// 蓝图管理器。
    /// </summary>
    public class BlueprintManager : GameManagerBase<BlueprintManagerOptions>
    {
        public BlueprintManager()
        {
            Initialize();
        }

        public BlueprintManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public BlueprintManager(IServiceProvider service, BlueprintManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        private void Initialize()
        {
            lock (ThisLocker)
                _InitializeTask ??= Task.Run(() =>
                {
                    _Id2BlueprintTemplate = Context.Set<BlueprintTemplate>().Include(c => c.FormulaTemplates).ThenInclude(c => c.BptfItemTemplates).ToDictionary(c => c.Id);
                    _Id2Template = _Id2BlueprintTemplate.SelectMany(c => c.Value.FormulaTemplates)
                                                        .SelectMany(c => c.BptfItemTemplates)
                                                        .OfType<GameThingTemplateBase>()
                                                        .Concat(_Id2BlueprintTemplate.SelectMany(c => c.Value.FormulaTemplates).OfType<GameThingTemplateBase>())
                                                        .Concat(_Id2BlueprintTemplate.Select(c => c.Value).OfType<GameThingTemplateBase>())
                                                        .ToDictionary(c => c.Id);
                });
        }

        Task _InitializeTask;

        private DbContext _DbContext;

        public DbContext Context { get => _DbContext ??= World.CreateNewTemplateDbContext(); }

        Dictionary<Guid, BlueprintTemplate> _Id2BlueprintTemplate;

        /// <summary>
        /// 记录所有蓝图模板的字典，键是蓝图Id值是蓝图模板对象。
        /// </summary>
        public Dictionary<Guid, BlueprintTemplate> Id2BlueprintTemplate
        {
            get
            {
                _InitializeTask.Wait();
                return _Id2BlueprintTemplate;
            }
        }

        /// <summary>
        /// 所有相关对象的加速字典。
        /// </summary>
        private Dictionary<Guid, GameThingTemplateBase> _Id2Template;

        /// <summary>
        /// 获取指定Id的模板对象。
        /// </summary>
        /// <param name="id">模板对象的Id。</param>
        /// <returns>模板对象，如果没有找到则返回null。</returns>
        public GameThingTemplateBase GetTemplateFromId(Guid id)
        {
            _InitializeTask.Wait();
            return _Id2Template.GetValueOrDefault(id);
        }

        /// <summary>
        /// 使用指定数据升级或制造物品。
        /// </summary>
        /// <param name="datas"></param>
        public void ApplyBluprint(ApplyBlueprintDatas datas)
        {
            if (datas.Count <= 0)
            {
                datas.HasError = true;
                datas.DebugMessage = "Count必须大于0";
                return;
            }
            var gu = datas.GameChar.GameUser;
            var tmpList = World.ObjectPoolListGameItem.Get();
            if (!World.CharManager.Lock(gu))
            {
                datas.HasError = true;
                datas.DebugMessage = $"无法锁定用户{gu.Id}";
                return;
            }
            try
            {
                _InitializeTask.Wait(); //等待初始化结束
                //获取有效的参与制造物品对象
                if (!World.ItemManager.GetItems(datas.GameItems.Select(c => c.Id), tmpList, datas.GameChar))
                    return;
                datas.GameItems.Clear();
                datas.GameItems.AddRange(tmpList);
                if (new Guid("{7F35CDA3-316D-4BE6-9CCF-C348BB7DD28B}") == datas.Blueprint.Id)
                {
                    GetFhResult(datas);
                }
                else if (new Guid("{972C78BF-773C-4DE7-95DB-5FD685A9A263}") == datas.Blueprint.Id)  //若是加速孵化
                {
                    JiasuFuhua(datas);
                }
                else if (new Guid("{7B1348B8-87DE-4C98-98B8-4705340E1ED2}") == datas.Blueprint.Id)  //若是增加体力
                {
                    AddTili(datas);
                }
                else
                {
                    var data = new BlueprintData(Services, datas.Blueprint);
                    for (int i = 0; i < datas.Count; i++)
                    {
                        data.Match(datas);
                        if (!data.Formulas.Any(c => c.IsMatched))  //若已经没有符合条件的公式。
                        {
                            datas.DebugMessage = $"计划制造{datas.Count}次,实际成功{i}次后，原料不足";
                            break;
                        }
                        foreach (var item in data.Formulas)
                        {
                            if (!item.IsMatched)
                                continue;
                            foreach (var meter in item.Materials)
                            {
                                meter.Template.VariableDeclaration.OfType<ReferenceGExpression>().All(c => c.Cache(item.RuntimeEnvironment));
                            }
                        }
                        data.Apply(datas);
                        datas.SuccCount++;
                    }
                }
                ChangesItem.Reduce(datas.ChangesItem);    //压缩变化数据
                switch (datas.Blueprint.Id.ToString("D").ToLower())
                {
                    case "8b4ac76c-d8cc-4300-95ca-668350149821": //针对孵化蓝图
                        var tmp = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.FuhuaSlotTId);
                        var slotFh = datas.ChangesItem.FirstOrDefault(c => c.ContainerId == tmp.Id);    //孵化容器
                        if (slotFh == null)
                            break;
                        var gameItem = slotFh.Adds.FirstOrDefault();    //孵化的组合
                        if (gameItem == null)
                            break;
                        var containerMounts = datas.ChangesItem.FirstOrDefault(c => c.ContainerId == gameItem.Id);    //组合容器
                        Debug.Assert(containerMounts.Adds.Count == 2);
                        gameItem.Children.AddRange(containerMounts.Adds);
                        datas.ChangesItem.Remove(containerMounts);
                        break;
                    default:
                        break;
                }
                World.CharManager.NotifyChange(gu);
            }
            finally
            {
                if (null != tmpList)
                    World.ObjectPoolListGameItem.Return(tmpList);
                World.CharManager.Unlock(gu, true);
            }
        }

        /// <summary>
        /// 取出孵化物品。
        /// </summary>
        /// <param name="datas"></param>
        public void GetFhResult(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameItems.Count > 0, "参数过少。")) return;
            var slotFh = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.FuhuaSlotTId); //孵化槽
            if (null == slotFh)
            {
                datas.HasError = true;
                datas.DebugMessage = "找不到孵化槽。";
                return;
            }
            var gameItem = datas.GameItems.Join(slotFh.Children, c => c.Id, c => c.Id, (l, r) => r).FirstOrDefault();    //要取出的物品
            if (null == gameItem)
            {
                datas.HasError = true;
                datas.DebugMessage = "找不到要取出的物品。";
                return;
            }
            if (!datas.Verify(gameItem.Name2FastChangingProperty.TryGetValue("fhcd", out var fcp), $"孵化物品没有冷却属性。Id = {gameItem.Id}")) return;
            if (!datas.Verify(fcp.IsComplate, $"物品没有孵化完成。Id = {gameItem.Id}。")) //若未完成孵化
                return;
            var slotZq = datas.GameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ZuojiBagSlotId);   //坐骑背包
            var gim = World.ItemManager;
            var headTid = gim.GetHead(gameItem).TemplateId;
            var bodyTid = gim.GetBody(gameItem).TemplateId;
            var zq = slotZq.Children.FirstOrDefault(c =>    //找同头同身坐骑
            {
                return c.Children.Any(c2 => c2.TemplateId == headTid) && c.Children.Any(c2 => c2.TemplateId == bodyTid);
            });
            if (null != zq)    //若已经有同种坐骑
            {
                var slotSl = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ShoulanSlotId);
                if (!datas.Verify(null != slotSl, "找不到兽栏。")) return;
                gameItem.Properties["neatk"] = Math.Round(gameItem.GetDecimalOrDefault("neatk"), MidpointRounding.AwayFromZero);
                gameItem.Properties["nemhp"] = Math.Round(gameItem.GetDecimalOrDefault("nemhp"), MidpointRounding.AwayFromZero);
                gameItem.Properties["neqlt"] = Math.Round(gameItem.GetDecimalOrDefault("neqlt"), MidpointRounding.AwayFromZero);
                gim.MoveItem(gameItem, 1, slotSl, datas.ChangesItem);
            }
            else //若尚无同种坐骑
            {
                gameItem.Properties["neatk"] = 10m;
                gameItem.Properties["nemhp"] = 10m;
                gameItem.Properties["neqlt"] = 10m;
                gim.MoveItem(gameItem, 1, slotZq, datas.ChangesItem);
            }
        }

        /// <summary>
        /// 加速孵化。
        /// </summary>
        /// <param name="datas"></param>
        public void JiasuFuhua(ApplyBlueprintDatas datas)
        {
            var fhSlot = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.FuhuaSlotTId);    //孵化槽
            var gameItem = fhSlot.Children.FirstOrDefault(c => c.Id == datas.GameItems[0].Id);  //要加速孵化的物品
            if (!gameItem.Name2FastChangingProperty.TryGetValue("fhcd", out var fcp))
            {
                datas.HasError = true;
                datas.DebugMessage = "孵化物品没有冷却属性";
                return;
            }
            //计算所需钻石
            var zuanshi = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ZuanshiId);    //钻石
            var tm = (fcp.MaxValue - fcp.GetCurrentValueWithUtc()) / 60;
            decimal cost;
            if (tm <= 5)   //若不收费
                cost = 0;
            else
                cost = Math.Ceiling(tm - 5);
            if (zuanshi.Count < cost)
            {
                datas.HasError = true;
                datas.DebugMessage = $"需要{cost}钻石,但目前仅有{zuanshi.Count}个钻石。";
                return;
            }
            //减少钻石
            zuanshi.Count -= cost;
            var gim = World.ItemManager;
            datas.ChangesItem.AddToChanges(zuanshi.ParentId ?? zuanshi.OwnerId.Value, zuanshi);
            //修改冷却时间
            fcp.LastValue = fcp.MaxValue;
            fcp.LastComputerDateTime = DateTime.UtcNow;
            datas.ChangesItem.AddToChanges(gameItem.ParentId ?? gameItem.OwnerId.Value, gameItem);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Verify(ApplyBlueprintDatas datas, IEnumerable<GameItem> gameItems, Guid containerTId, Guid itemTId)
        {
            var gitm = World.ItemTemplateManager;
            var cTemplate = gitm.GetTemplateFromeId(containerTId);
            if (datas.Verify(cTemplate != null, $"无法找到指定容器模板，Id = {containerTId}"))
                return false;
            var container = gameItems.FirstOrDefault(c => c.TemplateId == containerTId);
            datas.Verify(container != null, $"无法找到指定模板Id的容器，模板Id = {containerTId}");
            return true;
        }

        /// <summary>
        /// 钻石买体力。
        /// </summary>
        /// <param name="datas"></param>
        public void AddTili(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameChar.GradientProperties.TryGetValue("pp", out var fcp), "无法找到体力属性"))
                return;
            var zuanshi = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ZuanshiId);
            if (!datas.Verify(zuanshi != null, "无法找到钻石对象"))
                return;
            if (!datas.Verify(zuanshi.Count > 20, $"需要20钻石，但目前仅有{zuanshi.Count}。"))
                return;
            zuanshi.Count -= 20;    //扣除钻石
            fcp.GetCurrentValueWithUtc();
            fcp.LastValue += 20;    //增加体力
            datas.ChangesItem.AddToChanges(datas.GameChar.Id, zuanshi);
        }
    }

    public static class ApplyBlueprintDatasExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool Verify(this ApplyBlueprintDatas obj, bool succ, string errorMessage)
        {
            if (!succ)
            {
                obj.DebugMessage = errorMessage;
                obj.HasError = true;
            }
            return succ;
        }
    }
}
