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

    public sealed class BpItemDataObject
    {

        public BpItemDataObject(IEnumerable<GameCondition> conditions, GameStringProperty upperBound, GameStringProperty lowerBound, IEnumerable<BpPropertyChanges> propertyChanges)
        {
            Conditions = conditions;
            UpperBound = upperBound;
            LowerBound = lowerBound;
            PropertyChanges = propertyChanges;
        }

        public readonly IEnumerable<GameCondition> Conditions;
        public readonly GameStringProperty UpperBound;
        public readonly GameStringProperty LowerBound;
        public readonly IEnumerable<BpPropertyChanges> PropertyChanges;
    }

    /// <summary>
    /// 获取复杂属性的运行时值的环境数据。
    /// </summary>
    public sealed class BpEnvironmentDatas
    {
        public BpEnvironmentDatas(IServiceProvider service, GameChar gameChar, GameItem current, IDictionary<Guid, GameItem> gameItems)
        {
            _Service = service;
            _GameChar = gameChar;
            _Current = current;
            _GameItems = gameItems;
        }

        private IServiceProvider _Service;
        public IServiceProvider Service => _Service;

        private GameChar _GameChar;
        public GameChar GameChar => _GameChar;

        GameItem _Current;
        public GameItem Current => _Current;

        IDictionary<Guid, GameItem> _GameItems;
        public IDictionary<Guid, GameItem> GameItems => _GameItems;

    }

    public static class BlueprintExtensions
    {
        private readonly static ConcurrentDictionary<Guid, GameStringProperty> _FormulaId2ProbObject = new ConcurrentDictionary<Guid, GameStringProperty>();
        public static GameStringProperty GetProbObject(this BpFormulaTemplate formulaTemplate)
        {
            return _FormulaId2ProbObject.GetOrAdd(formulaTemplate.Id, c =>
            {
                if (GameStringProperty.TryParse(formulaTemplate.Prob, out GameStringProperty result))
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
            BpItemDataObject result;
            if (_ItemId2Datas.TryGetValue(bpItem.Id, out result))  //若已经缓存
                return result;
            //条件
            var conditionals = new List<GameCondition>(); GameCondition.FillFromString(conditionals, bpItem.Conditional);
            //上限
            GameStringProperty upperBound = null; GameStringProperty.TryParse(bpItem.CountUpperBound, out upperBound);
            //下限
            GameStringProperty lowerBound = null; GameStringProperty.TryParse(bpItem.CountLowerBound, out lowerBound);
            //属性更改
            var changes = new List<BpPropertyChanges>(); BpPropertyChanges.FillFromString(changes, bpItem.PropertiesChanges);
            //
            return _ItemId2Datas.GetOrAdd(bpItem.Id, new BpItemDataObject(conditionals, upperBound, lowerBound, changes));
        }

        /// <summary>
        /// 获取物料与指定物品是否匹配，不考虑同名属性因素。
        /// </summary>
        /// <param name="bpItem"></param>
        /// <param name="gameItem"></param>
        /// <returns></returns>
        public static bool IsMatch(this BpItemTemplate bpItem, GameItem gameItem)
        {
            return true;
        }

    }

    /// <summary>
    /// 使用蓝图的数据。
    /// </summary>
    public class ApplyBluprintDatas
    {
        public ApplyBluprintDatas()
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
        /// 键是物料模板Id,值对应的物料对象。
        /// </summary>
        public Dictionary<Guid, GameItem> Items { get; } = new Dictionary<Guid, GameItem>();

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

    public class GameStringProperty
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
        public static bool TryParse(string str, out GameStringProperty result)
        {
            str = str.Trim();  //忽略空白
            bool succ = false;
            result = null;
            if (TryGetConst(str, out object obj))   //若是常量
            {
                result = new GameStringProperty()
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
                result = new GameStringProperty()
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
        public GameStringProperty()
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
            var gim = datas.Service.GetRequiredService<GameItemManager>();
            var leftVal = gim.GetPropertyValue(gameItem, propName);
            if (RightSequence.Count == 0)   //若没有序列
                return leftVal;
            if (!(leftVal is decimal dec))
                return null;
            var index = (int)Math.Round(dec);
            if (index >= RightSequence.Count)
                return null;
            return RightSequence[index];
        }

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
    }

    /// <summary>
    /// 条件对象。
    /// </summary>
    public class GameCondition
    {
        static private readonly string comparePattern = @"(?<left>[^\=\<\>]+)(?<op>[\=\<\>]{1,2})(?<right>[^\=\<\>]+)[\,，]?";

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
                if (GameStringProperty.TryParse(item.Groups["left"].Value, out GameStringProperty gameStringProperty))
                    tmp.Left = gameStringProperty;
                if (GameStringProperty.TryParse(item.Groups["right"].Value, out gameStringProperty))
                    tmp.Right = gameStringProperty;
                conditions.Add(tmp);
            }
            return;
        }

        #region 属性及相关

        public GameStringProperty Left { get; set; }

        public GameStringProperty Right { get; set; }

        public string Operator { get; set; }

        #endregion 属性及相关

        public bool GetValue(BpEnvironmentDatas datas)
        {
            object left = Left.GetValue(datas);
            object right = Left.GetValue(datas);
            int? cr = null;
            if (left == right)
                return Operator switch
                {
                    "==" => true,
                    "=" => true,
                    "!=" => false,
                    "<>" => false,
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
                return Operator switch
                {
                    "==" => false,
                    "=" => false,

                    "!=" => true,
                    "<>" => true,
                    _ => throw new ArgumentOutOfRangeException(),
                };
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
            else
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
                if (GameStringProperty.TryParse(item.Groups["left"].Value, out GameStringProperty stringProperty))
                {
                    tmp.Left = stringProperty;
                    if (tmp.Left.IsConst)
                        Debug.WriteLine("发现试图对常量赋值。");
                }
                if (GameStringProperty.TryParse(item.Groups["right"].Value, out stringProperty))
                    tmp.Right = stringProperty;
                changes.Add(tmp);
            }
        }

        public BpPropertyChanges()
        {

        }


        public GameStringProperty Left { get; set; }

        public GameStringProperty Right { get; set; }

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
                    ov = (decimal)Left.GetValue(datas);
                    nv = (decimal)Right.GetValue(datas);
                    Left.SetValue(datas, ov + nv);
                    break;
                case "-":
                    ov = (decimal)Left.GetValue(datas);
                    nv = (decimal)Right.GetValue(datas);
                    Left.SetValue(datas, ov - nv);
                    break;
                case "*":
                    ov = (decimal)Left.GetValue(datas);
                    nv = (decimal)Right.GetValue(datas);
                    Left.SetValue(datas, ov * nv);
                    break;
                case "/":
                    ov = (decimal)Left.GetValue(datas);
                    nv = (decimal)Right.GetValue(datas);
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

        public Func<IServiceProvider, ApplyBluprintDatas, bool> DoApply { get; set; }
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
        public Dictionary<Guid, GameThingTemplateBase> Id2Template { get => _Id2Template; }

        private ConcurrentDictionary<Guid, GameStringProperty> _FormulaId2SequencePropertyData = new ConcurrentDictionary<Guid, GameStringProperty>();
        /// <summary>
        /// 获取公式的命中概率计算类。
        /// </summary>
        /// <param name="formulaId">公式的Id。</param>
        /// <returns></returns>
        public GameStringProperty GetSequencePropertyData(Guid formulaId)
        {
            _InitializeTask?.Wait();
            if (_FormulaId2SequencePropertyData.TryGetValue(formulaId, out GameStringProperty result))    //若已经缓存该公式的命中概率计算类
                return result;
            if (!_Id2Template.TryGetValue(formulaId, out GameThingTemplateBase thingTemplateBase) || !(thingTemplateBase is BpFormulaTemplate formula))   //若没有指定Id的公式
                return null;
            GameStringProperty.TryParse(formula.Prob, out result);
            _FormulaId2SequencePropertyData[formulaId] = result;
            return result;
        }

        private ConcurrentDictionary<Guid, (IEnumerable<GameCondition> Conditions, GameStringProperty UpperBound, GameStringProperty LowerBound, IEnumerable<BpPropertyChanges> PropertyChanges)>
            _ItemId2Datas = new ConcurrentDictionary<Guid, (IEnumerable<GameCondition> Conditions, GameStringProperty UpperBound, GameStringProperty LowerBound, IEnumerable<BpPropertyChanges> PropertyChanges)>();

        /// <summary>
        /// 获取物料项辅助计算对象。
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public (IEnumerable<GameCondition> Conditions, GameStringProperty UpperBound, GameStringProperty LowerBound, IEnumerable<BpPropertyChanges> PropertyChanges)
            GetBpftItemDatas(Guid itemId)
        {
            (IEnumerable<GameCondition> Conditions, GameStringProperty UpperBound, GameStringProperty LowerBound, IEnumerable<BpPropertyChanges> PropertyChanges) result;
            if (_ItemId2Datas.TryGetValue(itemId, out result))
                return result;
            if (!_Id2Template.TryGetValue(itemId, out GameThingTemplateBase tmp) || !(tmp is BpItemTemplate item))
                return default;
            //条件
            var conditionals = new List<GameCondition>();
            GameCondition.FillFromString(conditionals, item.Conditional);
            result.Conditions = conditionals;
            //上限
            GameStringProperty upperBound = null; GameStringProperty.TryParse(item.CountUpperBound, out upperBound);
            result.UpperBound = upperBound;
            //下限
            GameStringProperty lowerBound = null; GameStringProperty.TryParse(item.CountLowerBound, out lowerBound);
            result.LowerBound = lowerBound;
            //属性更改
            var changes = new List<BpPropertyChanges>();
            BpPropertyChanges.FillFromString(changes, item.PropertiesChanges);
            result.PropertyChanges = changes;
            return result;
        }

        /// <summary>
        /// 按物料项过滤匹配的虚拟物品。不考虑同名属性值要相同的问题。
        /// </summary>
        /// <param name="src"></param>
        /// <param name="bptfItem"></param>
        /// <returns></returns>
        public IEnumerable<GameItem> filter(IEnumerable<GameItem> src, BpItemTemplate bptfItem)
        {
            var result = src;
            if (bptfItem.Properties.TryGetValue("tid", out object tmpObject) && tmpObject is string tmpString && Guid.TryParse(tmpString, out Guid tid))   //若需要约束模板Id
                result = result.Where(c => c.TemplateId == tid);
            if (bptfItem.Properties.TryGetValue("ptid", out tmpObject) && tmpObject is string tmpString1 && Guid.TryParse(tmpString1, out Guid ptid))   //若需要约束模板Id
                result = result.Where(c => (!c.ParentId.HasValue && ptid == c.OwnerId) || ptid == c.Parent?.TemplateId);
            var item = GetBpftItemDatas(bptfItem.Id);
            //result = result.Where(c => item.Conditions.All(c1 => c1.GetResult(c.Properties)));
            return result;
        }

        public void ApplyBluprint(ApplyBluprintDatas datas)
        {
            _InitializeTask.Wait();
            //if (!World.CharManager.Lock(datas.GameChar.GameUser))    //若无法锁定用户
            //{
            //    datas.HasError = true;
            //    datas.DebugMessage = $"指定用户无效。";
            //    return;
            //}
            //try
            //{
            //    if (datas.GameItems.Count != 1)
            //    {
            //        datas.HasError = true;
            //        datas.DebugMessage = $"目标对象过多";
            //        return;
            //    }
            //    var objectId = datas.GameItems[0];
            //    var slot = datas.GameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShenWenSlotId);  //神纹装备槽
            //    var obj = slot.Children.FirstOrDefault(c => c.Id == objectId);    //目标物品对象,目前是神纹
            //    if (null == obj)    //找不到指定的目标物品
            //    {
            //        datas.HasError = true;
            //        datas.DebugMessage = $"找不到指定的目标物品。";
            //        return;
            //    }

            //    if (datas.BlueprintId == ProjectConstant.ShenwenLvUpBlueprint)  //若要进行神纹升级
            //    {
            //        var info = GetShenwenInfo(datas.GameChar, obj);
            //        if (info.Level + datas.Count > info.MaxLv)
            //        {
            //            datas.HasError = true;
            //            datas.DebugMessage = $"已达最大等级或升级次数过多。";
            //            return;
            //        }
            //        var daojuSlot = datas.GameChar.GameItems.First(c => c.TemplateId == ProjectConstant.DaojuBagSlotId);
            //        //var suipian = from tmp in daojuSlot.Children
            //        //              let gid=

            //        var lv = Convert.ToInt32(obj.Properties[ProjectConstant.LevelPropertyName]);

            //    }
            //    else if (datas.BlueprintId == ProjectConstant.ShenWenTupoBlueprint) //若要进行神纹突破
            //    {
            //    }
            //    else
            //    {
            //        datas.HasError = true;
            //        datas.DebugMessage = $"找不到指定蓝图的Id:{datas.BlueprintId}";
            //    }
            //}
            //finally
            //{
            //    World.CharManager.Unlock(datas.GameChar.GameUser);
            //}
        }

        /// <summary>
        /// 按蓝图和必定参加反应的物品填写物料字典。
        /// </summary>
        /// <param name="datas"></param>
        public void FillBpItems(ApplyBluprintDatas datas)
        {
            var allItems = datas.GameItems.Concat(OwHelper.GetAllSubItemsOfTree(datas.GameItems, c => c.Children)); //保证优先选择指定的数据
            foreach (var formu in datas.Blueprint.FormulaTemplates) //遍历所有公式
            {
                foreach (var item in formu.BptfItemTemplates)   //遍历每个物料
                {
                    if (item.IsNew)  //若是新建
                        continue;
                    filter(allItems, item).Join(datas.GameItems, c => c.Id, c => c.Id, (c1, c2) => c1); //符合条件的必用物品

                }
            }
        }

        public (int Level, int MaxLv, int TupoCount) GetShenwenInfo(GameChar gameChar, GameItem shenwen)
        {
            var tt = World.ItemTemplateManager.GetTemplateFromeId(shenwen.TemplateId);  //模板
            int lv = 0; //等级
            if (!shenwen.Properties.TryGetValue(ProjectConstant.LevelPropertyName, out object lvObj) && lvObj is int lvVal)
                lv = lvVal;
            int tp = 0; //突破次数
                        //if (!shenwen.Properties.TryGetValue(ProjectConstant.ShenwenTupoCountPropertyName, out object tpObj) && lvObj is int tpVal)
                        //    tp = tpVal;
                        //else
                        //    shenwen.Properties[ProjectConstant.ShenwenTupoCountPropertyName] = (decimal)tp; //加入属性
            int maxLv;  //当前突破次数下最大等级。
            BlueprintTemplate tbp = Id2BlueprintTemplate[ProjectConstant.ShenWenTupoBlueprint];
            var seq = (decimal[])tbp.Properties["ssl"];
            maxLv = (int)seq[lv];
            return (lv, maxLv, tp);
        }
    }
}
