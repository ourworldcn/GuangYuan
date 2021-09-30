using Game.Social;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Game;
using OW.Game.Expression;
using OW.Game.Item;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GuangYuan.GY001.BLL
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
            if (!(obj is GameItem gameItem))
            {
                return defaultValue;
            }

            return _Manager.GetPropertyValue(gameItem, propertyName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool SetValue(object obj, string propertyName, object val)
        {
            if (obj is GameItem gameItem)
                return _Manager.SetPropertyValue(gameItem, propertyName, val);
            else
                return false;

        }
    }

    public class BlueprintData
    {
        private IServiceProvider _Service;
        private readonly BlueprintTemplate _Template;

        public IServiceProvider Service { get => _Service; set => _Service = value; }

        public BlueprintTemplate Template => _Template;

        private readonly List<FormulaData> _Formulas;
        public List<FormulaData> Formulas { get => _Formulas; }

        public BlueprintData(IServiceProvider service, BlueprintTemplate template)
        {
            _Service = service;
            _Template = template;
            _Formulas = template.FormulaTemplates.Select(c => new FormulaData(c, this)).ToList();
        }

        public void Apply(ApplyBlueprintDatas datas)
        {
            VWorld world = Service.GetRequiredService<VWorld>();
            FormulaData[] formus = Formulas.OrderBy(c => c.Template.OrderNumber).ToArray();

            foreach (FormulaData item in formus)    //执行所有公式
            {
                if (!item.IsMatched) //若不可用
                {
                    continue;
                }

                if (!item.Template.ProbExpression.TryGetValue(item.RuntimeEnvironment, out object probObj) || !OwHelper.TryGetDecimal(probObj, out decimal prob))  //若无法得到命中概率
                {
                    continue;
                }

                if (!world.IsHit((double)prob)) //若未命中
                {
                    continue;
                }

                if (item.Apply(datas))   //若执行蓝图成功
                {
                    datas.FormulaIds.Add(item.Template.Id);
                    if (!item.Template.IsContinue)  //若无需继续
                    {
                        break;
                    }
                }
            }
            ChangeItem.Reduce(datas.ChangesItem);
            return;
        }

        public void Match(ApplyBlueprintDatas datas)
        {
            foreach (FormulaData item in Formulas)
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
        private GameExpressionRuntimeEnvironment _RuntimeEnvironment;

        /// <summary>
        /// 脚本的运行时环境。每个公式独立。
        /// </summary>
        public GameExpressionRuntimeEnvironment RuntimeEnvironment => _RuntimeEnvironment ??= new GameExpressionRuntimeEnvironment(Template?.CompileEnvironment);

        private readonly List<MaterialData> _Materials;
        public List<MaterialData> Materials { get => _Materials; }

        public bool IsMatched { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public bool Match(ApplyBlueprintDatas datas)
        {
            List<MaterialData> tmpList = Materials.Where(c => !c.Template.IsNew) //排除新建物品
                .ToList();
            List<GameItem> coll = datas.GameItems.Concat(OwHelper.GetAllSubItemsOfTree(datas.GameChar.GameItems, c => c.Children)).ToList();    //要遍历的所有物品，确保指定物品最先被匹配
            RuntimeEnvironment.StartScope();
            try
            {
                while (tmpList.Count > 0)   //当还有未匹配的原料时
                {
                    bool succ = false;
                    for (int i = tmpList.Count - 1; i >= 0; i--)
                    {
                        MaterialData item = tmpList[i];
                        if (item.Match(coll, out GameItem gameItem))
                        {
                            tmpList.RemoveAt(i);
                            while (coll.Remove(gameItem))
                            {
                                ;
                            }

                            succ = true;
                        }
                    }
                    if (!succ)   //若本轮没有任何一个原料匹配上
                    {
                        break;
                    }
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
                    GetIncrement(out decimal min, out decimal max);
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
                {
                    Debug.Fail($"无法获取增量发生的概率。原料Id={Template.Id}({Template.Remark})");
                }

                return result;
            }
        }

        private decimal? _Min, _Max;
        /// <summary>
        /// 最小和最大的增量值。仅计算一次，后续调用返回缓存，避免多次计算随机数。
        /// </summary>
        public bool GetIncrement(out decimal min, out decimal max)
        {
            if (_Min is null)
            {
                Debug.Assert(_Max is null);
                GameExpressionRuntimeEnvironment env = Parent.RuntimeEnvironment;
                bool succ = Template.TryGetLowerBound(env, out decimal lower);
                Debug.Assert(succ);
                succ = Template.TryGetUpperBound(env, out decimal upper);
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
            IEnumerable<GameItem> coll = gameItems;
            GameExpressionRuntimeEnvironment env = Parent.RuntimeEnvironment;
            env.StartScope();
            try
            {
                ConstGExpression constExpr;
                string id = Template.Id.ToString();
                if (env.Variables.TryGetValue(id, out GameExpressionBase oRxpr))
                {
                    Debug.Assert(oRxpr is ConstGExpression);
                    constExpr = oRxpr as ConstGExpression;
                }
                else
                {
                    env.Variables[id] = constExpr = new ConstGExpression();
                }

                foreach (GameItem item in coll)
                {
                    bool _ = constExpr.SetValue(env, item);   //设置对象
                    Debug.Assert(_);
                    if (!Template.ConditionalExpression.TryGetValue(env, out object matchObj) || !(matchObj is bool isMatth) || !isMatth) //若不符合条件
                    {
                        continue;
                    }

                    if (OwHelper.TryGetDecimal(Template.CountProbExpression.GetValueOrDefault(env, 0), out decimal countProp) && countProp > 0) //若概率可能大于0 TO DO
                    {
                        //校验数量
                        if (OwHelper.TryGetDecimal(Template.CountLowerBoundExpression.GetValueOrDefault(env, 0), out decimal lower))
                        {
                            ;
                        }

                        if (OwHelper.TryGetDecimal(Template.CountUpperBoundExpression.GetValueOrDefault(env, 0), out decimal upper))
                        {
                            ;
                        }

                        if (Math.Min(lower, upper) + (item.Count ?? 0) < 0) //若数量不够
                        {
                            continue;
                        }
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
            GameExpressionRuntimeEnvironment env = Parent.RuntimeEnvironment;
            GameItem gameItem;
            GameItemManager gim;
            //获取该原料对象
            if (!env.Variables.TryGetValue(Template.Id.ToString(), out GameExpressionBase expr) || !expr.TryGetValue(env, out object obj) || !(obj is GameItem))
            {
                return false || Template.AllowEmpty;
            }
            else
            {
                gameItem = obj as GameItem;
            }
            //修改数量
            if (!Template.CountProbExpression.TryGetValue(env, out object countPropObj) || !OwHelper.TryGetDecimal(countPropObj, out _)) //若无法获取概率
            {
                return false;
            }

            VWorld world = Parent.Parent.Service.GetRequiredService<VWorld>();
            gim = Parent.Parent.Service.GetService<GameItemManager>();
            if (world.IsHit((double)CountIncrementProb)) //若需要增量
            {
                decimal inc = CountIncrement;
                if (gameItem.Count + inc < 0)
                {
                    return false;
                }

                var count = inc + gameItem.Count.Value;
                bool _ = gim.SetPropertyValue(gameItem, "count", count);
                Debug.Assert(_);
            }
            if (!Template.PropertiesChangesExpression.TryGetValue(env, out _))
            {
                return false;
            }

            if (gameItem.Count.Value > 0) //若有剩余
            {
                if (Template.IsNew)
                {
                    datas.ChangesItem.AddToAdds(gameItem.ParentId ?? gameItem.OwnerId.Value, gameItem);
                }
                else
                {
                    datas.ChangesItem.AddToChanges(gameItem.ParentId ?? gameItem.OwnerId.Value, gameItem);
                }
            }
            else //若没有剩余
            {
                datas.ChangesItem.AddToRemoves(gameItem.ParentId ?? gameItem.OwnerId.Value, gameItem.Id);
            }

            return true;
        }

        private string GetDebuggerDisplay()
        {
            string str1 = string.IsNullOrWhiteSpace(Template?.DisplayName) ? Template?.Remark : Template?.DisplayName;
            return $"{{{str1},Matched={GetMatched()}}}";
        }

        /// <summary>
        /// 获取改原料对象当前匹配的对象。
        /// </summary>
        /// <returns></returns>
        public object GetMatched()
        {
            if (null == Template)
            {
                return null;
            }

            GameExpressionRuntimeEnvironment env = Parent.RuntimeEnvironment;
            if (!env.TryGetVariableValue(Template.Id.ToString(), out object result))
            {
                return null;
            }

            return result;
        }

        /// <summary>
        /// 返回设置模板Id的表达式。
        /// </summary>
        /// <returns></returns>
        public BinaryGExpression GetSetTIdExpr()
        {
            BinaryGExpression setTidExpr = (Template.PropertiesChangesExpression as BlockGExpression).Expressions.OfType<BinaryGExpression>().FirstOrDefault(c =>
            {
                if (c.Left is ReferenceGExpression refExpr && refExpr.Name == "tid" && refExpr.ObjectId == Template.Id.ToString() && c.Operator == "=")    //若是设置此条目的模板
                {
                    return true;
                }

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
            GameExpressionRuntimeEnvironment env = Parent.RuntimeEnvironment;

            BinaryGExpression setTidExpr = GetSetTIdExpr();
            if (null == setTidExpr || !setTidExpr.Right.TryGetValue(env, out object tidObj) || !OwHelper.TryGetGuid(tidObj, out Guid tid))
            {
                //datas.ErrorMessage = "未能找到新建物品的模板Id。";
                //datas.HasError = true;
                return false;
            }
            GameItemManager gim = Parent.Parent.Service.GetRequiredService<GameItemManager>();
            GameItem gameItem = gim.CreateGameItem(tid);

            string keyName = Template.Id.ToString();
            if (env.Variables.TryGetValue(keyName, out GameExpressionBase expr) && expr is ConstGExpression)   //若已经存在该变量
            {
                return expr.SetValue(env, gameItem);
            }
            else
            {
                env.Variables[keyName] = new ConstGExpression(gameItem);
            }

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
        public List<ChangeItem> ChangesItem { get; } = new List<ChangeItem>();

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

        private List<Guid> _ErrorItemTIds;

        /// <summary>
        /// 获取或设置，出错虚拟物品的模板Id。具体意义根据不同蓝图区别。
        /// </summary>
        public List<Guid> ErrorItemTIds => _ErrorItemTIds ??= new List<Guid>();

        private List<Guid> _MailIds;

        /// <summary>
        /// 执行蓝图导致发送邮件的Id集合。
        /// </summary>
        public List<Guid> MailIds => _MailIds ??= new List<Guid>();

        /// <summary>
        /// 错误码。
        /// </summary>
        public int ErrorCode { get; internal set; }
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
            {
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
        }

        private Task _InitializeTask;

        private DbContext _DbContext;

        public DbContext Context { get => _DbContext ??= World.CreateNewTemplateDbContext(); }

        private Dictionary<Guid, BlueprintTemplate> _Id2BlueprintTemplate;

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
            string idStr = datas.Blueprint.Id.ToString("D").ToLower();
            try
            {
                switch (idStr)
                {
                    case "8b4ac76c-d8cc-4300-95ca-668350149821":    //若是孵化
                        Fuhua(datas);
                        succ = true;
                        break;
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
                        UpgradeInHomeland(datas);
                        succ = true;
                        break;
                    case "06bdaa5c-3d88-4279-9826-8f5a554ab588":    //加速主控室/玉米地/树林/炮台/旗子/陷阱/捕兽竿升级
                        HastenOnHomeland(datas);
                        succ = true;
                        break;
                    case "8b26f520-fbf3-4979-831c-398a0150b3da":    // 取得玉米/ 木材放入仓库
                        Harvest(datas);
                        succ = true;
                        break;
                    case "f9262cb1-7357-4027-b742-d0a82f3ad4c1":    //合成
                        Hecheng(datas);
                        succ = true;
                        break;
                    case "dd5095f8-929f-45a5-a86c-4a1792e9d9c8":    //购买Pve次数
                        BuyPveCount(datas);
                        succ = true;
                        break;
                    case "6a0c5697-4228-4ec9-a69e-28d61bd52b32":    //坐骑等级提升
                        LevelUp(datas);
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

        #region 通用功能

        /// <summary>
        /// 通用的升级函数。
        /// </summary>
        /// <param name="datas"></param>
        public void LevelUp(ApplyBlueprintDatas datas)
        {
            var gim = World.ItemManager;
            if (datas.Count <= 0)
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = "知道次数应大于0";
                return;
            }
            for (int i = 0; i < datas.Count; i++)
            {
                var gi = datas.GameItems[0];
                var luDatas = new LevelUpDatas(World, datas.GameChar)
                {
                    GameItem = datas.GameItems[0],
                };
                if (gim.IsMounts(gi))
                    luDatas.GameItem = gim.GetBody(gi);
                else
                    luDatas.GameItem = gi;
                var cost = luDatas.GetCost();
                if (cost is null)   //若不可以升级
                {
                    return;
                }
                var succ = luDatas.Deplete(cost);
                if (!succ)  //若资源不足
                {
                    return;
                }
                //升级
                var oldLv = luDatas.GameItem.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);
                gim.SetPropertyValue(luDatas.GameItem, ProjectConstant.LevelPropertyName, oldLv + 1);
                datas.ChangesItem.AddToChanges(cost.Select(c => c.Item1).ToArray());
                datas.SuccCount = i + 1;
                datas.ChangesItem.AddToChanges(gi);
            }
        }

        #endregion 通用功能
        #region 家园相关

        /// <summary>
        /// 收获家园中玉米，木材等资源放入仓库。
        /// </summary>
        /// <param name="datas"></param>
        private void Harvest(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameItems.Count == 1, "只能升级一个对象。"))
            {
                return;
            }

            GameItem gameItem = datas.GameItems[0];  //收取的容器
            GameChar gameChar = datas.GameChar;
            GameItem hl = datas.Lookup(gameChar.GameItems, ProjectConstant.HomelandSlotId);    //家园
            if (null == hl)
            {
                return;
            }

            GameItem mainBase = datas.Lookup(hl.Children, ProjectConstant.MainBaseSlotId); //主基地
            if (mainBase is null)
            {
                return;
            }

            GameItem src = datas.Lookup(mainBase.Children, gameItem.TemplateId); //收获物
            if (src is null)
            {
                return;
            }

            GameItem destItem = gameItem.TemplateId switch // //目标对象
            {
                _ when gameItem.TemplateId == ProjectConstant.MucaishuTId => gameChar.GetMucai(),   //木材
                _ when gameItem.TemplateId == ProjectConstant.YumitianTId => gameChar.GetJinbi(), //玉米
                _ => null,
            };

            if (!src.TryGetPropertyValueWithFcp("Count", DateTime.UtcNow, true, out object countObj, out DateTime dt) || !OwHelper.TryGetDecimal(countObj, out decimal count))
            {
                datas.DebugMessage = "未知原因无法获取收获数量。";
                datas.HasError = true;
                return;
            }
            decimal stc = destItem.GetNumberOfStackRemainder();  //剩余可堆叠数
            count = Math.Min(count, stc);   //实际移走数量
            if (src.Name2FastChangingProperty.TryGetValue("Count", out FastChangingProperty fcp))    //若有快速变化属性
            {
                fcp.SetLastValue(fcp.LastValue - count, ref dt);
            }
            else
            {
                src.Count -= count;
            }

            destItem.Count += count;
            datas.ChangesItem.AddToChanges(src.ContainerId.Value, src);
            datas.ChangesItem.AddToChanges(destItem.ContainerId.Value, destItem);
        }

        /// <summary>
        /// 家园内部相关物品升级。
        /// </summary>
        /// <param name="datas"></param>
        private void UpgradeInHomeland(ApplyBlueprintDatas datas)
        {
            DateTime dt = DateTime.UtcNow;  //尽早确定开始时间
            Guid hlTid = ProjectConstant.HomelandSlotId; //家园Id
            Guid jianzhuBagTid = new Guid("{312612a5-30dd-4e0a-a71d-5074397428fb}");   //建筑背包tid
            GameItem hl = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == hlTid);   //家园对象
            if (!datas.Verify(datas.GameItems.Count == 1, "只能升级一个对象。"))
            {
                return;
            }

            GameItem gameItem = hl.AllChildren.FirstOrDefault(c => c.Id == datas.GameItems[0].Id);   //要升级的物品
            if (!datas.Verify(gameItem.ParentId.HasValue, "找不到父容器Id。"))
            {
                return;
            }

            GameItemManager gim = World.ItemManager;
            GameItemTemplate template = gim.GetTemplateFromeId(gameItem.TemplateId); //物品的模板对象
            if (!datas.Verify(OwHelper.TryGetDecimal(gameItem.GetPropertyValueOrDefault(ProjectConstant.LevelPropertyName, 0m), out decimal lvDec), "级别属性类型错误。"))
            {
                return;
            }

            int lv = (int)lvDec;    //原等级

            #region 等级校验
            if (!datas.Verify(template.GetMaxLevel(template.SequencePropertyNames.FirstOrDefault()) > lv, "已达最大等级", gameItem.TemplateId))  //若已达最大等级
            {
                return;
            }

            if (template.TryGetPropertyValue("mbnlv", out object mbnlvObj) && OwHelper.TryGetDecimal(mbnlvObj, out decimal mbnlv))    //若需要根据主控室等级限定升级
            {
                GameItem mb = hl.AllChildren.FirstOrDefault(c => c.TemplateId == ProjectConstant.HomelandSlotId);    //主控室
                decimal mbLv = mb.GetDecimalOrDefault(GameThingTemplateBase.LevelPrefix, 0m); //当前主控室等级
                if (!datas.Verify(mbLv >= mbnlv, "主控室等级过低，不能升级指定物品。", gameItem.TemplateId))
                {
                    return;
                }
            }
            #endregion 等级校验

            #region 所需资源校验

            GameItem gold = null;
            if (gameItem.TryGetDecimalPropertyValue("lug", out decimal lug)) //若需要金子
            {
                gold = datas.GameChar.GetJinbi();
                if (!datas.Verify(gold.Count >= lug, $"需要{lug}金币，目前只有{gold.Count}金币。", gold.TemplateId))
                {
                    return;
                }
            }
            GameItem wood = null;
            if (gameItem.TryGetDecimalPropertyValue("luw", out decimal luw)) //若需要木头
            {
                wood = datas.GameChar.GetMucai();
                if (!datas.Verify(wood.Count >= luw, $"需要{luw}木材，目前只有{wood.Count}木材。", wood.TemplateId))
                {
                    return;
                }
            }
            #endregion 所需资源校验

            #region 冷却相关
            FastChangingProperty fcp = FastChangingPropertyExtensions.FromDictionary(gameItem.Properties, "upgradecd");
            if (fcp != null)
            {
                if (!datas.Verify(fcp.IsComplate, "虚拟物品还在升级中", gameItem.TemplateId))
                {
                    return;
                }
            }

            decimal time = gameItem.GetDecimalOrDefault("lut", -2); //冷却的秒数
            GameItem worker = datas.Lookup(hl.Children, ProjectConstant.WorkerOfHomelandTId);
            if (worker is null)
            {
                return;
            }
            #endregion 冷却相关

            #region 修改属性

            if (time > 0) //若需要冷却
            {
                if (!datas.Verify(worker.GetNumberOfStackRemainder() > 0, "所有建筑工人都在忙", worker.TemplateId))
                {
                    return;
                }
                FastChangingProperty fcpObj = new FastChangingProperty(TimeSpan.FromSeconds(1), 1, time, 0, dt)
                {
                    Name = ProjectConstant.UpgradeTimeName,
                    Tag = ValueTuple.Create(datas.GameChar.Id, gameItem.Id),
                };
                Debug.WriteLine($"服务器认为升级开始时间为{dt}");
                gameItem.Name2FastChangingProperty[ProjectConstant.UpgradeTimeName] = fcpObj;
                fcpObj.Completed += UpgradeCompleted;
                //计算可能的完成时间
                DateTime dtComplate = fcpObj.ComputeComplateDateTime();   //预计完成时间
                TimeSpan ts = dtComplate - DateTime.UtcNow + TimeSpan.FromSeconds(0.02);
                Timer timer = new Timer(UpgradeComplateCallback, ValueTuple.Create(datas.GameChar.Id, gameItem.Id),
                    ts, Timeout.InfiniteTimeSpan);
                worker.Count++;
                datas.ChangesItem.AddToChanges(worker.ContainerId.Value, worker);
            }
            else //立即完成
            {
                gim.SetPropertyValue(gameItem, ProjectConstant.LevelPropertyName, lv + 1);    //设置新等级
            }
            if (null != wood)
            {
                wood.Count -= luw;
                datas.ChangesItem.AddToChanges(wood.ContainerId.Value, wood);
            }
            if (null != gold)
            {
                gold.Count -= lug;
                datas.ChangesItem.AddToChanges(gold.ContainerId.Value, gold);
            }
            datas.ChangesItem.AddToChanges(gameItem.ParentId.Value, gameItem);
            #endregion 修改属性
            return;
        }

        /// <summary>
        /// 加速完成家园内的升级项目。
        /// </summary>
        /// <param name="datas"></param>
        private void HastenOnHomeland(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameItems.Count == 1, "只能加速一个物品"))
            {
                return;
            }

            GameItem gameItem = datas.GameItems[0];  //加速的物品
            GameItem hl = datas.Lookup(datas.GameChar.GameItems, ProjectConstant.HomelandSlotId);
            if (hl is null) return;

            GameItem worker = datas.Lookup(hl.Children, ProjectConstant.WorkerOfHomelandTId);
            if (worker is null) return;

            if (!datas.Verify(worker.Count.HasValue && worker.Count > 0, "没有在升级的物品")) return;

            if (!datas.Verify(gameItem.Name2FastChangingProperty.TryGetValue("upgradecd", out FastChangingProperty fcp), "物品未进行升级"))
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
                DateTime dtComplate = fcp.ComputeComplateDateTime();
                decimal tm = (decimal)(dtComplate - dt).TotalMinutes;

                decimal cost = tm switch //需要花费的钻石
                {
                    _ when tm <= 5m => 0,
                    _ => Math.Ceiling(tm - 5),
                };
                if (cost > 0)   //若需要钻石
                {
                    GameItem dim = datas.Lookup(datas.GameChar.GetCurrencyBag().Children, ProjectConstant.ZuanshiId);    //钻石
                    if (dim is null) return;

                    if (!datas.Verify(dim.Count >= cost, $"需要{cost}钻石,但只有{dim.Count}钻石。"))
                    {
                        datas.ErrorItemTIds.Add(dim.TemplateId);
                        return;
                    }
                    dim.Count -= cost;
                    datas.ChangesItem.AddToChanges(dim.ContainerId.Value, dim);
                }
                //工人
                if (worker.Count > 0)
                    worker.Count--;
                datas.ChangesItem.AddToChanges(worker.ContainerId.Value, worker);
                //修改快速变化属性
                fcp.LastDateTime = dt;
                fcp.LastValue = fcp.MaxValue;
                fcp.InvokeOnCompleted(new CompletedEventArgs(dt));
                //追加新建物品
                datas.ChangesItem.AddRange(LastChangesItems); //追加新建物品
                LastChangesItems.Clear();
                gameItem.RemoveFastChangingProperty("upgradecd");
                datas.ChangesItem.AddToChanges(gameItem.ContainerId.Value, gameItem);
            }

            return;
        }

        /// <summary>
        /// 购买pve次数。
        /// </summary>
        /// <param name="datas">钻石和塔防次数对象会在变化中返回。</param>
        private void BuyPveCount(ApplyBlueprintDatas datas)
        {
            //5，5，10，10，30,0
            //ltlv 记载最后一次升级的时间
            var gc = datas.GameChar;    //角色对象
            var td = datas.Lookup(gc.GetCurrencyBag().Children, ProjectConstant.PveTCounterTId);
            if (td is null) //若无塔防对象
                return;
            if (!datas.Verify(td.Name2FastChangingProperty.TryGetValue("Count", out _), "找不到自动恢复属性。"))
                return;
            var lv = td.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);
            DateTime dt = DateTime.UtcNow;  //当前时间
            if (td.TryGetPropertyValue("ltlv", out var ltlvObj) && DateTime.TryParse(ltlvObj as string, out var ltlv))  //若找到上次升级时间属性
            {
                if (dt.Date <= ltlv.Date && !datas.Verify(td.Template.GetMaxLevel("lud") > lv + 1, "已经用尽全部购买次数。"))
                    return;
            }
            else
            {
                ltlv = dt;
                lv = 0;
            }
            var diam = datas.Lookup(gc.GetCurrencyBag().Children, ProjectConstant.ZuanshiId);//钻石
            if (diam is null)   //若没有钻石
                return;
            if (!datas.Verify(td.TryGetDecimalPropertyValue("lud", out var lud), "没有找到升级所需钻石数量。"))
                return;
            if (!datas.Verify(lud <= diam.Count, "钻石不足")) return;
            //修改数据
            var gim = World.ItemManager;
            gim.SetPropertyValue(td, ProjectConstant.LevelPropertyName, lv + 1);    //变更购买价格
            td.SetPropertyValue("ltlv", ltlv.ToString());  //记录购时间
            datas.ChangesItem.AddToChanges(td.ContainerId.Value, td);
            td.Name2FastChangingProperty["Count"].LastValue++;
            diam.Count -= lud;  //改钻石
            datas.ChangesItem.AddToChanges(diam);
            datas.ChangesItem.AddToChanges(td);
        }
        #endregion 家园相关

        [ContextStatic]
        private static List<ChangeItem> _LastChangesItems;

        /// <summary>
        /// 暂存自然cd得到的物品。
        /// </summary>
        public static List<ChangeItem> LastChangesItems => _LastChangesItems ??= new List<ChangeItem>();

        /// <summary>
        /// 某个物品升级结束。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpgradeCompleted(object sender, CompletedEventArgs e)
        {
            if (!(sender is FastChangingProperty fcp) || fcp.Name != "upgradecd")
            {
                return; //忽略
            }

            if (!(fcp.Tag is ValueTuple<Guid, Guid> ids))
            {
                return;
            }

            GameCharManager cm = World.CharManager;
            GameItemManager gim = World.ItemManager;
            GameChar gameChar = cm.GetCharFromId(ids.Item1);
            //gameChar ??= gim.GetChar(ids.Item2);
            if (gameChar is null)   //若用户已经下线
            {
                return;
            }

            if (!cm.Lock(gameChar.GameUser))   //若用户已经下线
            {
                return;
            }

            try
            {
                GameItem gameItem = gameChar.AllChildren.FirstOrDefault(c => c.Id == ids.Item2);
                if (gameItem is null)
                {
                    return;
                }

                int lv = (int)gameItem.GetDecimalOrDefault(ProjectConstant.LevelPropertyName, 0m);  //原等级
                gim.SetPropertyValue(gameItem, ProjectConstant.LevelPropertyName, lv + 1);    //设置新等级
                if (gameItem.TemplateId == ProjectConstant.MainControlRoomSlotId) //如果是主控室升级
                {
                    IEnumerable<MainbaseUpgradePrv> coll = MainbaseUpgradePrv.Alls.Where(c => c.Level == lv + 1);
                    List<GameItem> addItems = new List<GameItem>();
                    foreach (MainbaseUpgradePrv item in coll)
                    {
                        GameItem parent = gameChar.AllChildren.FirstOrDefault(c => c.TemplateId == item.ParentTId);
                        if (item.PrvTId.HasValue)    //若送物品
                        {
                            GameItem tmp = gim.CreateGameItem(item.PrvTId.Value);
                            gim.AddItem(tmp, parent, null, LastChangesItems);
                        }
                        if (item.Genus.HasValue)   //若送地块
                        {
                            int styleNumber = gameChar.GetCurrentFenggeNumber();   //激活的风格号
                            var subItem = World.ItemTemplateManager.GetTemplateByNumberAndIndex(styleNumber, item.Genus.Value % 100);
                            GameItem tmp = gim.CreateGameItem(subItem);
                            gim.AddItem(tmp, gameChar.GetHomeland(), null, LastChangesItems);
                        }
                    }
                }
                LastChangesItems.AddToChanges(gameItem.ContainerId.Value, gameItem);
            }
            catch (Exception)
            {

            }
            finally
            {
                cm.Unlock(gameChar.GameUser, true);
            }
        }

        /// <summary>
        /// 当升级可能结束时调用。
        /// </summary>
        /// <param name="state">值元组(角色Id,虚拟物品Id)</param>
        private void UpgradeComplateCallback(object state)
        {
            if (!(state is ValueTuple<Guid, Guid> para))
            {
                return;
            }

            GameCharManager cm = World.CharManager;
            GameChar gc = cm.GetCharFromId(para.Item1);
            if (null == gc) //若角色不在线
            {
                return;
            }

            GameUser gu = gc.GameUser;
            if (!cm.Lock(gu))   //若不能锁定
            {
                if (null == cm.GetCharFromId(para.Item1)) //若争用导致已经下线
                {
                    return; //可以等待下次登录时再计算
                }
                else   //TO DO 致命问题，但目前不知道如何才会引发(大概率发生了死锁)，暂无解决方法
                {
                    ILogger<BlueprintManager> logger = Service.GetService<ILogger<BlueprintManager>>();
                    logger?.LogError($"长期无法锁定在线用户，Number={gu.Id}。");
                    return;
                }
            }

            try
            {
                GameItem gameItem = gc.AllChildren.FirstOrDefault(c => c.Id == para.Item2);  //获取结束升级的对象
                if (gameItem == null)  //若已经无效
                {
                    return;
                }

                FastChangingProperty fcp = gameItem.Name2FastChangingProperty.GetValueOrDefault("upgradecd");
                if (fcp == null)    //若已经处理完毕
                {
                    return;
                }

                DateTime dtComplate = fcp.ComputeComplateDateTime();   //预期完成时间
                KeyValuePair<string, FastChangingProperty>[] ary = gameItem.Name2FastChangingProperty.Where(c => c.Key != "upgradecd").ToArray();
                foreach (KeyValuePair<string, FastChangingProperty> item in ary)   //先引发所有渐变属性的计算，升级后可能需要重新计算公式
                {
                    DateTime tmp = dtComplate;
                    item.Value.GetCurrentValue(ref tmp);
                }
                DateTime dtTmp = dtComplate;
                fcp.GetCurrentValue(ref dtTmp);
                //var fcpCount = gameItem.Name2FastChangingProperty.GetValueOrDefault("Count");
                if (fcp.IsComplate)  //若已经完成
                {
                    //fcpCount?.GetCurrentValue(ref dtTmp);    //计算升级完成时点的数量，忽略时点回退误差
                    GameItemManager gim = World.ItemManager;
                    var work = gc.GetHomeland().Children.First(c => c.TemplateId == ProjectConstant.WorkerOfHomelandTId);
                    work.Count--;
                    LastChangesItems.AddToChanges(work.ContainerId.Value, work);    //追加工人变化数据
                    gameItem.RemoveFastChangingProperty(fcp.Name);
                }
                gc.ChangesItems.AddRange(LastChangesItems); LastChangesItems.Clear();
                World.MissionManager.ScanAsync(gc);
            }
            finally
            {
                cm.Unlock(gu, true);
                var logger1 = Service.GetRequiredService<ILogger<BlueprintManager>>();
                logger1.LogInformation($"[{DateTime.UtcNow}]Call UpgradeComplateCallback Complated");
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
            GameUser gu = datas.GameChar.GameUser;
            List<GameItem> tmpList = World.ObjectPoolListGameItem.Get();
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
                {
                    return;
                }

                datas.GameItems.Clear();
                datas.GameItems.AddRange(tmpList);
                if (!Dispatch(datas))
                {
                    BlueprintData data = new BlueprintData(Service, datas.Blueprint);
                    for (int i = 0; i < datas.Count; i++)
                    {
                        data.Match(datas);
                        if (!data.Formulas.Any(c => c.IsMatched))  //若已经没有符合条件的公式。
                        {
                            datas.DebugMessage = $"计划制造{datas.Count}次,实际成功{i}次后，原料不足";
                            break;
                        }
                        foreach (FormulaData item in data.Formulas)
                        {
                            if (!item.IsMatched)
                            {
                                continue;
                            }

                            foreach (MaterialData meter in item.Materials)
                            {
                                meter.Template.VariableDeclaration.OfType<ReferenceGExpression>().All(c => c.Cache(item.RuntimeEnvironment));
                            }
                        }
                        data.Apply(datas);
                        datas.SuccCount++;
                    }
                }
                ChangeItem.Reduce(datas.ChangesItem);    //压缩变化数据
                //switch (datas.Blueprint.Id.ToString("D").ToLower())
                //{
                //    case "8b4ac76c-d8cc-4300-95ca-668350149821": //针对孵化蓝图
                //        GameItem tmp = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.FuhuaSlotTId);  //孵化槽
                //        ChangeItem slotFh = datas.ChangesItem.FirstOrDefault(c => c.ContainerId == tmp.Id);    //孵化容器变化数据
                //        if (slotFh == null)
                //        {
                //            break;
                //        }

                //        GameItem gameItem = slotFh.Adds.FirstOrDefault();    //孵化的组合
                //        if (gameItem == null)
                //        {
                //            break;
                //        }

                //        ChangeItem containerMounts = datas.ChangesItem.FirstOrDefault(c => c.ContainerId == gameItem.Id);    //组合容器
                //        Debug.Assert(containerMounts.Adds.Count == 2);
                //        gameItem.Children.AddRange(containerMounts.Adds);
                //        datas.ChangesItem.Remove(containerMounts);
                //        break;
                //    default:
                //        break;
                //}
                World.CharManager.NotifyChange(gu);
                if (!datas.HasError) //若无错
                    World.MissionManager.ScanAsync(datas.GameChar);
            }
            catch (Exception err)
            {
                datas.DebugMessage = err.Message + " @" + err.StackTrace;
                datas.HasError = true;
            }
            finally
            {
                if (null != tmpList)
                {
                    World.ObjectPoolListGameItem.Return(tmpList);
                }

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
            if (!datas.Verify(datas.GameItems.Count > 0, "参数过少。"))
            {
                return;
            }

            GameItem slotFh = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.FuhuaSlotTId); //孵化槽
            if (null == slotFh)
            {
                datas.HasError = true;
                datas.DebugMessage = "找不到孵化槽。";
                return;
            }
            GameItem gameItem = datas.GameItems.Join(slotFh.Children, c => c.Id, c => c.Id, (l, r) => r).FirstOrDefault();    //要取出的物品
            if (null == gameItem)
            {
                datas.HasError = true;
                datas.DebugMessage = "找不到要取出的物品。";
                return;
            }
            if (!datas.Verify(gameItem.Name2FastChangingProperty.TryGetValue("fhcd", out FastChangingProperty fcp), $"孵化物品没有冷却属性。Number = {gameItem.Id}"))
            {
                return;
            }

            if (!datas.Verify(fcp.IsComplate, $"物品没有孵化完成。Number = {gameItem.Id}。")) //若未完成孵化
            {
                return;
            }

            GameItem slotZq = datas.GameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ZuojiBagSlotId);   //坐骑背包
            GameItemManager gim = World.ItemManager;
            Guid headTid = gim.GetHead(gameItem).TemplateId;
            Guid bodyTid = gim.GetBody(gameItem).TemplateId;
            GameItem zq = slotZq.Children.FirstOrDefault(c =>    //找同头同身坐骑
            {
                return c.Children.Any(c2 => c2.TemplateId == headTid) && c.Children.Any(c2 => c2.TemplateId == bodyTid);
            });
            if (null != zq)    //若已经有同种坐骑
            {
                GameItem slotSl = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ShoulanSlotId);
                if (!datas.Verify(null != slotSl, "找不到兽栏。"))
                {
                    return;
                }

                gameItem.Properties["neatk"] = Math.Round(gameItem.GetDecimalOrDefault("neatk"), MidpointRounding.AwayFromZero);
                gameItem.Properties["nemhp"] = Math.Round(gameItem.GetDecimalOrDefault("nemhp"), MidpointRounding.AwayFromZero);
                gameItem.Properties["neqlt"] = Math.Round(gameItem.GetDecimalOrDefault("neqlt"), MidpointRounding.AwayFromZero);
                if (!gim.MoveItem(gameItem, 1, slotSl, datas.ChangesItem))   //若无法放入
                {
                    //发邮件
                    var social = World.SocialManager;
                    var mail = new GameMail()
                    {
                    };
                    mail.Properties["MailTypeId"] = ProjectConstant.孵化补给动物.ToString();
                    social.SendMail(mail, new Guid[] { datas.GameChar.Id }, SocialConstant.FromSystemId,
                        new ValueTuple<GameItem, Guid>[] { (gameItem, ProjectConstant.ShoulanSlotId) });
                }

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
            GameItem fhSlot = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.FuhuaSlotTId);    //孵化槽
            GameItem gameItem = fhSlot.Children.FirstOrDefault(c => c.Id == datas.GameItems[0].Id);  //要加速孵化的物品
            if (!gameItem.Name2FastChangingProperty.TryGetValue("fhcd", out FastChangingProperty fcp))
            {
                datas.HasError = true;
                datas.DebugMessage = "孵化物品没有冷却属性";
                return;
            }
            //计算所需钻石
            GameItem zuanshi = datas.GameChar.GetZuanshi();    //钻石
            DateTime dt = DateTime.UtcNow;
            decimal tm = (fcp.MaxValue - fcp.GetCurrentValue(ref dt)) / 60;
            decimal cost;
            if (tm <= 5)   //若不收费
            {
                cost = 0;
            }
            else
            {
                cost = Math.Ceiling(tm - 5);
            }

            if (!datas.Verify(cost <= zuanshi.Count, $"需要{cost}钻石,但目前仅有{zuanshi.Count}个钻石。"))
            {
                return;
            }
            //减少钻石
            zuanshi.Count -= cost;
            GameItemManager gim = World.ItemManager;
            datas.ChangesItem.AddToChanges(zuanshi.ParentId ?? zuanshi.OwnerId.Value, zuanshi);
            //修改冷却时间
            fcp.LastValue = fcp.MaxValue;
            fcp.LastDateTime = DateTime.UtcNow;
            datas.ChangesItem.AddToChanges(gameItem.ParentId ?? gameItem.OwnerId.Value, gameItem);
        }

        /// <summary>
        /// 孵化坐骑/动物。
        /// </summary>
        /// <param name="datas"></param>
        public void Fuhua(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameItems.Count == 2, "必须指定双亲"))
                return;
            var jinyinTId = new Guid("{ac7d593c-ce82-4642-97a3-14025da633e4}");
            var jiyin = datas.GameChar.GetItemBag().Children.First(c => c.TemplateId == jinyinTId); //基因蛋
            if (!datas.Verify(null != jiyin && jiyin.Count > 0, "没有基因蛋", jinyinTId))
                return;
            var gim = World.ItemManager;
            var fuhuaSlot = datas.GameChar.GetFuhuaSlot();
            var renCout = gim.GetFreeCapacity(fuhuaSlot);
            if (!datas.Verify(renCout > 0, "孵化槽已经满", fuhuaSlot.TemplateId))
                return;
            var parent1 = datas.GameItems[0];
            var parent2 = datas.GameItems[1];
            var child = FuhuaCore(datas.GameChar, parent1, parent2);
            child.Name2FastChangingProperty.Add("fhcd", new FastChangingProperty(TimeSpan.FromSeconds(1), 1, 3600 * 8, 0, DateTime.UtcNow)
            {
                Tag = (datas.GameChar.Id, child.Id),
            });
            gim.AddItem(child, fuhuaSlot, null, datas.ChangesItem); //放入孵化槽
            var qiwu = datas.GameChar.GetQiwuBag();
            if (jiyin.Count > 1)    //若尚有剩余基因蛋
            {
                jiyin.Count--;
                datas.ChangesItem.AddToChanges(jiyin);
            }
            else //若基因蛋用完
            {
                gim.MoveItem(jiyin, 1, qiwu, datas.ChangesItem);
            }

            if (parent1.TemplateId == ProjectConstant.HomelandPatCard) //若是卡片
                gim.MoveItem(parent1, 1, qiwu, datas.ChangesItem);
            if (parent2.TemplateId == ProjectConstant.HomelandPatCard) //若是卡片
                gim.MoveItem(parent2, 1, qiwu, datas.ChangesItem);
        }

        /// <summary>
        /// 孵化的核心算法。
        /// </summary>
        /// <param name="gameChar">角色对象。</param>
        /// <param name="parent1">双亲1。可以不是角色拥有的坐骑。</param>
        /// <param name="parent2">双亲2。可以不是角色拥有的坐骑。</param>
        /// <returns>孵化的结果。资质按双亲平均值计算。</returns>
        public GameItem FuhuaCore(GameChar gameChar, GameItem parent1, GameItem parent2)
        {
            var gim = World.ItemManager;
            var t1BodyT = gim.GetBodyTemplate(parent1);
            var t2BodyT = gim.GetBodyTemplate(parent2);
            var tids = gim.GetTujianResult(gameChar, t1BodyT, t2BodyT);
            GameItemTemplate headT, bodyT;  //输出结果的头和身体模板对象
            if (null != tids && VWorld.IsHit((double)tids.Value.Item3))  //若使用图鉴
            {
                headT = gim.GetTemplateFromeId(tids.Value.Item1);
                bodyT = gim.GetTemplateFromeId(tids.Value.Item2);
            }
            else //不用有图鉴
            {
                double probChun = 0.2;  //纯种坐骑的概率

                Action<GameItem> action = c =>  //分别针对每个双亲的计算公式
                {
                    if (c.TemplateId == ProjectConstant.HomelandPatCard) //若是卡片
                    {
                        var rank = parent1.Properties.GetDecimalOrDefault("nerank");  //等级
                        if (rank >= 3)   //若是高级坐骑
                        {
                            var bd = gim.GetBody(parent1);    //取身体对象
                            var tidString = bd.TemplateId.ToString(); //记录合成次数的键名
                            var suppusCount = gameChar.Properties.GetDecimalOrDefault(tidString); //已经用该卡合成的次数
                            if (suppusCount <= 3) //若尚未达成次数
                            {
                                probChun = 0; //不准出现纯种生物
                            }
                            gameChar.Properties[tidString] = ++suppusCount;
                        }
                    }
                };

                action(parent1); action(parent2);
                if (VWorld.IsHit(probChun))   //若出纯种生物
                {
                    if (VWorld.IsHit(0.5))   //若出a头
                    {
                        headT = gim.GetHeadTemplate(parent1);
                        bodyT = gim.GetBodyTemplate(parent1);
                    }
                    else //若出b头
                    {
                        headT = gim.GetHeadTemplate(parent2);
                        bodyT = gim.GetBodyTemplate(parent2);
                    }

                }
                else //出杂交生物
                {
                    if (VWorld.IsHit(0.5))   //若出a头
                    {
                        headT = gim.GetHeadTemplate(parent1);
                        bodyT = gim.GetBodyTemplate(parent2);
                    }
                    else //若出b头
                    {
                        headT = gim.GetHeadTemplate(parent2);
                        bodyT = gim.GetBodyTemplate(parent1);
                    }
                }
            }

            GameItem result = gim.CreateMounts(headT, bodyT);
            SetNe(result, parent1, parent2);    //设置天赋值
            return result;
        }

        /// <summary>
        /// 设置杂交后的天赋。
        /// </summary>
        /// <param name="child">要设置的对象。</param>
        /// <param name="parent1">双亲1。</param>
        /// <param name="parent1">双亲2。</param>
        private void SetNe(GameItem child, GameItem parent1, GameItem parent2)
        {
            var rank1 = parent1.Properties.GetDecimalOrDefault("nerank");
            var rank2 = parent2.Properties.GetDecimalOrDefault("nerank");

            var ne1 = parent1.Properties.GetDecimalOrDefault("neatk");
            var ne2 = parent2.Properties.GetDecimalOrDefault("neatk");
            var atk = Math.Max(0, (ne1 * rank1 * 0.15m + ne2 * rank2 * 0.15m) / 2 + VWorld.WorldRandom.Next(-5, 6));

            ne1 = parent1.Properties.GetDecimalOrDefault("nemhp");
            ne2 = parent2.Properties.GetDecimalOrDefault("nemhp");
            var mhp = Math.Max(0, (ne1 * rank1 * 0.15m + ne2 * rank2 * 0.15m) / 2 + VWorld.WorldRandom.Next(-5, 6));

            ne1 = parent1.Properties.GetDecimalOrDefault("neqlt");
            ne2 = parent2.Properties.GetDecimalOrDefault("neqlt");
            var qlt = Math.Max(0, (ne1 * rank1 * 0.15m + ne2 * rank2 * 0.15m) / 2 + VWorld.WorldRandom.Next(-5, 6));
            child.Properties["atk"] = atk;
            child.Properties["mhp"] = mhp;
            child.Properties["qlt"] = qlt;
        }
        #endregion 孵化相关

        #region 合成相关

        /// <summary>
        /// 资质合成。
        /// </summary>
        /// <param name="datas"></param>
        public void Hecheng(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameItems.Count == 2, $"物品数量错误。"))
            {
                return;
            }

            GameChar gc = datas.GameChar;
            GameItem lockAtk = datas.Lookup(gc.GameItems, ProjectConstant.LockAtkSlotId);
            if (lockAtk is null)
            {
                return;
            }

            GameItem lockMhp = datas.Lookup(gc.GameItems, ProjectConstant.LockMhpSlotId);
            if (lockMhp is null)
            {
                return;
            }

            GameItem lockQlt = datas.Lookup(gc.GameItems, ProjectConstant.LockQltSlotId);
            if (lockQlt is null)
            {
                return;
            }

            GameItem gameItem = datas.GameItems.FirstOrDefault(c => c.Parent.TemplateId == ProjectConstant.ZuojiBagSlotId);
            if (!datas.Verify(null != gameItem, "没有坐骑。"))
            {
                return;
            }

            GameItem gameItem2 = datas.GameItems.FirstOrDefault(c => c.Parent.TemplateId == ProjectConstant.ShoulanSlotId);
            if (!datas.Verify(null != gameItem2, "没有野兽。"))
            {
                return;
            }

            DbContext db = datas.GameChar.GameUser.DbContext;
            GameItemManager gim = World.ItemManager;
            //攻击资质
            if (lockAtk.Children.Count <= 0)
            {
                double rnd1 = VWorld.GetRandomNumber(0, 1);
                decimal rnd2 = (decimal)(rnd1 switch
                {
                    _ when rnd1 < 0.15 => VWorld.GetRandomNumber(0, 0.2),
                    _ when rnd1 >= 0.15 && rnd1 < 0.85 => VWorld.GetRandomNumber(0.25, 0.75),
                    _ => VWorld.GetRandomNumber(0.75, 1),
                });
                decimal ne = gameItem.GetDecimalOrDefault("neatk") * (1 - rnd2) + gameItem2.GetDecimalOrDefault("neatk") * rnd2;
                ne = Math.Round(ne, MidpointRounding.AwayFromZero);
                gameItem.SetPropertyValue("neatk", ne);
            }
            else
            {
                GameItem lockItem = lockAtk.Children.First();
                datas.ChangesItem.AddToRemoves(lockItem.ContainerId.Value, lockItem.Id);
                gim.ForceDelete(lockItem);
            }
            //血量资质
            if (lockMhp.Children.Count <= 0)
            {
                double rnd1 = VWorld.GetRandomNumber(0, 1);
                decimal rnd2 = (decimal)(rnd1 switch
                {
                    _ when rnd1 < 0.15 => VWorld.GetRandomNumber(0, 0.2),
                    _ when rnd1 >= 0.15 && rnd1 < 0.85 => VWorld.GetRandomNumber(0.25, 0.75),
                    _ => VWorld.GetRandomNumber(0.75, 1),
                });
                decimal ne = gameItem.GetDecimalOrDefault("nemhp") * (1 - rnd2) + gameItem2.GetDecimalOrDefault("nemhp") * rnd2;
                ne = Math.Round(ne, MidpointRounding.AwayFromZero);
                gameItem.SetPropertyValue("nemhp", ne);
            }
            else
            {
                GameItem lockItem = lockMhp.Children.First();
                datas.ChangesItem.AddToRemoves(lockItem.ContainerId.Value, lockItem.Id);
                gim.ForceDelete(lockItem);
            }
            //质量资质
            if (lockQlt.Children.Count <= 0)
            {
                double rnd1 = VWorld.GetRandomNumber(0, 1);
                decimal rnd2 = (decimal)(rnd1 switch
                {
                    _ when rnd1 < 0.15 => VWorld.GetRandomNumber(0, 0.2),
                    _ when rnd1 >= 0.15 && rnd1 < 0.85 => VWorld.GetRandomNumber(0.25, 0.75),
                    _ => VWorld.GetRandomNumber(0.75, 1),
                });
                decimal ne = gameItem.GetDecimalOrDefault("neqlt") * (1 - rnd2) + gameItem2.GetDecimalOrDefault("neqlt") * rnd2;
                ne = Math.Round(ne, MidpointRounding.AwayFromZero);
                gameItem.SetPropertyValue("neqlt", ne);
            }
            else
            {
                GameItem lockItem = lockQlt.Children.First();
                datas.ChangesItem.AddToRemoves(lockItem.ContainerId.Value, lockItem.Id);
                gim.ForceDelete(lockItem);
            }
            datas.ChangesItem.AddToRemoves(gameItem2.ContainerId.Value, gameItem2.Id);
            gim.ForceDelete(gameItem2);
            datas.ChangesItem.AddToChanges(gameItem.ContainerId.Value, gameItem);
        }
        #endregion 合成相关

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Verify(ApplyBlueprintDatas datas, IEnumerable<GameItem> gameItems, Guid containerTId, Guid itemTId)
        {
            GameItemTemplateManager gitm = World.ItemTemplateManager;
            GameItemTemplate cTemplate = gitm.GetTemplateFromeId(containerTId);
            if (datas.Verify(cTemplate != null, $"无法找到指定容器模板，Number = {containerTId}"))
            {
                return false;
            }

            GameItem container = gameItems.FirstOrDefault(c => c.TemplateId == containerTId);
            datas.Verify(container != null, $"无法找到指定模板Id的容器，模板Id = {containerTId}");
            return true;
        }

        /// <summary>
        /// 钻石买体力。
        /// </summary>
        /// <param name="datas"></param>
        public void AddTili(ApplyBlueprintDatas datas)
        {
            var tili = datas.GameChar.GetTili();
            var fcp = tili?.Name2FastChangingProperty.GetValueOrDefault("Count");
            if (!datas.Verify(fcp != null, "无法找到体力属性"))
            {
                return;
            }

            GameItem zuanshi = datas.GameChar.GetZuanshi();
            if (!datas.Verify(zuanshi != null, "无法找到钻石对象"))
            {
                return;
            }

            if (!datas.Verify(zuanshi.Count > 20, $"需要20钻石，但目前仅有{zuanshi.Count}。"))
            {
                return;
            }

            fcp.GetCurrentValueWithUtc();
            fcp.LastValue += 20;    //无视限制增加体力
            tili.Count = fcp.LastValue; //
            zuanshi.Count -= 20;    //扣除钻石
            datas.ChangesItem.AddToChanges(datas.GameChar.Id, zuanshi);
            datas.ChangesItem.AddToChanges(tili);
        }

        #region 社交相关

        #endregion 社交相关
    }

    public static class ApplyBlueprintDatasExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Verify(this ApplyBlueprintDatas obj, bool succ, string errorMessage, params Guid[] errorItemTIds)
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
        public static GameItem Lookup(this ApplyBlueprintDatas obj, IEnumerable<GameItem> parent, Guid? templateId, Guid? id = null)
        {
            IEnumerable<GameItem> resultColl;
            if (templateId.HasValue)    //若需限定模板Id
            {
                Guid tid = templateId.Value;
                resultColl = parent.Where(c => c.TemplateId == tid);
            }
            else
            {
                resultColl = parent;
            }

            GameItem result;
            if (id.HasValue) //若要限定Id
            {
                Guid idMe = id.Value;
                result = resultColl.FirstOrDefault(c => c.Id == idMe);
            }
            else
            {
                result = resultColl.FirstOrDefault();
            }

            if (result is null)  //若没有找到
            {
                obj.DebugMessage = $"无法找到物品。TId={templateId},Number={id}";
                obj.HasError = true;
                if (templateId.HasValue)
                {
                    obj.ErrorItemTIds.Add(templateId.Value);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// 主基地升级送物配置类。
    /// </summary>
    public class MainbaseUpgradePrv
    {
        #region MyRegion

        public const string text = "1	{e58f479f-362b-44e7-a5da-74f2ff0f1667}		{312612a5-30dd-4e0a-a71d-5074397428fb}	迫击炮-2" + "\r\n" +
        "1	{cad6ffb8-3e56-4bf0-a3ab-af0251e467ee}		{312612a5-30dd-4e0a-a71d-5074397428fb}	近战炮-1" + "\r\n" +
        "1	{fff06375-be88-4930-bc5b-ea73a8f97c12}		{312612a5-30dd-4e0a-a71d-5074397428fb}	爆炸樱桃-2" + "\r\n" +
        "1	{e0a05839-bb55-4e0c-9982-44123b307bde}		{312612a5-30dd-4e0a-a71d-5074397428fb}	减速荆棘-1" + "\r\n" +
        "2	{b2e4887c-401c-4753-bf15-dcae2a05194f}		{312612a5-30dd-4e0a-a71d-5074397428fb}	回旋炮-1" + "\r\n" +
        "2	{ce188fec-ce1a-4c35-aa8c-c41c65767fcb}		{312612a5-30dd-4e0a-a71d-5074397428fb}	爆炸樱桃-3" + "\r\n" +
        "2	{868a9f60-cf8c-4a81-8a20-8ff92487833c}		{312612a5-30dd-4e0a-a71d-5074397428fb}	弹飞蘑菇-2" + "\r\n" +
        "2		101	{234f8c55-4c3c-4406-ad38-081d29564f20}	地块-1" + "\r\n" +
        "3	{6d7e3c66-8be5-44a3-b548-702bf91118b9}		{312612a5-30dd-4e0a-a71d-5074397428fb}	近战炮-2" + "\r\n" +
        "3	{dfe95a1f-e54a-41d0-85cd-5c2d82741e23}		{312612a5-30dd-4e0a-a71d-5074397428fb}	滚石炮-1" + "\r\n" +
        "3	{a1f79ee3-374e-4e21-84a2-f77071444906}		{312612a5-30dd-4e0a-a71d-5074397428fb}	减速荆棘-2" + "\r\n" +
        "4	{24ec8437-f886-4289-9d5d-7e304e7b1974}		{312612a5-30dd-4e0a-a71d-5074397428fb}	回旋炮-2" + "\r\n" +
        "4	{51b4bb51-33fc-4595-a174-52f01b52c72e}		{312612a5-30dd-4e0a-a71d-5074397428fb}	毁灭蘑菇-1" + "\r\n" +
        "4		102	{234f8c55-4c3c-4406-ad38-081d29564f20}	地块-2" + "\r\n" +
        "5	{97f6af0d-77ce-4eeb-bad6-78577651e5d1}		{312612a5-30dd-4e0a-a71d-5074397428fb}	直射炮-3" + "\r\n" +
        "5	{c447775c-5079-4524-84df-2130d66a8f64}		{312612a5-30dd-4e0a-a71d-5074397428fb}	导弹炮-1" + "\r\n" +
        "5	{6733f1c6-b5e6-4f57-8011-790e83ea8a96}		{312612a5-30dd-4e0a-a71d-5074397428fb}	弹飞蘑菇-3" + "\r\n" +
        "6	{8b493d6d-3fb4-42e2-92a8-91ba9bf177e4}		{312612a5-30dd-4e0a-a71d-5074397428fb}	近战炮-3" + "\r\n" +
        "6	{e377981a-2501-44cf-a350-6d82255a4f02}		{312612a5-30dd-4e0a-a71d-5074397428fb}	黏着栗子-1" + "\r\n" +
        "6		103	{234f8c55-4c3c-4406-ad38-081d29564f20}	地块-3" + "\r\n" +
        "7	{67cbef93-c4f0-4df2-9647-915cb85fce7b}		{312612a5-30dd-4e0a-a71d-5074397428fb}	迫击炮-3" + "\r\n" +
        "7	{fad2355c-6514-42bb-8ca8-101c7c1be06a}		{312612a5-30dd-4e0a-a71d-5074397428fb}	减速荆棘-3" + "\r\n" +
        "7	{bba37f02-c4b1-4bb7-aa2e-d3f35a722cf8}		{312612a5-30dd-4e0a-a71d-5074397428fb}	黏着栗子-2" + "\r\n" +
        "8	{2a9a1e7c-6676-4e97-8726-3062b57a2a6a}		{312612a5-30dd-4e0a-a71d-5074397428fb}	滚石炮-2" + "\r\n" +
        "8	{f9144284-5e50-4643-bb2a-f8ae6e4f3290}		{312612a5-30dd-4e0a-a71d-5074397428fb}	回旋炮-3" + "\r\n" +
        "8		104	{234f8c55-4c3c-4406-ad38-081d29564f20}	地块-4" + "\r\n" +
        "9	{05be07f7-a97a-4cab-85c1-4759daeb17bf}		{312612a5-30dd-4e0a-a71d-5074397428fb}	导弹炮-2" + "\r\n" +
        "9	{d4bd6008-af90-4a38-9c17-7ee961fb4787}		{312612a5-30dd-4e0a-a71d-5074397428fb}	毁灭蘑菇-2";
        #endregion

        /// <summary>
        /// 送的等级，如1，就是升级到1级时送品
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 如果有值，则是送品地块的位置号。
        /// </summary>
        public int? Genus { get; set; }

        /// <summary>
        /// 送品的模板Id。
        /// </summary>
        public Guid? PrvTId { get; set; }

        /// <summary>
        /// 送品的父容器模板Id。
        /// </summary>
        public Guid ParentTId { get; set; }

        /// <summary>
        /// 注释，服务器不使用。
        /// </summary>
        public string Remark { get; set; }

        private static List<MainbaseUpgradePrv> _Alls;
        public static List<MainbaseUpgradePrv> Alls
        {
            get
            {
                if (_Alls is null)
                {
                    lock (typeof(MainbaseUpgradePrv))
                    {
                        if (_Alls is null)
                        {
                            _Alls = new List<MainbaseUpgradePrv>();
                            foreach (string line in text.Split("\r\n", StringSplitOptions.None))   //枚举行
                            {
                                if (string.IsNullOrWhiteSpace(line))
                                {
                                    continue;
                                }

                                string[] ary = line.Split('\t', StringSplitOptions.None);
                                MainbaseUpgradePrv item = new MainbaseUpgradePrv()
                                {
                                    Level = int.Parse(ary[0]),
                                    Genus = int.TryParse(ary[2], out int gns) ? gns : null as int?,
                                    ParentTId = Guid.Parse(ary[3]),
                                    PrvTId = string.IsNullOrWhiteSpace(ary[1]) ? null as Guid? : Guid.Parse(ary[1]),
                                    Remark = ary[4],
                                };
                                _Alls.Add(item);
                            }
                        }
                    }
                }

                return _Alls;
            }
        }
    }
}
