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
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
        public bool HasError
        {
            get;
            set;
        }

        [Conditional("DEBUG")]
        public void SetDebugMessage(string msg) => DebugMessage = msg;

        private string _DebugMessage;
        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage
        {
            get => _DebugMessage;
            set => _DebugMessage = value;
        }

        /// <summary>
        /// 返回命中公式的Id集合。
        /// </summary>
        public List<Guid> FormulaIds { get; } = new List<Guid>();

        List<Guid> _ErrorItemTIds;

        /// <summary>
        /// 获取或设置，出错虚拟物品的模板Id。具体意义根据不同蓝图区别。
        /// </summary>
        public List<Guid> ErrorItemTIds => _ErrorItemTIds ??= new List<Guid>();
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
        /// 分发需要特定处理的蓝图。
        /// </summary>
        /// <param name="datas"></param>
        /// <returns>true蓝图已经处理，false蓝图未处理。</returns>
        private bool Dispatch(ApplyBlueprintDatas datas)
        {
            bool succ;
            var idStr = datas.Blueprint.Id.ToString("D").ToLower();
            try
            {
                switch (idStr)
                {
                    case "7f35cda3-316d-4be6-9ccf-c348bb7dd28b":    //若是取蛋
                        GetFhResult(datas);
                        succ = true;
                        break;
                    case "972c78bf-773c-4de7-95db-5fd685a9a263":  //若是加速孵化
                        JiasuFuhua(datas);
                        succ = true;
                        break;
                    case "7b1348b8-87de-4c98-98b8-4705340e1ed2":  //若是增加体力
                        AddTili(datas);
                        succ = true;
                        break;
                    case "384ed85c-82fd-4f08-86e7-eae5ad6eef2c":    //家园所属虚拟物品内升级
                        UpgradeOnHomeland(datas);
                        succ = true;
                        break;
                    case "8b26f520-fbf3-4979-831c-398a0150b3da":    // 取得玉米/ 木材放入仓库
                        Harvest(datas);
                        succ = true;
                        break;
                    default:
                        succ = false;
                        break;
                }
            }
            catch (Exception err)
            {
                datas.HasError = true;
                datas.SetDebugMessage(err.Message);
                succ = true;
            }
            return succ;
        }

        #region 家园相关

        /// <summary>
        /// 收获家园中玉米，木材等资源放入仓库。
        /// </summary>
        /// <param name="datas"></param>
        private void Harvest(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameItems.Count == 1, "只能升级一个对象。")) return;
            var gameItem = datas.GameItems[0];  //收取的容器
            var gameChar = datas.GameChar;
            var hl = datas.Lookup(gameChar.GameItems, ProjectConstant.CharTemplateId, ProjectConstant.HomelandSlotId);    //家园
            if (null == hl) return;
            var mainBase = datas.Lookup(hl.Children, ProjectConstant.MainBaseSlotId); //主基地
            if (null == mainBase) return;
            if (gameItem.TemplateId == ProjectConstant.MucaishuTId)    //若收取木材
            {
                var wood = datas.Lookup(gameChar.GameItems, ProjectConstant.CharTemplateId, ProjectConstant.MucaiId); //木材
                if (null == wood) return;
                var count = gameItem.Count.Value; //收取数量
                var gim = Services.GetRequiredService<GameItemManager>();
                var stc = gim.GetNumberOfStackRemainder(wood, out _);   //可堆叠数
                count = Math.Min(count, stc);   //实际移走数量
                wood.Count += count;
                gameItem.Count -= count;
                datas.ChangesItem.AddToChanges(wood.ContainerId.Value, wood);
                datas.ChangesItem.AddToChanges(gameItem.ContainerId.Value, gameItem);
            }
            else if (gameItem.TemplateId == ProjectConstant.YumitianTId)   //若收取玉米地
            {
                var gold = datas.Lookup(gameChar.GameItems, ProjectConstant.CharTemplateId, ProjectConstant.JinbiId); //金币
                if (null == gold) return;
                var count = gameItem.Count;
                gold.Count += count;
                gameItem.Count -= count;
                datas.ChangesItem.AddToChanges(gold.ContainerId.Value, gold);
                datas.ChangesItem.AddToChanges(gameItem.ContainerId.Value, gameItem);
            }
            else
            {
                datas.ErrorItemTIds.Add(gameItem.TemplateId);
                datas.DebugMessage = $"不认识的物品，TemplateId={gameItem.TemplateId}";
                datas.HasError = true;
            }
        }

        /// <summary>
        /// 家园内部相关物品升级。
        /// </summary>
        /// <param name="datas"></param>
        private void UpgradeOnHomeland(ApplyBlueprintDatas datas)
        {
            Guid hlTid = ProjectConstant.HomelandSlotId; //家园Id
            Guid jianzhuBagTid = new Guid("{312612a5-30dd-4e0a-a71d-5074397428fb}");   //建筑背包tid
            var hl = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == hlTid);   //家园对象
            if (!datas.Verify(datas.GameItems.Count == 1, "只能升级一个对象。"))
                return;
            var gameItem = hl.AllChildren.FirstOrDefault(c => c.Id == datas.GameItems[0].Id);   //要升级的物品
            if (!datas.Verify(gameItem.ParentId.HasValue, "找不到父容器Id。"))
                return;
            var gim = World.ItemManager;
            if (!datas.Verify(OwHelper.TryGetDecimal(gameItem.GetPropertyValueOrDefault(ProjectConstant.LevelPropertyName, 0m), out var lvDec), "级别属性类型错误。"))
                return;
            var lv = (int)lvDec;    //原等级

            GameItem gold = null;
            if (gameItem.TryGetDecimalPropertyValue("lug", out var lug)) //若需要金子
            {
                gold = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.JinbiId);
                if (!datas.Verify(gold.Count >= lug, $"需要{lug}金币，目前只有{gold.Count}金币。", gold.TemplateId))
                    return;
            }
            GameItem wood = null;
            if (gameItem.TryGetDecimalPropertyValue("luw", out var luw)) //若需要木头
            {
                wood = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.MucaiId);
                if (!datas.Verify(wood.Count >= luw, $"需要{luw}木材，目前只有{wood.Count}木材。", wood.TemplateId))
                    return;
            }
            #region 冷却相关
            var fcp = FastChangingPropertyExtensions.FromDictionary(gameItem.Properties, "upgradecd");
            if (fcp != null)
                if (!datas.Verify(fcp.IsComplate, "虚拟物品还在升级中", gameItem.TemplateId))
                    return;
            var time = gameItem.GetDecimalOrDefault("lut", -2); //冷却的秒数
            var worker = hl.Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.WorkerOfHomelandTId);  //工人
            var countOfBuilding = worker.Count ?? 0;    //当前在建建筑
            var maxCountOfBuilding = worker.GetDecimalOrDefault("stc", 0m); //最大建筑队列数
            if (!datas.Verify(countOfBuilding < maxCountOfBuilding, "当前工人已全部在工作", worker.TemplateId))
                return;
            #endregion 冷却相关
            //修改属性
            if (null != wood)
                wood.Count -= luw;
            if (null != gold)
                gold.Count -= lug;
            if (time > 0) //若需要冷却
            {
                var fcpObj = new FastChangingProperty(TimeSpan.FromSeconds(1), 1, time, 0, DateTime.UtcNow)
                {
                    Name = "upgradecd",
                };
                gameItem.Name2FastChangingProperty["upgradecd"] = fcpObj;
                DateTime dtComplate = fcpObj.ComputeComplateDateTime();   //预计完成时间
                //计算可能的完成时间
                Timer timer = new Timer(UpgradeComplateCallback, (gameItem.Id), dtComplate - DateTime.UtcNow, Timeout.InfiniteTimeSpan);
            }
            else //立即完成
            {
                gim.SetPropertyValue(gameItem, ProjectConstant.LevelPropertyName, lv + 1);    //设置新等级
            }
            datas.ChangesItem.AddToChanges(gameItem.ParentId.Value, gameItem);
            fcp.Tag = (datas.GameChar.Id, gameItem.Id);
            fcp.Completed += UpgradeCompleted;
            return;
        }

        /// <summary>
        /// 加速完成家园内的升级项目。
        /// </summary>
        /// <param name="datas"></param>
        private void HastenOnHomeland(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameItems.Count == 1, "只能加速一个物品")) return;
            var gameItem = datas.GameItems[0];  //加速的物品
            var hl = datas.Lookup(datas.GameChar.GameItems, ProjectConstant.HomelandSlotId);
            if (hl is null) return;
            var worker = datas.Lookup(hl.Children, ProjectConstant.WorkerOfHomelandTId);
            if (worker is null) return;
            if (!datas.Verify(worker.Count.HasValue && worker.Count > 0, "没有在升级的物品")) return;
            if (!datas.Verify(gameItem.Name2FastChangingProperty.TryGetValue("upgradecd", out var fcp), "物品未进行升级"))
            {
                datas.ErrorItemTIds.Add(gameItem.TemplateId);
                return;
            }
            DateTime dt = DateTime.UtcNow;
            fcp.GetCurrentValue(ref dt);
            if (fcp.LastValue >= fcp.MaxValue)  //若已经完成
            {
                gameItem.RemoveFastChangingProperty("upgradecd");
                datas.ChangesItem.AddToChanges(gameItem.ContainerId.Value, gameItem);
                return;
            }
            else //若未完成
            {
                var dtComplate = fcp.ComputeComplateDateTime();
                var tm = (decimal)(dtComplate - dt).TotalMinutes;

                var cost = tm switch //需要花费的钻石
                {
                    _ when tm <= 5m => 0,
                    _ => Math.Ceiling(tm - 5),
                };
                if (cost > 0)   //若需要钻石
                {
                    var dim = datas.Lookup(datas.GameChar.GameItems, ProjectConstant.ZuanshiId);    //钻石
                    if (dim is null)
                        return;
                    if (!datas.Verify(dim.Count >= cost, $"需要{cost}钻石,但只有{dim.Count}钻石。"))
                    {
                        datas.ErrorItemTIds.Add(dim.TemplateId);
                        return;
                    }
                    dim.Count -= cost;
                    datas.ChangesItem.AddToChanges(dim.ContainerId.Value, dim);
                }
                gameItem.RemoveFastChangingProperty("upgradecd");
                datas.ChangesItem.AddToChanges(gameItem.ContainerId.Value, gameItem);
            }

            return;
        }

        #endregion 家园相关

        /// <summary>
        /// 某个物品升级结束。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpgradeCompleted(object sender, CompletedEventArgs e)
        {
            var fcp = sender as FastChangingProperty;
            if (fcp is null || fcp.Name != "upgradecd")
                return; //忽略
            if (!(fcp.Tag is ValueTuple<Guid, Guid> ids))
                return;
            var cm = World.CharManager;
            var gameChar = cm.GetCharFromId(ids.Item1);
            if (gameChar is null)   //若用户已经下线
                return;
            if (!cm.Lock(gameChar.GameUser))   //若用户已经下线
                return;
            try
            {
                var gameItem = gameChar.AllChildren.FirstOrDefault(c => c.Id == ids.Item2);
                if (gameItem is null)
                    return;
                var gim = World.ItemManager;
                var lv = (int)gameItem.GetDecimalOrDefault(ProjectConstant.LevelPropertyName, 0m);  //原等级
                gim.SetPropertyValue(gameItem, ProjectConstant.LevelPropertyName, lv + 1);    //设置新等级
                if (gameItem.TemplateId == ProjectConstant.MainBaseSlotId) //如果是主基地升级
                {

                }
            }
            catch (Exception)
            {

            }
            finally
            {
                cm.Unlock(gameChar.GameUser, true);
            }
        }

        private void MainBaseUpgraded()
        {

        }

        /// <summary>
        /// 当升级可能结束时调用。
        /// </summary>
        /// <param name="state">值元组(角色Id,虚拟物品Id)</param>
        private void UpgradeComplateCallback(object state)
        {
            var para = ((Guid, Guid))state;
            var cm = World.CharManager;
            var gc = cm.GetCharFromId(para.Item1);
            if (null == gc) //若角色不在线
                return;
            var gu = gc.GameUser;
            if (!cm.Lock(gu))   //若不能锁定
                if (null == cm.GetCharFromId(para.Item1)) //若争用导致已经下线
                    return; //可以等待下次登录时再计算
                else   //TO DO 致命问题，但目前不知道如何才会引发(大概率发生了死锁)，暂无解决方法
                {
                    var logger = Services.GetService<ILogger<BlueprintManager>>();
                    logger.LogError($"长期无法锁定在线用户，Id={gu.Id}。");
                }
            try
            {
                var gameItem = gc.AllChildren.FirstOrDefault(c => c.Id == para.Item2);  //获取结束升级的对象
                if (gameItem == null)  //若已经无效
                    return;
                var fcp = gameItem.Name2FastChangingProperty.GetValueOrDefault("upgradecd");
                if (fcp == null)    //若已经处理完毕
                    return;
                var dtComplate = fcp.ComputeComplateDateTime();   //预期完成时间
                var dtTmp = dtComplate;
                var fcpCount = gameItem.Name2FastChangingProperty.GetValueOrDefault("Count");
                if (fcp.IsComplate)  //若已经完成
                {
                    fcpCount?.GetCurrentValue(ref dtTmp);    //计算升级完成时点的数量，忽略时点回退误差
                    var gim = World.ItemManager;
                    var lv = (int)gameItem.GetDecimalOrDefault(ProjectConstant.LevelPropertyName, 0m);  //原等级
                    gim.SetPropertyValue(gameItem, ProjectConstant.LevelPropertyName, lv + 1);    //设置新等级
                }
            }
            finally
            {
                cm.Unlock(gu, true);
            }
            return;
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
                if (!Dispatch(datas))
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

        #region 孵化相关

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
            DateTime dt = DateTime.UtcNow;
            var tm = (fcp.MaxValue - fcp.GetCurrentValue(ref dt)) / 60;
            decimal cost;
            if (tm <= 5)   //若不收费
                cost = 0;
            else
                cost = Math.Ceiling(tm - 5);
            if (!datas.Verify(cost <= zuanshi.Count, $"需要{cost}钻石,但目前仅有{zuanshi.Count}个钻石。")) return;
            //减少钻石
            zuanshi.Count -= cost;
            var gim = World.ItemManager;
            datas.ChangesItem.AddToChanges(zuanshi.ParentId ?? zuanshi.OwnerId.Value, zuanshi);
            //修改冷却时间
            fcp.LastValue = fcp.MaxValue;
            fcp.LastDateTime = DateTime.UtcNow;
            datas.ChangesItem.AddToChanges(gameItem.ParentId ?? gameItem.OwnerId.Value, gameItem);
        }

        #endregion 孵化相关

        #region 合成相关

        /// <summary>
        /// 资质合成。
        /// </summary>
        /// <param name="datas"></param>
        public void Hecheng(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameItems.Count == 2, $"物品数量错误。")) return;
            var lockAtk = datas.Lookup(datas.GameItems, ProjectConstant.LockAtkSlotId);
            if (lockAtk is null) return;
            var lockMhp = datas.Lookup(datas.GameItems, ProjectConstant.LockMhpSlotId);
            if (lockMhp is null) return;
            var lockQlt = datas.Lookup(datas.GameItems, ProjectConstant.LockQltSlotId);
            if (lockQlt is null) return;
            var gameItem = datas.GameItems.FirstOrDefault(c => c.Parent.TemplateId == ProjectConstant.ZuojiBagSlotId);
            if (!datas.Verify(null != gameItem, "没有坐骑。")) return;
            var gameItem2 = datas.GameItems.FirstOrDefault(c => c.Parent.TemplateId == ProjectConstant.ShoulanSlotId);
            if (!datas.Verify(null != gameItem2, "没有野兽。")) return;
            //攻击资质
            if (lockAtk.Children.Count <= 0)
            {
                var rnd1 = VWorld.GetRandomNumber(0, 1);
                var rnd2 = (decimal)(rnd1 switch
                {
                    _ when rnd1 < 0.15 => VWorld.GetRandomNumber(0, 0.2),
                    _ when rnd1 >= 0.15 && rnd1 < 0.85 => VWorld.GetRandomNumber(0.25, 0.75),
                    _ => VWorld.GetRandomNumber(0.75, 1),
                });
                var ne = gameItem.GetDecimalOrDefault("neatk") * (1 - rnd2) + gameItem2.GetDecimalOrDefault("neatk") * rnd2;
                ne = Math.Round(ne, MidpointRounding.AwayFromZero);
                gameItem.SetPropertyValue("neatk", ne);
            }
            else
            {
                var lockItem = lockAtk.Children.First();
                datas.ChangesItem.AddToRemoves(lockItem.ContainerId.Value, lockItem.Id);
                lockAtk.Children.Remove(lockItem);
                datas.GameChar.GameUser.DbContext.Remove(lockItem);
            }
            //血量资质
            if (lockMhp.Children.Count <= 0)
            {
                var rnd1 = VWorld.GetRandomNumber(0, 1);
                var rnd2 = (decimal)(rnd1 switch
                {
                    _ when rnd1 < 0.15 => VWorld.GetRandomNumber(0, 0.2),
                    _ when rnd1 >= 0.15 && rnd1 < 0.85 => VWorld.GetRandomNumber(0.25, 0.75),
                    _ => VWorld.GetRandomNumber(0.75, 1),
                });
                var ne = gameItem.GetDecimalOrDefault("nemhp") * (1 - rnd2) + gameItem2.GetDecimalOrDefault("nemhp") * rnd2;
                ne = Math.Round(ne, MidpointRounding.AwayFromZero);
                gameItem.SetPropertyValue("nemhp", ne);
            }
            else
            {
                var lockItem = lockMhp.Children.First();
                datas.ChangesItem.AddToRemoves(lockItem.ContainerId.Value, lockItem.Id);
                lockMhp.Children.Remove(lockItem);
                datas.GameChar.GameUser.DbContext.Remove(lockItem);
            }
            //质量资质
            if (lockQlt.Children.Count <= 0)
            {
                var rnd1 = VWorld.GetRandomNumber(0, 1);
                var rnd2 = (decimal)(rnd1 switch
                {
                    _ when rnd1 < 0.15 => VWorld.GetRandomNumber(0, 0.2),
                    _ when rnd1 >= 0.15 && rnd1 < 0.85 => VWorld.GetRandomNumber(0.25, 0.75),
                    _ => VWorld.GetRandomNumber(0.75, 1),
                });
                var ne = gameItem.GetDecimalOrDefault("neqlt") * (1 - rnd2) + gameItem2.GetDecimalOrDefault("neqlt") * rnd2;
                ne = Math.Round(ne, MidpointRounding.AwayFromZero);
                gameItem.SetPropertyValue("neqlt", ne);
            }
            else
            {
                var lockItem = lockQlt.Children.First();
                datas.ChangesItem.AddToRemoves(lockItem.ContainerId.Value, lockItem.Id);
                lockQlt.Children.Remove(lockItem);
                datas.GameChar.GameUser.DbContext.Remove(lockItem);
            }
            datas.ChangesItem.AddToRemoves(gameItem2.ContainerId.Value, gameItem2.Id);
            gameItem2.Parent.Children.Remove(gameItem2);
            datas.GameChar.GameUser.DbContext.Remove(gameItem2);
            datas.ChangesItem.AddToChanges(gameItem.ContainerId.Value, gameItem);
        }
        #endregion 合成相关

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
        static public bool Verify(this ApplyBlueprintDatas obj, bool succ, string errorMessage, params Guid[] errorItemTIds)
        {
            if (!succ)
            {
                obj.ErrorItemTIds.AddRange(errorItemTIds);
                obj.DebugMessage = errorMessage;
                obj.HasError = true;
            }
            return succ;
        }

        /// <summary>
        /// 在指定的集合中寻找指定模板Id 或和 物品Id的物品。
        /// 无法找到时，自动填写<see cref="ApplyBlueprintDatas"/>中数据。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="parent"></param>
        /// <param name="templateId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        static public GameItem Lookup(this ApplyBlueprintDatas obj, IEnumerable<GameItem> parent, Guid? templateId, Guid? id = null)
        {
            IEnumerable<GameItem> resultColl;
            if (templateId.HasValue)    //若需限定模板Id
            {
                var tid = templateId.Value;
                resultColl = parent.Where(c => c.TemplateId == tid);
            }
            else
                resultColl = parent;
            GameItem result;
            if (id.HasValue) //若要限定Id
            {
                var idMe = id.Value;
                result = resultColl.FirstOrDefault(c => c.Id == idMe);
            }
            else
                result = resultColl.FirstOrDefault();
            if (result is null)  //若没有找到
            {
                obj.DebugMessage = $"无法找到物品。TId={templateId},Id={id}";
                obj.HasError = true;
                if (templateId.HasValue)
                    obj.ErrorItemTIds.Add(templateId.Value);
            }
            return result;
        }
    }

}
