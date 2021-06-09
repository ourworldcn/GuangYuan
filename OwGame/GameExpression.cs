﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace OwGame.Expression
{

    /// <summary>
    /// 给对象设置属性的帮助器。
    /// 因该作为全局的服务之一。
    /// </summary>
    public abstract class GamePropertyHelper
    {
        /// <summary>
        /// 获取对象的属性、
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual object GetValue(object obj, string propertyName, object defaultValue = default)
        {
            var pd = TypeDescriptor.GetProperties(obj).OfType<PropertyDescriptor>().FirstOrDefault(c => c.Name == propertyName);
            return null == pd ? defaultValue : pd.GetValue(obj) ?? defaultValue;
        }

        /// <summary>
        /// 设置对象的属性。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="val"></param>
        /// <returns>true成功设置，false未能成功设置。</returns>
        public abstract bool SetValue(object obj, string propertyName, object val);
        //{
        //    var _ = obj as GameThingBase;
        //    var dic = _?.Properties;
        //    dic[propertyName] = val;
        //    return true;
        //}
    }

    /// <summary>
    /// 运行时环境。
    /// </summary>
    public class GameExpressionRuntimeEnvironment
    {
        public GameExpressionRuntimeEnvironment()
        {
            _Variables = new Dictionary<string, GameExpressionBase>();
        }

        public GameExpressionRuntimeEnvironment(GameExpressionCompileEnvironment env)
        {
            Services = env.Services;
            var dic = Variables;
            _Variables = new Dictionary<string, GameExpressionBase>(env.Variables);
        }

        private Dictionary<string, GameExpressionBase> _Variables;
        /// <summary>
        /// 变量。
        /// </summary>
        public Dictionary<string, GameExpressionBase> Variables => _Variables;
        private readonly Stack<Dictionary<string, GameExpressionBase>> StackVariables = new Stack<Dictionary<string, GameExpressionBase>>();

        /// <summary>
        /// 获取指定名称变量的运行时值。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns>true成功获取，此时<paramref name="result"/>中有该值，false未能找到变量。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetVariableValue(string name, out object result)
        {
            if (!Variables.TryGetValue(name, out var varExpr) || null == varExpr)
            {
                result = default;
                return false;
            }
            return varExpr.TryGetValue(this, out result);
        }

        /// <summary>
        /// 获取指定名称变量的运行时值，且试图转换未一个decimal值。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetVariableDecimal(string name, out decimal result)
        {
            if (!TryGetVariableValue(name, out var obj) || !OwHelper.TryGetDecimal(obj, out result))
            {
                result = default; return false;
            }
            return true;
        }

        //public bool SettVariableValue(string name ,GameExpressionBase val)
        //{
        //}

        ///// <summary>
        ///// 参数。
        ///// </summary>
        //public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        /// <summary>
        /// 服务。
        /// </summary>
        public IServiceProvider Services { get; set; }

        /// <summary>
        /// 开始一个范围，复制原有范围内所有变量。
        /// </summary>
        public void StartScope()
        {
            var dic = new Dictionary<string, GameExpressionBase>(Variables);
            StackVariables.Push(_Variables);
            _Variables = dic;
        }

        /// <summary>
        /// 退出范围，可选是否将此范围的变量合并到新范围中。
        /// </summary>
        /// <param name="merge"></param>
        /// <returns></returns>
        public bool EndScope(bool merge = false)
        {
            if (!StackVariables.TryPop(out var dic))
                return false;
            if (merge)   //若需要合并
            {
                foreach (var item in Variables)
                    dic[item.Key] = item.Value;
            }
            _Variables = dic;
            return true;
        }
    }

    /// <summary>
    /// 编译环境类。
    /// </summary>
    public class GameExpressionCompileEnvironment
    {
        public GameExpressionCompileEnvironment()
        {

        }

        public IServiceProvider Services { get; set; }

        private string _CurrentObjectId;
        public string CurrentObjectId { get => _CurrentObjectId; set => _CurrentObjectId = value; }


        private readonly Dictionary<string, GameExpressionBase> _Variables = new Dictionary<string, GameExpressionBase>();
        /// <summary>
        /// 变量声明。
        /// </summary>
        public Dictionary<string, GameExpressionBase> Variables => _Variables;


        private Stack<string> _CurrentObjectIds = new Stack<string>();

        /// <summary>
        /// 设置一个新的当前对象Id。并保存旧Id,在以后可以用<see cref="RestoreCurrentObject(out string)"/>恢复。
        /// </summary>
        /// <param name="newObjectId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartCurrentObject(string newObjectId)
        {
            _CurrentObjectIds.Push(_CurrentObjectId);
            _CurrentObjectId = newObjectId;
        }

        /// <summary>
        /// 返回上一个当前对象Id。
        /// </summary>
        /// <param name="oldObjectId">返回true时,这里是原有对象Id,返回false时，这个出参的状态未知。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RestoreCurrentObject(out string oldObjectId)
        {
            oldObjectId = _CurrentObjectId;
            return _CurrentObjectIds.TryPop(out _CurrentObjectId);
        }

        /// <summary>
        /// 用于同步锁定的对象。
        /// </summary>
        public object ThisLocker => _CurrentObjectIds;
    }

    public abstract class GameExpressionBase
    {

        public GameExpressionBase()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="env"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetValueOrDefault(GameExpressionRuntimeEnvironment env, object defaultValue = null) => TryGetValue(env, out var result) ? result : defaultValue;

        public abstract bool TryGetValue(GameExpressionRuntimeEnvironment env, out object result);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="env"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public abstract bool SetValue(GameExpressionRuntimeEnvironment env, object val);

        #region 静态成员

        #region 运行时相关成员

        static private readonly Random Random = new Random();
        /// <summary>
        /// 公用<see cref="Random"/>种子的互斥锁。
        /// </summary>
        static private SpinLock _RandomLocker = new SpinLock();

        /// <summary>
        /// 返回一个[0,1)区间内的双精度浮点数。支持多线程并发调用。
        /// </summary>
        /// <returns></returns>
        static public double NextDouble()
        {
            bool gotLock = false;
            _RandomLocker.Enter(ref gotLock);
            try
            {
                return Random.NextDouble();

            }
            finally
            {
                if (gotLock) _RandomLocker.Exit();
            }
        }

        /// <summary>
        /// 获取一个整数。
        /// </summary>
        /// <param name="max">小于该数且大于或等于0的一个随机数。</param>
        /// <returns></returns>
        static public int Next(int max)
        {
            bool gotLock = false;
            _RandomLocker.Enter(ref gotLock);
            try
            {
                return Random.Next(max);

            }
            finally
            {
                if (gotLock) _RandomLocker.Exit();
            }
        }

        #endregion 运行时相关成员

        #region 编译时相关成员

        /// <summary>
        /// 编译变量声明。
        /// </summary>
        /// <param name="env"></param>
        /// <param name="inputs"></param>
        /// <returns></returns>
        static public void CompileVariableDeclare(GameExpressionCompileEnvironment env, string inputs)
        {
            if (string.IsNullOrEmpty(inputs))
                return;
            Dictionary<string, GameExpressionBase> result = env.Variables;
            var alls = inputs.Split(OwHelper.CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Split('=', StringSplitOptions.None).Select(exp => exp.Trim()).ToArray());
            foreach (var expStr in alls.Where(c => c.Length != 2))
            {
                Debug.WriteLine($"检测到不合规的变量声明——{string.Join('=', expStr)}");
            }
            foreach (var item in alls.Where(c => c.Length == 2))    //提取所有变量名
                result[item[0]] = null;
            foreach (var item in alls.Where(c => c.Length == 2))
            {
                result[item[0]] = CompileVariableInit(env, item[1]);
            }
            return;
        }

        /// <summary>
        /// 编译变量初始化。
        /// </summary>
        /// <param name="env"></param>
        /// <param name="inputs"></param>
        /// <returns></returns>
        static public GameExpressionBase CompileVariableInit(GameExpressionCompileEnvironment env, string inputs)
        {
            GameExpressionBase result = null;
            inputs = inputs.Trim();
            if (string.IsNullOrEmpty(inputs))
                return ConstGExpression.Null;
            if (inputs.Contains('|'))    //若可能是数组
                result = MakeArray(env, inputs);
            else //若是其他简单操作数
                result = MakeOperand(env, inputs);
            return result;
        }

        /// <summary>
        /// 获取一个表示数组的表达式。
        /// </summary>
        /// <param name="env"></param>
        /// <param name="inputs"></param>
        /// <returns></returns>
        static public ArrayGExpression MakeArray(GameExpressionCompileEnvironment env, string inputs)
        {
            var elements = inputs.Split('|', StringSplitOptions.None).Select(c => ConstOrReference(env, c));
            ArrayGExpression result = new ArrayGExpression(elements);
            return result;
        }

        /// <summary>
        /// 分析常量和变量引用。
        /// </summary>
        /// <param name="env"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public GameExpressionBase ConstOrReference(GameExpressionCompileEnvironment env, string str)
        {
            return ConstGExpression.TryParse(str, out var result) ? (GameExpressionBase)result : new ReferenceGExpression(str.Trim()/*这里要考虑空白是否有意义 TO DO*/, env.CurrentObjectId);
        }

        const string comparePattern = @"\s*(?<or>{dec}|[^{}]+)\s*(?<op>[{}]{0,2})";
        const string decPattern = @"\-?[\d\.]+";
        private static string _Pattern;

        static protected string PatternString
        {
            get
            {
                if (null == _Pattern)
                {
                    var tmp = string.Concat(BinaryGExpression.Operators.Keys.SelectMany(c => c).Distinct().Select(c => @"\" + char.ToString(c)));
                    _Pattern = comparePattern.Replace(@"{}", tmp).Replace("{dec}", decPattern);
                }
                return _Pattern;
            }

        }

        /// <summary>
        /// 分析函数，数组元素引用，变量引用和常量。
        /// </summary>
        /// <param name="env"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        static public GameExpressionBase MakeOperand(GameExpressionCompileEnvironment env, string inputs)
        {
            inputs = inputs.Trim();
            if (string.IsNullOrEmpty(inputs))
                return ConstGExpression.Null;
            GameExpressionBase result;
            if (inputs.Contains('(') && inputs.EndsWith(')'))    //若可能是函数
            {
                var items = inputs.Split('(');
                if (items.Length == 2) //若是函数调用
                {
                    var paras = items[1].TrimEnd(')').Trim();   //参数列表
                    if (string.IsNullOrEmpty(paras)) //若是空列表
                        result = new FunctionCallGExpression(items[0], Array.Empty<GameExpressionBase>() as IEnumerable<GameExpressionBase>);
                    else
                    {
                        var _ = paras.Split(OwHelper.CommaArrayWithCN).Select(c => ConstOrReference(env, c));
                        result = new FunctionCallGExpression(items[0], _);
                    }
                }
                else
                    result = null;
            }
            else if (inputs.Contains('[') && inputs.EndsWith(']'))    //若可能是数组元素访问
            {
                var items = inputs.Split('[');
                if (items.Length == 2) //若是函数调用
                {
                    var _ = ConstOrReference(env, items[1].TrimEnd(']'));
                    result = new ArrayElementGExpression(ConstOrReference(env, items[0]), _);
                }
                else
                    result = null;
            }
            else
                result = ConstOrReference(env, inputs);
            return result;
        }

        /// <summary>
        /// 编译表达式组。
        /// </summary>
        /// <param name="env"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        static public BlockGExpression CompileBlockExpression(GameExpressionCompileEnvironment env, string str)
        {
            var coll = str.Split(OwHelper.CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries);
            var para = coll.Select(c => CompileExpression(env, c));
            return new BlockGExpression(para);
        }

        /// <summary>
        /// 编译表达式。
        /// </summary>
        /// <param name="env"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        static public GameExpressionBase CompileExpression(GameExpressionCompileEnvironment env, string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return ConstGExpression.Null;
            var matchs = Regex.Matches(str, PatternString);
            List<string> operandList = new List<string>(); //操作数
            List<string> operatorList = new List<string>();    //操作符
            foreach (var item in matchs.OfType<Match>())    //分解字符串
            {
                if (!item.Success)
                    continue;
                operandList.Add(item.Groups["or"].Value);
                var op = item.Groups["op"]?.Value;
                if (!string.IsNullOrWhiteSpace(op))
                    operatorList.Add(item.Groups["op"].Value);
            }
            //构造表达式树
            operandList.Reverse();
            Stack<string> operands = new Stack<string>(operandList);
            operatorList.Reverse();
            Stack<string> operators = new Stack<string>(operatorList);
            return CompileExpression(env, operands, operators);
        }

        /// <summary>
        /// 编译表达式。
        /// </summary>
        /// <param name="operators"></param>
        /// <param name="operands">操作数</param>
        /// <param name="opt">操作符</param>
        static public GameExpressionBase CompileExpression(GameExpressionCompileEnvironment env, Stack<string> operands, Stack<string> operators)
        {
            var left = MakeOperand(env, operands.Pop());
            if (operators.Count <= 0)   //若没有操作符
                return left;
            var opt = operators.Pop();
            while (true)
            {
                if (!operators.TryPeek(out var optNext))    //若已经没有操作符
                    return new BinaryGExpression(left, opt, MakeOperand(env, operands.Pop()));
                else //若还有操作符
                {
                    if (OperatorCompareTo(opt, optNext) >= 0)   //若当前操作符优先级大于或等于下一个操作符
                    {
                        left = new BinaryGExpression(left, opt, MakeOperand(env, operands.Pop()));
                        opt = operators.Pop();
                    }
                    else
                    {
                        left = new BinaryGExpression(left, opt, CompileExpression(env, operands, operators));
                        break;
                    }
                }
            }
            return left;
        }

        static int GetPriority(string op)
        {
            BinaryGExpression.Operators.TryGetValue(op, out var result);
            return result;
        }

        /// <summary>
        /// 比较两个运算符的优先级。
        /// </summary>
        /// <param name="op1"></param>
        /// <param name="op2"></param>
        /// <returns></returns>
        static int OperatorCompareTo(string op1, string op2)
        {
            return GetPriority(op1).CompareTo(GetPriority(op2));
        }
        #endregion 编译时相关成员

        #endregion 静态成员
    }


    #region 基础类型

    /// <summary>
    /// 函数调用的。
    /// </summary>
    public class FunctionCallGExpression : GameExpressionBase
    {
        private string _Name;

        public FunctionCallGExpression()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="name">函数名。</param>
        /// <param name="parameters">参数列表。</param>
        public FunctionCallGExpression(string name, IEnumerable<GameExpressionBase> parameters)
        {
            Name = name;
            Parameters.AddRange(parameters);
        }

        public FunctionCallGExpression(string name, params GameExpressionBase[] parameters)
        {
            Name = name;
            Parameters.AddRange(parameters);
        }


        /// <summary>
        /// 函数名，不区分大小写，一律视同全小写。设置的值中有大写字母，也会自动转换为小写。且忽略空白。
        /// </summary>
        public string Name { get => _Name; set => _Name = value.Trim().ToLower(); }

        public List<GameExpressionBase> Parameters { get; } = new List<GameExpressionBase>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool SetValue(GameExpressionRuntimeEnvironment env, object val)
        {
            return false;
        }

        /// <summary>
        /// 缓存随机数计算，避免多次计算。
        /// </summary>
        private decimal? _CacheRandom = null;

        public override bool TryGetValue(GameExpressionRuntimeEnvironment env, out object result)
        {
            bool succ = false;
            switch (Name)
            {
                case "rnd": //生成[0,1)之间的随机数
                    Debug.Assert(Parameters.Count == 0, "rnd函数不需要参数");
                    _CacheRandom ??= Convert.ToDecimal(NextDouble());
                    result = _CacheRandom.Value;
                    succ = true;
                    break;
                case "rndi":    //生成[0,max)区间内的随机整数。
                    if (_CacheRandom is null)
                    {
                        var maxObj = Parameters.SingleOrDefault()?.GetValueOrDefault(env);
                        if (maxObj == null || !OwHelper.TryGetDecimal(maxObj, out var dec))
                            result = default;
                        else
                        {
                            result = Next(Convert.ToInt32(Math.Round(dec)));
                            succ = true;
                        }
                    }
                    result = _CacheRandom.Value;
                    break;
                case "round":   //取整
                    var paraExpr = Parameters.SingleOrDefault();
                    if (null != paraExpr && paraExpr.TryGetValue(env, out result) && OwHelper.TryGetDecimal(result, out var decVal))
                    {
                        result = Math.Round(decVal);
                        succ = true;
                    }
                    else
                        result = default;
                    break;
                case "rnds":
                    if (_CacheRandom is null)
                    {
                        if (Parameters.Count % 3 != 0 || Parameters.Count <= 0)   //若参数个数不是3的倍数，或没有参数
                        {
                            result = default;
                        }
                        else //若参数正确
                        {
                            List<ValueTuple<decimal, decimal, decimal>> lst = new List<(decimal, decimal, decimal)>();
                            var probNum = 0m;    //概率数
                            for (int i = 0; i < Parameters.Count / 3; i++)
                            {
                                var succ1 = OwHelper.TryGetDecimal(Parameters[i]?.GetValueOrDefault(env), out var d1);
                                var succ2 = OwHelper.TryGetDecimal(Parameters[i + 1]?.GetValueOrDefault(env), out var d2);
                                var succ3 = OwHelper.TryGetDecimal(Parameters[i + 2]?.GetValueOrDefault(env), out var d3);
                                if (!(succ1 && succ2 && succ3))
                                {
                                    result = default;
                                    succ = false;
                                    break;
                                }
                                probNum += d1;
                                lst.Add((probNum, d2, d3));
                                succ = true;
                            }
                            if (!succ)  //若参数有错误
                            {
                                result = default;
                                break;
                            }
                            var fac = lst.Sum(c => c.Item1) * (decimal)NextDouble();  //缩放后的因子
                            var item = lst.First(c => c.Item1 > fac);   //命中项
                            _CacheRandom = item.Item2 + (item.Item3 - item.Item2) * (decimal)NextDouble();
                            succ = true;
                        }
                    }
                    result = _CacheRandom.Value;
                    break;
                case "lerp":
                    if (Parameters.Count != 3 || !OwHelper.TryGetDecimal(Parameters[0], out var value1) || !OwHelper.TryGetDecimal(Parameters[1], out var value2) ||
                        !OwHelper.TryGetDecimal(Parameters[0], out var amount) || amount < 0 || amount > 1)   //若参数个数不是3个数值或比重不在[0,1]区间内
                    {
                        result = default;
                    }
                    else
                    {
                        result = value1 + amount * (value2 - value1);
                        //result = value1 * (1 - amount) + amount * value2;
                        succ = true;
                    }
                    break;
                default:
                    result = default;
                    break;
            }
            return succ;
        }
    }

    /// <summary>
    /// 常量。
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ConstGExpression : GameExpressionBase
    {
        /// <summary>
        /// 空引用。
        /// </summary>
        static readonly public ConstGExpression Null = new ConstGExpression(null);

        static public bool TryParse(string str, out ConstGExpression result)
        {
            str = str.Trim();
            if (string.IsNullOrEmpty(str))
            {
                result = Null;
                return true;
            }

            if (str.StartsWith('"') && str.EndsWith('"'))  //若明确是字符串
            {
                result = new ConstGExpression(str.Trim('"'));
            }
            else if (decimal.TryParse(str, out var dec))    //若是一个数字
                result = new ConstGExpression(dec);
            else if (Guid.TryParse(str, out var guid))  //若是Guid
                result = new ConstGExpression(guid);
            else //若不是一个明确的常量
            {
                result = null;
                return false;
            }
            return true;

        }

        private object _Value;
        public object Value { get => _Value; }

        public ConstGExpression(object value)
        {
            _Value = value;
        }

        public ConstGExpression()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool SetValue(GameExpressionRuntimeEnvironment env, object val)
        {
            Debug.WriteLineIf(val is GameExpressionBase, $"不应在常量对象中引用另一个常量对象。");
            _Value = val;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool TryGetValue(GameExpressionRuntimeEnvironment env, out object result)
        {
            result = Value;
            return true;
        }

        private string GetDebuggerDisplay()
        {
            if (_Value is Guid)
            {
                var str = _Value.ToString();
                return $"{{{str.Substring(0, 4)}...{str.Substring(str.Length - 4, 4)}}}";
            }
            return $"{Value}";
        }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ReferenceGExpression : GameExpressionBase
    {
        public ReferenceGExpression()
        {

        }

        public string Name { get; set; }
        public string ObjectId { get; }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="name">引用变量或属性的名称。在这里空白被认为是有意义的。</param>
        /// <param name="objectId">对象Id，当取属性值时，用此Id寻找对象，在这里空白被认为是有意义的。</param>
        public ReferenceGExpression(string name, string objectId)
        {
            Name = name;
            ObjectId = objectId;
        }

        public override bool SetValue(GameExpressionRuntimeEnvironment env, object val)
        {
            if (env.Variables.TryGetValue(Name, out var expr))   //若此引用是一个常量
            {
                return expr.SetValue(env, val);
            }
            if (!env.Variables.TryGetValue(ObjectId, out var objExp) || !objExp.TryGetValue(env, out var obj))   //若没有找到对象
                return false;
            var srv = env.Services.GetService(typeof(GamePropertyHelper)) as GamePropertyHelper;
            return srv.SetValue(obj, Name, val);
        }

        /// <summary>
        /// 试图获取变量的值。
        /// </summary>
        /// <param name="env"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetVariablesValue(GameExpressionRuntimeEnvironment env, out object result)
        {
            return env.TryGetVariableValue(Name, out result);
        }

        public override bool TryGetValue(GameExpressionRuntimeEnvironment env, out object result)
        {
            bool succ = TryGetVariablesValue(env, out result);
            if (succ)   //若是变量
                return succ;
            if (!env.Variables.TryGetValue(ObjectId, out var obj) || obj is null)  //若找不到对象
            {
                result = default;
            }
            else //若找到了对象
            {
                var gph = env.Services.GetService(typeof(GamePropertyHelper)) as GamePropertyHelper;

                if (!obj.TryGetValue(env, out var tmp) || tmp == null)  //若未找到了对象
                    result = null;
                else
                {
                    result = gph.GetValue(tmp, Name);
                    return result != default;
                }
            }
            return succ;
        }

        private string GetDebuggerDisplay()
        {
            return $"{{{ObjectId}.{Name}}}";
        }
    }

    /// <summary>
    /// 代表数组的类。
    /// </summary>
    public class ArrayGExpression : GameExpressionBase
    {
        /// <summary>
        /// 分析数组。
        /// </summary>
        /// <param name="str">'|'</param>
        /// <param name="env"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool TryParse(string str, GameExpressionCompileEnvironment env, out ArrayGExpression result)
        {
            result = new ArrayGExpression(str.Split('|').Select(c => ConstOrReference(env, c)));
            return true;
        }

        public ArrayGExpression(IEnumerable<GameExpressionBase> elementes)
        {
            _Elementes.AddRange(elementes);
        }

        private List<GameExpressionBase> _Elementes = new List<GameExpressionBase>();
        internal List<GameExpressionBase> Elementes { get => _Elementes; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool SetValue(GameExpressionRuntimeEnvironment env, object val)
        {
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool TryGetValue(GameExpressionRuntimeEnvironment env, out object result)
        {
            result = _Elementes;
            return true;
        }
    }

    /// <summary>
    /// 访问一个数组的元素。
    /// </summary>
    public class ArrayElementGExpression : GameExpressionBase
    {

        public ArrayElementGExpression(GameExpressionBase array, GameExpressionBase index)
        {
            Array = array;
            Index = index;
        }

        public GameExpressionBase Array { get; set; }
        public GameExpressionBase Index { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetIndex(GameExpressionRuntimeEnvironment env, out int index)
        {
            index = 0;
            var indexObj = Index.GetValueOrDefault(env);
            if (null == indexObj || !OwHelper.TryGetDecimal(indexObj, out var indexDec))
                return false;
            index = Convert.ToInt32(Math.Round(indexDec));
            return true;
        }

        public override bool SetValue(GameExpressionRuntimeEnvironment env, object val)
        {
            var ary = Array.GetValueOrDefault(env) as ArrayGExpression;
            if (null == ary)
                return false;
            if (!TryGetIndex(env, out var index))
                return false;
            return ary.Elementes[index].SetValue(env, val);
        }

        public override bool TryGetValue(GameExpressionRuntimeEnvironment env, out object result)
        {
            if (!Array.TryGetValue(env, out var aryObj) || !(aryObj is IList<GameExpressionBase> ary))    //若找不到数组
                goto errLable;
            if (!TryGetIndex(env, out var index))   //若找不到索引
                goto errLable;
            if (index < 0 || index >= ary.Count)    //若索引超界
                goto errLable;
            var aryEle = ary[index];
            if (null == aryEle) //若元素为空
                goto errLable;
            return aryEle.TryGetValue(env, out result);
        errLable:
            result = default;
            return false;
        }
    }

    //[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    [DebuggerDisplay("{" + nameof(Left) + "} {" + nameof(Operator) + ",nq} {" + nameof(Right) + "}")]
    public class BinaryGExpression : GameExpressionBase
    {
        static public readonly Dictionary<string, int> Operators = new Dictionary<string, int>()
        {
            { "+", 10},
            { "-",10 },
            { "*",11 },
            { "/",11 },

            { "=",2},

            { ">",8},
            { ">=",8},
            { "==",8},
            { "!=",8},
            { "<",8},
            { "<=",8},

            { "&&",6},
            { "||",5},

        };

        public BinaryGExpression(GameExpressionBase left, string @operator, GameExpressionBase right)
        {
            Left = left;
            Operator = @operator;
            Right = right;
        }

        public GameExpressionBase Left { get; set; }

        private string _Operator;
        public string Operator
        {
            get => _Operator;
            set
            {
                //TO DO
                _Operator = value;
            }
        }

        public GameExpressionBase Right { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool SetValue(GameExpressionRuntimeEnvironment env, object val)
        {
            return false;
        }

        public override bool TryGetValue(GameExpressionRuntimeEnvironment env, out object result)
        {
            bool succ = true;
            var leftObj = Left.GetValueOrDefault(env);
            var rightObj = Right.GetValueOrDefault(env);
            switch (Operator)
            {
                #region 算数运算符

                case "+":
                    if (OwHelper.TryGetDecimal(leftObj, out var left) && OwHelper.TryGetDecimal(rightObj, out var right))
                        result = left + right;
                    else
                        goto errLable;
                    break;
                case "-":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left - right;
                    else
                        goto errLable;
                    break;
                case "*":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left * right;
                    else
                        goto errLable;
                    break;
                case "/":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left / right;
                    else
                        goto errLable;
                    break;

                #endregion 算数运算符

                #region 比较运算符
                case ">":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left > right;
                    else
                        goto errLable;
                    break;
                case ">=":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left >= right;
                    else
                        goto errLable;
                    break;
                case "==":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left == right;
                    else
                        result = Equals(leftObj, rightObj);
                    break;
                case "!=":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left != right;
                    else
                        result = !Equals(leftObj, rightObj);
                    break;
                case "<":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left < right;
                    else
                        goto errLable;
                    break;
                case "<=":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left <= right;
                    else
                        goto errLable;
                    break;
                #endregion 比较运算符

                #region 逻辑运算符
                case "||":
                    {
                        if (leftObj is bool leftBool && rightObj is bool rightBool)
                            result = leftBool || rightBool;
                        else
                            goto errLable;
                    }
                    break;
                case "&&":
                    {
                        if (leftObj is bool leftBool && rightObj is bool rightBool)
                            result = leftBool && rightBool;
                        else
                            goto errLable;
                    }
                    break;

                #endregion 逻辑运算符

                #region 赋值运算符
                case "=":
                    if (!Left.SetValue(env, rightObj))
                        goto errLable;
                    result = rightObj;
                    break;
                #endregion 赋值运算符
                default:
                    goto errLable;
            }
            return succ;
        errLable:
            result = default;
            return false;
        }

        private string GetDebuggerDisplay()
        {
            return $"{{{Left} {Operator} {Right}}}";
        }
    }

    #endregion 基础类型

    /// <summary>
    /// 表示包含一个表达式序列的块，表达式中可定义变量。
    /// </summary>
    public class BlockGExpression : GameExpressionBase
    {
        public BlockGExpression()
        {

        }

        public BlockGExpression(IEnumerable<GameExpressionBase> expressions)
        {
            Expressions.AddRange(expressions);
        }

        List<GameExpressionBase> _Expressions = new List<GameExpressionBase>();

        public List<GameExpressionBase> Expressions { get => _Expressions; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool SetValue(GameExpressionRuntimeEnvironment env, object val)
        {
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool TryGetValue(GameExpressionRuntimeEnvironment env, out object result)
        {
            result = default;
            object tmp = default;
            var returnVal = Expressions.All(c => c.TryGetValue(env, out tmp));
            if (!returnVal)
                return false;
            result = tmp;
            return true;
        }
    }
}