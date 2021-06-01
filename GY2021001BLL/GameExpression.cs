
using GY2021001DAL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace OwGame
{
    /// <summary>
    /// 给对象设置属性的帮助器。
    /// 因该作为全局的服务之一。
    /// </summary>
    public class GamePropertyHelper
    {
        /// <summary>
        /// 获取对象的属性、
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetValue(object obj, string propertyName, object defaultValue = default)
        {
            var _ = obj as GameThingBase;
            var dic = _?.Properties;
            return dic == null ? defaultValue : dic.GetValueOrDefault(propertyName, defaultValue);
        }

        /// <summary>
        /// 设置对象的属性。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetValue(object obj, string propertyName, object val)
        {
            var _ = obj as GameThingBase;
            var dic = _?.Properties;
            dic[propertyName] = val;
            return true;
        }
    }

    public class GameExpressionRuntimeEnvironment
    {
        public GameExpressionRuntimeEnvironment()
        {
        }

        /// <summary>
        /// 变量。
        /// </summary>
        public Dictionary<string, GameExpressionBase> Variables { get; } = new Dictionary<string, GameExpressionBase>();

        ///// <summary>
        ///// 参数。
        ///// </summary>
        //public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        /// <summary>
        /// 服务。
        /// </summary>
        public IServiceProvider Services { get; set; }
    }

    public class GameExpressionCompileEnvironment
    {
        public string CurrentObjectId { get; set; }

        public IServiceProvider Services { get; set; }

        /// <summary>
        /// 变量声明。
        /// </summary>
        public Dictionary<string, GameExpressionBase> Variables { get; } = new Dictionary<string, GameExpressionBase>();
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
        static private SpinLock sl = new SpinLock();
        static public double NextDouble()
        {
            bool gotLock = false;
            sl.Enter(ref gotLock);
            try
            {
                return Random.NextDouble();

            }
            finally
            {
                if (gotLock) sl.Exit();
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
            sl.Enter(ref gotLock);
            try
            {
                return Random.Next(max);

            }
            finally
            {
                if (gotLock) sl.Exit();
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
            Dictionary<string, GameExpressionBase> result = env.Variables;
            var alls = inputs.Split(OwHelper.CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Split('=', StringSplitOptions.None));
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
        /// 编译一组赋值语句。
        /// </summary>
        /// <param name="env"></param>
        /// <param name="inputs"></param>
        static public void CompileBody(GameExpressionCompileEnvironment env, string inputs)
        {

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
                result = CompileArray(env, inputs);
            else //若是其他简单操作数
                result = CompileOperand(env, inputs);
            return result;
        }

        /// <summary>
        /// 获取一个表示数组的表达式。
        /// </summary>
        /// <param name="env"></param>
        /// <param name="inputs"></param>
        /// <returns></returns>
        static public ArrayGExpression CompileArray(GameExpressionCompileEnvironment env, string inputs)
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

        const string comparePattern = @"(?<or>[^{}]+)\s*(?<op>[{}]{0,2})";

        private static string _Pattern;

        static protected string PatternString
        {
            get
            {
                if (null == _Pattern)
                {
                    var tmp = string.Concat(BinaryGExpression.Operators.Keys.SelectMany(c => c).Distinct().Select(c => @"\" + char.ToString(c)));
                    _Pattern = comparePattern.Replace(@"{}", tmp);
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
        static public GameExpressionBase CompileOperand(GameExpressionCompileEnvironment env, string inputs)
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
                    var _ = items[1].TrimEnd(')').Split(OwHelper.CommaArrayWithCN).Select(c => ConstOrReference(env, c));
                    result = new FunctionCallGExpression(items[0], _);
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
        /// 编译表达式。
        /// </summary>
        /// <param name="env"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        static public GameExpressionBase CompileExpression(GameExpressionCompileEnvironment env, string str)
        {
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
            var left = CompileOperand(env, operands.Pop());
            if (operators.Count <= 0)   //若没有操作符
                return left;
            var opt = operators.Pop();
            while (true)
            {
                if (!operators.TryPeek(out var optNext))    //若已经没有操作符
                    return new BinaryGExpression(left, opt, CompileOperand(env, operands.Pop()));
                else //若还有操作符
                {
                    if (OperatorCompareTo(opt, optNext) >= 0)   //若当前操作符优先级大于或等于下一个操作符
                        left = new BinaryGExpression(left, opt, CompileOperand(env, operands.Pop()));
                    else
                        left = new BinaryGExpression(left, opt, CompileExpression(env, operands, operators));
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
    /// 函数调用的
    /// </summary>
    public class FunctionCallGExpression : GameExpressionBase
    {
        private string name;

        public FunctionCallGExpression()
        {

        }

        public FunctionCallGExpression(string name, IEnumerable<GameExpressionBase> parameters)
        {
            Name = name;
            _Parameters.AddRange(parameters);
        }


        /// <summary>
        /// 函数名，不区分大小写，一律视同全小写。设置的值中有大写字母，也会自动转换为小写。且忽略空白。
        /// </summary>
        public string Name { get => name; set => name = value.Trim().ToLower(); }

        public List<GameExpressionBase> _Parameters { get; } = new List<GameExpressionBase>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool SetValue(GameExpressionRuntimeEnvironment env, object val)
        {
            return false;
        }

        public override bool TryGetValue(GameExpressionRuntimeEnvironment env, out object result)
        {
            bool succ = false;
            switch (Name)
            {
                case "rnd":
                    result = NextDouble();
                    succ = true;
                    break;
                case "rndi":
                    var maxObj = _Parameters.SingleOrDefault()?.GetValueOrDefault(env);
                    if (maxObj == null || !OwHelper.TryGetDecimal(maxObj, out var dec))
                        result = default;
                    else
                    {
                        result = Next(Convert.ToInt32(Math.Round(dec)));
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
    public class ConstGExpression : GameExpressionBase
    {
        /// <summary>
        /// 空引用。
        /// </summary>
        static public ConstGExpression Null = new ConstGExpression(null);

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

        private readonly object _Value;

        public ConstGExpression(object value)
        {
            _Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool SetValue(GameExpressionRuntimeEnvironment env, object val)
        {
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool TryGetValue(GameExpressionRuntimeEnvironment env, out object result)
        {
            result = _Value;
            return true;
        }
    }

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
            if (env.Variables.TryGetValue(Name, out var exp))
                return exp.SetValue(env, val);
            var srv = env.Services.GetService(typeof(GamePropertyHelper)) as GamePropertyHelper;
            if (!env.Variables.TryGetValue(Name, out var objExp))
                return false;
            var obj = objExp.GetValueOrDefault(env);
            return null == obj ? false : srv.SetValue(obj, Name, val);
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
            if (!env.Variables.TryGetValue(Name, out var expr))
            {
                result = null;
                return false;
            }
            return expr.TryGetValue(env, out result);
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
                    result = Equals(leftObj, rightObj);
                    break;
                case "!=":
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
                default:
                    goto errLable;
            }
            return succ;
        errLable:
            result = default;
            return false;
        }
    }

    /// <summary>
    /// 语句的基类。
    /// </summary>
    public abstract class StatementGExpression : GameExpressionBase
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool SetValue(GameExpressionRuntimeEnvironment env, object val)
        {
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool TryGetValue(GameExpressionRuntimeEnvironment env, out object result)
        {
            result = default;
            return false;
        }

        public abstract bool Run(GameExpressionRuntimeEnvironment env);

    }

    /// <summary>
    /// 赋值语句。
    /// </summary>
    public class AssignGExpression : StatementGExpression
    {
        public AssignGExpression()
        {

        }

        public AssignGExpression(GameExpressionBase left, GameExpressionBase right)
        {
            Left = left;
            Right = right;
        }

        public GameExpressionBase Left { get; set; }

        public GameExpressionBase Right { get; set; }

        public override bool Run(GameExpressionRuntimeEnvironment env)
        {
            return true;
        }
    }

    public class BodyGExpression : StatementGExpression
    {
        public override bool Run(GameExpressionRuntimeEnvironment env)
        {
            return true;
        }
    }
    #endregion 基础类型

}
