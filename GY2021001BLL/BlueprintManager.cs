using GY2021001DAL;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
            try
            {
                succ = Materials.OrderBy(c => c.Template.PropertiesChanges)   //TO DO
                    .All(c => c.Apply(datas));
            }
            catch (Exception)
            {
                //TO DO
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
            if (Template.IsNew)
            {
                var setTidExpr = (Template.PropertiesChangesExpression as BlockGExpression).Expressions.OfType<BinaryGExpression>().FirstOrDefault(c =>
                {
                    if (c.Left is ReferenceGExpression refExpr && refExpr.Name == "tid" && refExpr.ObjectId == Template.Id.ToString())    //若是设置此条目的模板
                        return true;
                    return false;
                });
                if (setTidExpr == null)
                {
                    datas.DebugMessage = "未能找到新建物品设置模板Id的表达式。";
                    datas.HasError = true;
                    return false;
                }
                if (!setTidExpr.Right.TryGetValue(env, out var tidObj) || !OwHelper.TryGetGuid(tidObj, out var tid))
                {
                    datas.DebugMessage = "未能找到新建物品的模板Id。";
                    datas.HasError = true;
                    return false;
                }
                gim = Parent.Parent.Service.GetRequiredService<GameItemManager>();
                gameItem = gim.CreateGameItem(tid);
                var keyName = Template.Id.ToString();
                GameExpressionBase expr;
                if (env.Variables.TryGetValue(keyName, out expr))   //若已经存在该变量
                    expr.SetValue(env, gameItem);
                else
                {
                    env.Variables[keyName] = new ConstGExpression(gameItem);
                }
            }
            else if (!env.Variables.TryGetValue(Template.Id.ToString(), out var expr) || !expr.TryGetValue(env, out var obj) || !(obj is GameItem))
                return false || Template.AllowEmpty;
            else
                gameItem = obj as GameItem;
            //修改数量
            if (!Template.CountProbExpression.TryGetValue(env, out var countPropObj) || !OwHelper.TryGetDecimal(countPropObj, out var prob)) //若无法获取概率
                return false;
            var world = Parent.Parent.Service.GetRequiredService<VWorld>();
            gim = Parent.Parent.Service.GetService<GameItemManager>();
            var ci = new ChangesItem()
            {
                ContainerId = gameItem.ParentId ?? gameItem.OwnerId.Value,
            };
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
                ci.Changes.Add(gameItem);
            else //若没有剩余
                ci.Removes.Add(gameItem.Id);
            datas.ChangesItem.Add(ci);
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

                var data = new BlueprintData(Service, datas.Blueprint);
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

                ChangesItem.Reduce(datas.ChangesItem);    //压缩变化数据

                World.CharManager.NotifyChange(gu);
            }
            finally
            {
                if (null != tmpList)
                    World.ObjectPoolListGameItem.Return(tmpList);
                World.CharManager.Unlock(gu, true);
            }
        }


    }

}
