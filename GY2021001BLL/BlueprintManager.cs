using GY2021001DAL;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OwGame;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class BlueprintData
    {
        IServiceProvider _Service;
        private readonly BlueprintTemplate _Template;
        private List<FormulaData> _Formulas;

        public IServiceProvider Service { get => _Service; set => _Service = value; }

        public BlueprintTemplate Template => _Template;

        public BlueprintData(IServiceProvider service, BlueprintTemplate template)
        {
            _Service = service;
            _Template = template;
            _Formulas = template.FormulaTemplates.Select(c => new FormulaData(c, this)).ToList();
        }
    }

    public class FormulaData
    {
        private readonly BpFormulaTemplate _Template;
        private List<MaterialData> _Materials;
        private readonly BlueprintData _Parent;

        public FormulaData(BpFormulaTemplate template, BlueprintData parent)
        {
            _Parent = parent;
            _Template = template;
            _Materials = template.BptfItemTemplates.Select(c => new MaterialData(c, this)).ToList();
        }

        public BpFormulaTemplate Template => _Template;

        public BlueprintData Parent => _Parent;
    }

    public class MaterialData
    {
        private readonly FormulaData _Parent;
        private readonly BpItemTemplate _Template;
        private readonly GameVariable _Variable;

        public MaterialData(BpItemTemplate template, FormulaData parent)
        {
            _Parent = parent;
            _Template = template;
            if (!GameVariable.TryParse(template.VariableDeclaration, out _Variable))
            {
                throw new NotImplementedException();    //TO DO
            }
        }

        public BpItemTemplate Template => _Template;

        public FormulaData Parent => _Parent;

        public GameVariable Variable => _Variable;

        public bool Match(ApplyBlueprintDatas datas)
        {
            bool returnVal = true;
            var coll = datas.GameItems.Concat(OwHelper.GetAllSubItemsOfTree(datas.GameChar.GameItems, c => c.Children));    //要遍历的所有物品，确保指定物品最先被匹配
            BpEnvironmentDatas env = new BpEnvironmentDatas();
            return returnVal;
        }
    }

    public sealed class BpItemDataObject
    {

        public BpItemDataObject(IEnumerable<GameCondition> conditions, GameOperand upperBound, GameOperand lowerBound, IEnumerable<BpPropertyChanges> propertyChanges, GameVariable variable)
        {
            Conditions = conditions;
            UpperBound = upperBound;
            LowerBound = lowerBound;
            PropertyChanges = propertyChanges;
            Variable = variable;
        }

        public readonly IEnumerable<GameCondition> Conditions;
        public readonly GameOperand UpperBound;
        public readonly GameOperand LowerBound;
        public readonly IEnumerable<BpPropertyChanges> PropertyChanges;
        public GameVariable Variable;
    }

    /// <summary>
    /// 获取复杂属性的运行时值的环境数据。
    /// </summary>
    public sealed class BpEnvironmentDatas
    {
        public BpEnvironmentDatas()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="gameChar"></param>
        /// <param name="current"></param>
        /// <param name="gameItems">直接引用该对象，对象后续更改将导致调用该对象成员的结果发生相应变化。</param>
        public BpEnvironmentDatas(IServiceProvider service, GameChar gameChar, GameItem current, IDictionary<Guid, GameItem> gameItems)
        {
            _Service = service;
            _GameChar = gameChar;
            _Current = current;
            _GameItems = gameItems;
        }

        private IServiceProvider _Service;
        /// <summary>
        /// 当前使用的服务对象。
        /// </summary>
        public IServiceProvider Service => _Service;

        private GameChar _GameChar;
        /// <summary>
        /// 当前的角色对象。
        /// </summary>
        public GameChar GameChar => _GameChar;

        GameItem _Current;
        /// <summary>
        /// 当前物品对象。
        /// </summary>
        public GameItem Current { get => _Current; set => _Current = value; }

        IDictionary<Guid, GameItem> _GameItems;
        /// <summary>
        /// 键是物料对象Id,值是具体对象（用此对象当作物料）。
        /// </summary>
        public IDictionary<Guid, GameItem> GameItems => _GameItems;

        /// <summary>
        /// 局部变量。
        /// </summary>
        internal GameVariable Variables { get; } = new GameVariable();
    }

    public static class BlueprintExtensions
    {
        private readonly static ConcurrentDictionary<Guid, GameOperand> _FormulaId2ProbObject = new ConcurrentDictionary<Guid, GameOperand>();
        public static GameOperand GetProbObject(this BpFormulaTemplate formulaTemplate)
        {
            return _FormulaId2ProbObject.GetOrAdd(formulaTemplate.Id, c =>
            {
                if (GameOperand.TryParse(formulaTemplate.Prob, out GameOperand result))
                    return result;
                return null;
            });
        }

        /// <summary>
        /// 物料模板对象的复杂属性计算对象。
        /// </summary>
        static readonly private ConcurrentDictionary<Guid, BpItemDataObject> _ItemId2Datas = new ConcurrentDictionary<Guid, BpItemDataObject>();

        /// <summary>
        /// 获取物料项辅助计算对象。
        /// </summary>
        /// <param name="bpItem"></param>
        /// <returns></returns>
        public static BpItemDataObject GetBpItemDataObject(this BpItemTemplate bpItem)
        {
            if (_ItemId2Datas.TryGetValue(bpItem.Id, out BpItemDataObject result))  //若已经缓存
                return result;
            //条件
            var conditionals = new List<GameCondition>(); GameCondition.FillFromString(conditionals, bpItem.Conditional);
            //上限
            GameOperand upperBound = null; GameOperand.TryParse(bpItem.CountUpperBound, out upperBound);
            //下限
            GameOperand lowerBound = null; GameOperand.TryParse(bpItem.CountLowerBound, out lowerBound);
            //属性更改
            var changes = new List<BpPropertyChanges>(); BpPropertyChanges.FillFromString(changes, bpItem.PropertiesChanges);
            //获取变量对象
            var gameVarSucc = GameVariable.TryParse(bpItem.VariableDeclaration, out var gameVar);
            return _ItemId2Datas.GetOrAdd(bpItem.Id, new BpItemDataObject(conditionals, upperBound, lowerBound, changes, gameVar));
        }

        /// <summary>
        /// 获取物料与指定物品是否匹配。
        /// </summary>
        /// <param name="bpItem"></param>
        /// <param name="gameItem"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMatch(this BpItemTemplate bpItem, BpEnvironmentDatas datas)
        {
            var _ = GetBpItemDataObject(bpItem).Conditions;
            return _.All(c => c.GetValue(datas));
        }

        /// <summary>
        /// 在一组物品中寻找匹配的物品。
        /// </summary>
        /// <param name="bpItem"></param>
        /// <param name="datas">除了<see cref="BpEnvironmentDatas.Current"/>属性外需要正确设置。返回时如果找到，则该属性是其对象，否则为null。</param>
        /// <param name="gameItems"></param>
        /// <returns>true成功找到匹配项，此时<paramref name="datas"/>的Current属性包含找到的数据，false没有找到</returns>
        public static bool FindMatch(this BpItemTemplate bpItem, BpEnvironmentDatas datas, IEnumerable<GameItem> gameItems)
        {
            var conditions = bpItem.GetBpItemDataObject().Conditions as ICollection<GameCondition>;
            conditions ??= bpItem.GetBpItemDataObject().Conditions.ToList();
            foreach (var item in gameItems)
            {
                datas.Current = item;
                if (conditions.All(c => c.GetValue(datas)))
                    return true;
            }
            datas.Current = null;
            return false;
        }

        static ConcurrentDictionary<Guid, GameVariable> _Id2Variable = new ConcurrentDictionary<Guid, GameVariable>();

        /// <summary>
        /// 获取该公式的蓝图脚本变量。
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        public static GameVariable GetGameVariable(this BpFormulaTemplate formula)
        {
            if (_Id2Variable.TryGetValue(formula.Id, out var result))   //若已经分析了本地变量
                return result;
            result = new GameVariable();
            foreach (var item in formula.BptfItemTemplates)
            {
                if (!GameVariable.TryParse(item.VariableDeclaration, out var tmp))    //若语法错误
                {
                    Debug.WriteLine($"无法分析公式对象(Id={formula.Id})的变量声明\"{item.VariableDeclaration}\"");
                    return _Id2Variable.GetOrAdd(formula.Id, null as GameVariable);
                }
                tmp.AddPrefix(item.Id);
                foreach (var kvp in tmp.Properties)
                {
                    result.Properties[kvp.Key] = kvp.Value;
                }
            }
            var str = string.Join(',', formula.BptfItemTemplates.Select(c => c.VariableDeclaration));
            if (!GameVariable.TryParse(str, out result))    //若语法错误
            {
                Debug.WriteLine($"无法分析公式对象(Id={formula.Id})的变量声明\"{str}\"");
                return _Id2Variable.GetOrAdd(formula.Id, null as GameVariable);
            }
            return _Id2Variable.GetOrAdd(formula.Id, result);
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
    }

    /// <summary>
    /// 解析和计算字符表达式的类。
    /// 支持如:{0A396457-FA17-419E-BBB1-20C2588BC2EB}\sscatk:-1|-3|-5|-8|-11|-15|-19|-24|-30
    /// rndi整数取[0,整数)的整数(定点型)，rnd取[0，1)的定点数。
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class GameOperand
    {
        #region 静态成员

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <param name="result"></param>
        /// <returns>true明确分析得到一个值，false得到的是一个没有明确类型的字符串，调用者可能需要再次解释出参中的字符串。
        /// 针对空引用或字符串或全空白则立即返回false,且出参是<see cref="string.Empty"/></returns>
        public static bool TryGetConst(string str, out object result)
        {
            bool succ = false;
            if (string.IsNullOrWhiteSpace(str))
            {
                result = string.Empty;
            }
            else if (decimal.TryParse(str, out decimal resultDec))   //若是数值
            {
                result = resultDec;
                succ = true;
            }
            else if (str.StartsWith('"') && str.EndsWith('"'))  //若是字符串
            {
                result = str.Trim('"');
                succ = true;
            }
            else if (Guid.TryParse(str, out Guid guid))  //若是一个Guid
            {
                result = guid;
                succ = true;
            }
            else
                result = str;
            return succ;
        }

        /// <summary>
        /// 通过分析字符串获取对象。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParse(string str, out GameOperand result)
        {
            //"F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4"
            str = str.Trim();  //忽略空白
            StringBuilder sb = new StringBuilder();
            bool succ = false;
            result = null;
            if (TryGetConst(str, out object obj))   //若是常量
            {
                result = new GameOperand()
                {
                    IsConst = true,
                    ConstValue = obj,
                };
                succ = true;
            }
            else
            {
                var ary = str.Split(OwHelper.ColonArrayWithCN, StringSplitOptions.RemoveEmptyEntries); //以冒号分割
                if (ary.Length < 1 || ary.Length > 2)    //若结构不正确
                    return succ;
                result = new GameOperand()
                {
                    IsConst = false,
                };
                string[] leftAry;
                //处理左半部分
                leftAry = ary[0].Split(OwHelper.PathSeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in leftAry)
                {
                    _ = TryGetConst(item, out obj);
                    result.LeftPath.Add(obj);
                }
                if (2 == ary.Length)   //若有左右两部分
                {
                    if (!OwHelper.AnalyseSequence(ary[1], result.RightSequence))
                        return false;
                }
                succ = true;
                result.Normalize();
            }
            return succ;
        }

        /// <summary>
        /// 获取复杂指代的左侧部分。
        /// </summary>
        /// <param name="path"></param>
        /// <param name="current"></param>
        /// <param name="gameItems"></param>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static bool TryGetLeft(IEnumerable<object> path, GameItem current, IDictionary<Guid, GameItem> gameItems, out GameItem obj, out string propertyName)
        {
            propertyName = path.LastOrDefault() as string;
            obj = null;
            if (null == propertyName)
                return false;
            var objIds = path.TakeWhile(c => c is Guid).OfType<Guid>().ToArray();    //获取前n个指代对象的id集合
            if (objIds.Length <= 0) //若没有指定路径
                obj = current;  //就是当前对象
            else
            {
                if (!gameItems.TryGetValue(objIds[0], out obj))
                    return false;

                foreach (var item in objIds.Skip(1))    //后续id是对象的子对象的模板Id
                {
                    obj = obj.Children.FirstOrDefault(c => c.TemplateId == item);
                    if (null == obj)
                        return false;
                }
            }
            return true;
        }

        #endregion 静态成员
        public GameOperand()
        {

        }

        /// <summary>
        /// 化简。
        /// </summary>
        public void Normalize()
        {
        }

        #region 属性及相关

        /// <summary>
        /// 是否是常量。为true时 <see cref="ConstValue"/>才有效。
        /// </summary>
        public bool IsConst { get; set; }

        /// <summary>
        /// 常量的值，此值仅在<see cref="IsConst"/> 为 true时有效。
        /// </summary>
        public object ConstValue { get; set; }

        List<object> _LeftPath;
        /// <summary>
        /// 左侧属性路径。
        /// </summary>
        public List<object> LeftPath { get => _LeftPath ??= new List<object>(); }

        List<decimal> _RightSequence;
        /// <summary>
        /// 右侧序列。
        /// </summary>
        public List<decimal> RightSequence { get => _RightSequence ??= new List<decimal>(); }
        #endregion 属性及相关

        /// <summary>
        /// 
        /// </summary>
        /// <param name="datas">环境数据。</param>
        /// <returns></returns>
        public object GetValue(BpEnvironmentDatas datas)
        {
            if (IsConst) //若是常量
                return ConstValue;
            else if (LeftPath.Count == 0)  //若左侧没有指代
                return null;
            if (!TryGetLeft(LeftPath, datas.Current, datas.GameItems, out GameItem gameItem, out string propName)) //若找不到指代对象
                return null;
            var world = datas.Service?.GetRequiredService<VWorld>();
            var gim = world.ItemManager;
            //获取属性值
            if (!datas.Variables.TryGetValue(propName, datas, out var leftVal)) //若没有本地变量
            {
                var _ = VWorld.WorldRandom;
                //处理取随机数的问题
                if (propName.StartsWith("rndi"))  //若取随机值
                {
                    //rndi101
                    var maxRndStr = propName.Substring(4);
                    if (int.TryParse(maxRndStr, out var maxRnd))
                        leftVal = (decimal)_.Next(maxRnd);
                    else
                        leftVal = (decimal)_.Next(100);
                }
                else if (propName.StartsWith("rnd"))  //若取随机值
                {
                    leftVal = (decimal)_.NextDouble();
                }
                else
                    leftVal = propName switch
                    {
                        _ => gim.GetPropertyValue(gameItem, propName)
                    };
            }
            if (_RightSequence == null || RightSequence.Count == 0)   //若没有序列
                return leftVal;
            if (!(leftVal is decimal dec))
                return null;
            var index = (int)Math.Round(dec);
            if (index >= RightSequence.Count || index < 0)  //索引超界
                return null;
            return RightSequence[index];
        }

        /// <summary>
        /// 为指代的属性赋值。
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="val">新值。</param>
        /// <returns></returns>
        public bool SetValue(BpEnvironmentDatas datas, object val)
        {
            if (IsConst) //若是常量
                return false;
            else if (LeftPath.Count == 0)  //若左侧没有指代
                return false;
            if (!TryGetLeft(LeftPath, datas.Current, datas.GameItems, out GameItem gameItem, out string propName)) //若找不到指代对象
                return false;
            var gim = datas.Service.GetRequiredService<GameItemManager>();
            gim.SetPropertyValue(gameItem, propName, val);
            return true;
        }

        protected string GetDebuggerDisplay()
        {
            if (IsConst)
                return ConstValue.ToString();
            else if (RightSequence.Count > 0)
                return $"{string.Join('/', LeftPath)}:{string.Join('|', RightSequence)}";
            else
                return $"{string.Join('/', LeftPath)}";
        }

        /// <summary>
        /// 为左侧表达式添加限定前缀。
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true成功添加了前缀，false没有必要添加。</returns>
        public bool AddPrefix(Guid id)
        {
            if (IsConst) //若是常量
                return false;
            if (_LeftPath == null || LeftPath.Count == 0)    //左侧无表达式
                return false;
            if (LeftPath[0] is Guid tmp && tmp == id)   //若已经添加了前缀
                return false;
            LeftPath.Insert(0, id);
            return true;
        }
    }

    /// <summary>
    /// 提取的局部变量类。
    /// </summary>
    public class GameVariable
    {
        /// <summary>
        /// 分析字符串。试图得到一个对象。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="result"></param>
        /// <returns>true成功得到一个对象，false至少有一个表达式语法错误。
        /// 字符串是空引用，空字符串或空白会立即返回成功，此时<paramref name="result"/>是一个对象，但没有内容。</returns>
        public static bool TryParse(string str, out GameVariable result)
        {
            bool returnVal = true;
            result = new GameVariable()
            {
            };
            if (string.IsNullOrWhiteSpace(str))
                return returnVal;
            var items = str.Split(OwHelper.CommaArrayWithCN);
            foreach (var item in items)
            {
                var ary = item.Split('=');
                if (ary.Length != 2 || !GameOperand.TryParse(ary[1], out var operand))
                {
                    returnVal = false;
                    break;
                }
                result.Properties[ary[0]] = operand;
            }
            if (!returnVal) result = null;
            return returnVal;
        }

        /// <summary>
        /// 所绑定的原料Id。
        /// </summary>
        Guid _MaterialId;

        public GameVariable()
        {

        }

        /// <summary>
        /// 记录所有局部变量。
        /// </summary>
        public Dictionary<string, GameOperand> Properties { get; } = new Dictionary<string, GameOperand>();
        public Guid MaterialId { get => _MaterialId; set => _MaterialId = value; }

        /// <summary>
        /// 获取值。
        /// </summary>
        /// <param name="propName"></param>
        /// <param name="environmentDatas"></param>
        /// <param name="result">返回属性的值。</param>
        /// <returns>true返回了属性值，false没有找到指定属性。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(string propName, BpEnvironmentDatas environmentDatas, out object result)
        {
            if (!Properties.TryGetValue(propName, out var operand))
            {
                result = null;
                return false;
            }
            GameItem oldCurrent = environmentDatas.Current;
            try
            {
                if (!environmentDatas.GameItems.TryGetValue(_MaterialId, out var newCurrent))   //若找不到指定的原料
                {
                    result = null;
                    return false;
                }
                environmentDatas.Current = newCurrent;
                result = operand.GetValue(environmentDatas);
            }
            finally
            {
                environmentDatas.Current = oldCurrent;
            }
            return true;
        }

        /// <summary>
        /// 为所有变量右侧操作数对象内左侧引用，增加原料Id限定。
        /// </summary>
        /// <param name="id"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPrefix(Guid id)
        {
            foreach (var item in Properties)
            {
                item.Value.AddPrefix(id);
            }
        }
    }

    /// <summary>
    /// 条件对象。
    /// </summary>
    public class GameCondition
    {
        static private readonly string comparePattern = @"(?<left>[^\=\<\>]+)(?<op>[\=\<\>]{1,2})(?<right>[^\=\<\>\,，]+)[\,，]?";

        public static void FillFromString(ICollection<GameCondition> conditions, string str)
        {
            var matchs = Regex.Matches(str, comparePattern);

            foreach (var item in matchs.OfType<Match>())
            {
                if (!item.Success)
                    continue;
                var tmp = new GameCondition()
                {
                    Operator = item.Groups["op"].Value.Trim(),
                };
                if (GameOperand.TryParse(item.Groups["left"].Value, out GameOperand gameStringProperty))
                    tmp.Left = gameStringProperty;
                if (GameOperand.TryParse(item.Groups["right"].Value, out gameStringProperty))
                    tmp.Right = gameStringProperty;
                conditions.Add(tmp);
            }
            return;
        }

        #region 属性及相关

        public GameOperand Left { get; set; }

        public GameOperand Right { get; set; }

        public string Operator { get; set; }

        #endregion 属性及相关

        public bool GetValue(BpEnvironmentDatas datas)
        {
            object left = Left.GetValue(datas);
            object right = Right.GetValue(datas);
            int? cr = null;
            if (left == right)  //若引用相等
                return Operator switch
                {
                    "==" => true,
                    "=" => true,
                    "!=" => false,
                    "<>" => false,
                    "<=" => true,
                    ">=" => true,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            else if (left == null)
                return Operator switch
                {
                    ">" => false,
                    ">=" => false,

                    "<" => true,
                    "<=" => true,

                    "==" => false,
                    "=" => false,

                    "!=" => true,
                    "<>" => true,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            else if (!left.GetType().Equals(right.GetType()))   //类型不等
            {
                if (TryToDecimal(left, out decimal d1) && TryToDecimal(right, out decimal d2))  //若都能转换为decimal
                    return Operator switch
                    {
                        ">" => d1 > d2,
                        ">=" => d1 >= d2,

                        "<" => d1 < d2,
                        "<=" => d1 <= d2,

                        "==" => d1 == d2,
                        "=" => d1 == d2,

                        "!=" => d1 != d2,
                        "<>" => d1 != d2,
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                else
                    return Operator switch
                    {
                        "==" => false,
                        "=" => false,

                        "!=" => true,
                        "<>" => true,
                        _ => throw new ArgumentOutOfRangeException(),
                    };
            }
            else if (left is IComparable comparable)
            {
                cr = comparable.CompareTo(right);
                return Operator switch
                {
                    ">" => cr > 0,
                    ">=" => cr >= 0,

                    "<" => cr < 0,
                    "<=" => cr <= 0,

                    "==" => 0 == cr,
                    "=" => 0 == cr,

                    "!=" => 0 != cr,
                    "<>" => 0 != cr,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
            else //若不支持比较接口
            {
                var b = Equals(left, right);
                return Operator switch
                {
                    "==" => b,
                    "=" => b,

                    "!=" => !b,
                    "<>" => !b,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
        }

        bool TryToDecimal(object obj, out decimal result)
        {
            bool succ = false;
            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    result = Convert.ToDecimal(obj);
                    succ = true;
                    break;
                default:
                    result = decimal.Zero;
                    break;
            }
            return succ;
        }
    };

    /// <summary>
    /// 属性变化。
    /// </summary>
    public class BpPropertyChanges
    {
        private readonly static string Patt = @"(?<left>[^\+\-\*\/\=]+)(?<op>[\+\-\*\/\=]+)(?<right>[\d\.]+)[\,，]?";
        public static void FillFromString(ICollection<BpPropertyChanges> changes, string str)
        {
            foreach (var item in Regex.Matches(str, Patt).OfType<Match>())
            {
                if (!item.Success)
                    continue;
                var tmp = new BpPropertyChanges()
                {
                    OpertorStr = item.Groups["op"].Value,
                };
                if (GameOperand.TryParse(item.Groups["left"].Value, out GameOperand stringProperty))
                {
                    tmp.Left = stringProperty;
                    if (tmp.Left.IsConst)
                        Debug.WriteLine("发现试图对常量赋值。");
                }
                if (GameOperand.TryParse(item.Groups["right"].Value, out stringProperty))
                    tmp.Right = stringProperty;
                changes.Add(tmp);
            }
        }

        public BpPropertyChanges()
        {

        }


        public GameOperand Left { get; set; }

        public GameOperand Right { get; set; }

        public string OpertorStr { get; set; }

        public void Apply(BpEnvironmentDatas datas)
        {
            var gameItem = datas.Current;
            var gim = datas.Service.GetRequiredService<GameItemManager>();
            decimal ov, //老值
                nv; //新值
            switch (OpertorStr)
            {
                case "+":
                    ov = Convert.ToDecimal(Left.GetValue(datas));
                    nv = Convert.ToDecimal(Right.GetValue(datas));
                    Left.SetValue(datas, ov + nv);
                    break;
                case "-":
                    ov = Convert.ToDecimal(Left.GetValue(datas));
                    nv = Convert.ToDecimal(Right.GetValue(datas));
                    Left.SetValue(datas, ov - nv);
                    break;
                case "*":
                    ov = Convert.ToDecimal(Left.GetValue(datas));
                    nv = Convert.ToDecimal(Right.GetValue(datas));
                    Left.SetValue(datas, ov * nv);
                    break;
                case "/":
                    ov = Convert.ToDecimal(Left.GetValue(datas));
                    nv = Convert.ToDecimal(Right.GetValue(datas));
                    Left.SetValue(datas, ov / nv);
                    break;
                case "=":
                    Left.SetValue(datas, Right.GetValue(datas));
                    break;
                default:
                    break;
            }
        }
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

        private ConcurrentDictionary<Guid, (IEnumerable<GameCondition> Conditions, GameOperand UpperBound, GameOperand LowerBound, IEnumerable<BpPropertyChanges> PropertyChanges)>
            _ItemId2Datas = new ConcurrentDictionary<Guid, (IEnumerable<GameCondition> Conditions, GameOperand UpperBound, GameOperand LowerBound, IEnumerable<BpPropertyChanges> PropertyChanges)>();

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
        /// 获取物料项辅助计算对象。
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public (IEnumerable<GameCondition> Conditions, GameOperand UpperBound, GameOperand LowerBound, IEnumerable<BpPropertyChanges> PropertyChanges)
            GetBpftItemDatas(Guid itemId)
        {
            (IEnumerable<GameCondition> Conditions, GameOperand UpperBound, GameOperand LowerBound, IEnumerable<BpPropertyChanges> PropertyChanges) result;
            if (_ItemId2Datas.TryGetValue(itemId, out result))
                return result;
            if (!_Id2Template.TryGetValue(itemId, out GameThingTemplateBase tmp) || !(tmp is BpItemTemplate item))
                return default;
            //条件
            var conditionals = new List<GameCondition>();
            GameCondition.FillFromString(conditionals, item.Conditional);
            result.Conditions = conditionals;
            //上限
            GameOperand upperBound = null; GameOperand.TryParse(item.CountUpperBound, out upperBound);
            result.UpperBound = upperBound;
            //下限
            GameOperand lowerBound = null; GameOperand.TryParse(item.CountLowerBound, out lowerBound);
            result.LowerBound = lowerBound;
            //属性更改
            var changes = new List<BpPropertyChanges>();
            BpPropertyChanges.FillFromString(changes, item.PropertiesChanges);
            result.PropertyChanges = changes;
            return result;
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

                for (int i = 0; i < datas.Count; i++)
                {
                    datas.Items.Clear();    //清除原料选择
                    datas.Incrementes.Clear();  //清楚数量计算
                    FillBpItems(datas); //填充物料表
                    ComputeIncrement(datas);    //计算原料是否够用
                    var formus = new List<BpFormulaTemplate>();
                    //过滤掉原料不足的公式
                    foreach (var formu in datas.Blueprint.FormulaTemplates)
                    {
                        bool succ = true;
                        foreach (var bpItem in formu.BptfItemTemplates)
                        {
                            if (!datas.Incrementes.TryGetValue(bpItem.Id, out var incs)) { succ = false; break; }   //找不到数量标注
                            var item = datas.Items.GetValueOrDefault(bpItem.Id);
                            if (null == item) { succ = false; break; }  //找不到对应原料
                            if (item.Count + incs.Item1 < 0) { succ = false; break; }  //数量不足
                        }
                        if (succ)    //若成功
                            formus.Add(formu);
                    }
                    if (0 == formus.Count)  //若已经没有符合条件的公式。
                    {
                        datas.DebugMessage = $"计划制造{datas.Count}次,实际成功{i}次后，原料不足";
                        break;
                    }
                    //计算公式转化
                    foreach (var formu in formus.OrderBy(c => c.OrderNumber))
                    {
                        //检验公式所需物料是否齐全
                        if (!formu.BptfItemTemplates.All(c => c.IsNew || datas.Items.ContainsKey(c.Id)))   //若公式物料不齐全
                            continue;
                        var prop = Convert.ToDouble(formu.GetProbObject().GetValue(new BpEnvironmentDatas(Service, datas.GameChar, null, datas.Items)));
                        if (!World.IsHit(prop))   //若未命中
                            continue;
                        //计算按该公式生成物品
                        if (!ApplyFormula(datas, formu))
                            return;
                        if (!formu.IsContinue)   //若不需要继续
                            break;
                    }
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

        /// <summary>
        /// 按公式计算反应结果。结果数量可能超过堆叠数量。
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="formula"></param>
        /// <returns>true成功反应，false,至少一项原料不足。</returns>
        protected bool ApplyFormula(ApplyBlueprintDatas datas, BpFormulaTemplate formula)
        {
            //检验公式所需物料是否齐全
            if (!formula.BptfItemTemplates.All(c => c.IsNew || datas.Items.ContainsKey(c.Id)))   //若公式物料不齐全
                return false;
            foreach (var item in formula.BptfItemTemplates)   //遍历每个物料清单
            {
                var data = item.GetBpItemDataObject();
                GameItem current;
                var env = new BpEnvironmentDatas(Service, datas.GameChar, null, datas.Items);
                if (item.IsNew) //若是增加的物品
                {
                    var coll = from tmp in data.PropertyChanges
                               let leftPath = tmp.Left.LeftPath
                               where leftPath.Count == 1 && leftPath[0] is string str && str.Equals("tid", StringComparison.InvariantCultureIgnoreCase)
                               let id = tmp.Right.GetValue(env)
                               where id is Guid
                               select new { tmp, id = (Guid)id };
                    var first = coll.FirstOrDefault();
                    if (null == first)
                    {
                        datas.DebugMessage = $"新生成物品必须指定TemplateId,但物料{item.Id}({item.DisplayName})中没有指定。";
                        datas.HasError = true;
                        return false;
                    }
                    var tid = (Guid)first.tmp.Right.GetValue(env);
                    current = World.ItemManager.CreateGameItem(tid);
                    datas.Items[item.Id] = current; //加入物料集合
                    env.Current = current;
                }
                else
                {
                    if (!datas.Items.TryGetValue(item.Id, out GameItem gameItem)) //若未找到原料对应物品
                    {
                        datas.DebugMessage = $"没有找到原料{item.Id}({item.DisplayName})对应的物品。";
                        datas.HasError = true;
                        return false;
                    }
                    env.Current = current = gameItem;
                }

                #region 更改数量
                if (!datas.Incrementes.TryGetValue(item.Id, out var incs))
                    ComputeIncrement(datas);
                incs = datas.Incrementes[item.Id];

                var inc = incs.Item3;  //增量
                decimal count = item.IsCountRound ? Math.Round(inc + (current.Count ?? 1)) : inc + (current.Count ?? 1);
                if (count < 0)
                {
                    datas.HasError = true;
                    datas.DebugMessage = "原料不足";
                    return false;
                }
                World.ItemManager.SetPropertyValue(current, "Count", count);
                #endregion  更改数量
                bool bChange = item.IsNew || inc != 0;   //是否有变化
                //应用属性
                if (count > 0) //若还存在
                {
                    foreach (var pc in data.PropertyChanges)
                    {
                        pc.Apply(env);
                        bChange = true;
                    }
                    var chn = new ChangesItem()
                    {
                        ContainerId = current.ParentId ?? current.OwnerId.Value,
                    };
                    chn.Changes.Add(current);
                }
                else //已经消失
                {
                    var chn = new ChangesItem()
                    {
                        ContainerId = current.ParentId ?? current.OwnerId.Value,
                    };
                    chn.Removes.Add(current.Id);
                    var gim = World.ItemManager;
                    var parent = current.Parent ?? (datas.GameChar as GameThingBase);
                    World.ItemManager.RemoveItemsWhere(parent, c => c.Id == current.Id);
                }
                if (bChange)    //若有了变化
                {
                    //记录变化物品
                    var chn = new ChangesItem()
                    {
                        ContainerId = current.ParentId ?? current.OwnerId.Value,
                    };
                    if (item.IsNew)  //若是新建物品
                        chn.Adds.Add(current);
                    else //若是变化物品
                        chn.Changes.Add(current);
                    datas.ChangesItem.Add(chn);
                }
            }
            return true;
        }


        /// <summary>
        /// 按蓝图和必定参加反应的物品填写物料字典。
        /// </summary>
        /// <param name="datas"></param>
        public void FillBpItems(ApplyBlueprintDatas datas)
        {
            var allItems = World.ObjectPoolListGameItem.Get();
            try
            {
                //保证提示数据优先选定
                allItems.AddRange(datas.GameItems);
                allItems.AddRange(OwHelper.GetAllSubItemsOfTree(datas.GameChar.GameItems, c => c.Children));
                var bpItems = datas.Blueprint.FormulaTemplates.SelectMany(c => c.BptfItemTemplates).Where(c => !c.IsNew).ToList();    //所有物料对象,排除新生物品
                var env = new BpEnvironmentDatas(Service, datas.GameChar, null, datas.Items);
                while (0 < bpItems.Count)    //当仍有需要匹配的物料对象
                {
                    bool succ = false;
                    for (int i = bpItems.Count - 1; i >= 0; i--)
                    {
                        var item = bpItems[i];
                        if (item.FindMatch(env, allItems))   //若找到匹配的物料
                        {
                            datas.Items[item.Id] = env.Current;
                            bpItems.RemoveAt(i);
                            succ = true;
                        }
                    }
                    if (!succ)
                        break;
                }
                if (bpItems.Count > 0)  //若物料填充不全
                                        //TO DO
                    ;
            }
            finally
            {
                World.ObjectPoolListGameItem.Return(allItems);
            }
        }

        /// <summary>
        /// 计算每个物料损耗增量。物料要填写完整，否则找不到则认为不足。此时忽略个别原料。
        /// </summary>
        /// <param name="datas">此时<see cref="ApplyBlueprintDatas.Items"/>要填写完毕。</param>
        public void ComputeIncrement(ApplyBlueprintDatas datas)
        {
            foreach (var item in datas.Blueprint.FormulaTemplates.SelectMany(c => c.BptfItemTemplates))
            {
                var current = datas.Items.GetValueOrDefault(item.Id);
                if (null == current)    //若找不到原料
                    continue;
                var data = item.GetBpItemDataObject();
                var env = new BpEnvironmentDatas(Service, datas.GameChar, current, datas.Items);

                #region 更改数量
                var lower = Convert.ToDouble(data.LowerBound.GetValue(env));
                var upper = Convert.ToDouble(data.UpperBound.GetValue(env));
                var inc = (decimal)(lower + VWorld.WorldRandom.NextDouble() * Math.Abs(upper - lower));  //增量
                if (item.IsCountRound)   //若需要取整
                    datas.Incrementes[item.Id] = ((decimal)Math.Round(lower), (decimal)Math.Round(upper), Math.Round(inc));
                else
                    datas.Incrementes[item.Id] = ((decimal)lower, (decimal)upper, inc);
                if (current.Count + (decimal)Math.Min(lower, upper) < 0)    //若原料数量不足
                {
                    datas.HasError = true;
                    datas.DebugMessage += $"原料{item.Id}({item.DisplayName})数量不足。{Environment.NewLine}";
                }
                #endregion  更改数量
            }
        }

    }

}
