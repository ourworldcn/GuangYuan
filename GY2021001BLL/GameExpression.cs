
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace OwGame
{

    public class GamePropertyHelper
    {
        public object GetValue(object obj, string propertyName, object defaultValue = null)
        {
            return null;
        }

        public bool SetValue(object obj, string propertyName, object val)
        {
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

        /// <summary>
        /// 参数。
        /// </summary>
        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        /// <summary>
        /// 服务。
        /// </summary>
        public IServiceProvider Services { get; set; }
    }

    public class GameExpressionCompileEnvironment
    {
        public string CurrentObjectId { get; set; }

        public IServiceProvider Services { get; set; }

        public string DictionaryPropertyName { get; set; }
    }

    public abstract class GameExpressionBase
    {

        public GameExpressionBase()
        {

        }

        public abstract object GetValueOrDefault(GameExpressionRuntimeEnvironment env, object defaultValue = null);

        static private readonly Random Random = new Random();
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

        static public IDictionary<string, GameExpressionBase> CompileVariableDeclare(GameExpressionCompileEnvironment env, string inputs)
        {
            Dictionary<string, GameExpressionBase> result = new Dictionary<string, GameExpressionBase>();
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
            return result;
        }

        /// <summary>
        /// 
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
            var lastChar = inputs[inputs.Length - 1];
            if (inputs.Contains('|'))    //若可能是数组
            {
                result = MakeArray(env, inputs);
            }
            else if (inputs.Contains('(') && inputs.EndsWith(')'))    //若可能是函数
            {
                var items = inputs.Split('(');
                if (items.Length == 2) //若是函数调用
                {
                    var paramAry = new GameExpressionBase[] { ConstOrReference(env, items[1].TrimEnd(')')), };
                    result = new FunctionCallGExpression(items[0], paramAry);
                }
                else
                    result = null;
            }
            else if (inputs.Contains('[') && inputs.EndsWith(']'))    //若可能是数组元素访问
            {
                var items = inputs.Split('[');
                if (items.Length == 2) //若是函数调用
                {
                    result = new ArrayElementGExpression(ConstOrReference(env, items[0]), ConstOrReference(env, items[1].TrimEnd(']')));
                }
                else
                    result = null;
            }
            else
                result = ConstOrReference(env, inputs);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public ConstGExpression ConstOrDefault(string str, ConstGExpression defaultValue = null)
        {
            return ConstGExpression.TryParse(str, out var result) ? result : defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public GameExpressionBase ConstOrReference(GameExpressionCompileEnvironment env, string str)
        {
            str = str.Trim();
            return ConstGExpression.TryParse(str, out var result) ? (GameExpressionBase)result : new ReferenceGExpression(str, env.CurrentObjectId);
        }


    }

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

        public override object GetValueOrDefault(GameExpressionRuntimeEnvironment env, object defaultValue = null)
        {
            object result;
            switch (Name)
            {
                case "rnd":
                    result = NextDouble();
                    break;
                case "rndi":
                    var maxObj = _Parameters.SingleOrDefault()?.GetValueOrDefault(env);
                    if (maxObj == null || !OwHelper.TryGetDecimal(maxObj, out var dec))
                        result = defaultValue;
                    else
                        result = Next(Convert.ToInt32(Math.Round(dec)));
                    break;
                default:
                    result = defaultValue;
                    break;
            }
            return result;
        }
    }

    /// <summary>
    /// 常量。
    /// </summary>
    public class ConstGExpression : GameExpressionBase
    {
        static public ConstGExpression Null = new ConstGExpression(null);

        static public bool TryParse(string str, out ConstGExpression result)
        {
            str = str.Trim();
            if (string.IsNullOrEmpty(str))
            {
                result = Null;
                return true;
            }

            if (str.StartsWith('\"') && str.EndsWith('\"'))  //若明确是字符串
            {
                result = new ConstGExpression(str.Trim('\"'));
            }
            else if (decimal.TryParse(str, out var dec))    //若是一个数字
                result = new ConstGExpression(dec);
            else if (Guid.TryParse(str, out var guid))  //若是Guid
                result = new ConstGExpression(guid);
            else
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

        public ConstGExpression(string str)
        {
            if (str.StartsWith('\"') && str.EndsWith('\"'))  //若明确是字符串
            {
                _Value = str.Trim('\"');
            }
            else if (decimal.TryParse(str, out var dec))
                _Value = dec;
            else if (Guid.TryParse(str, out var guid))
                _Value = guid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        /// <param name="defaultValue"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object GetValueOrDefault(GameExpressionRuntimeEnvironment env, object defaultValue = null)
        {
            return _Value;
        }
    }

    public class ReferenceGExpression : GameExpressionBase
    {
        public ReferenceGExpression()
        {

        }

        public string Name { get; set; }
        public string ObjectId { get; }

        public ReferenceGExpression(string name, string objectId)
        {
            Name = name;
            ObjectId = objectId;
        }

        public override object GetValueOrDefault(GameExpressionRuntimeEnvironment env, object defaultValue = null)
        {
            if (!env.Variables.TryGetValue(Name, out var expr)) //若不是变量参数
            {
                if (!env.Variables.TryGetValue(ObjectId, out var obj) || obj is null)  //若找不到对象
                    return defaultValue;
                var gph = env.Services.GetService(typeof(GamePropertyHelper)) as GamePropertyHelper;
                var tmp = obj.GetValueOrDefault(env);
                return null == tmp ? defaultValue : gph.GetValue(tmp, Name, defaultValue);
            }
            return expr.GetValueOrDefault(env, defaultValue);
        }
    }

    /// <summary>
    /// 代表数组的类。
    /// </summary>
    public class ArrayGExpression : GameExpressionBase
    {

        public ArrayGExpression(IEnumerable<GameExpressionBase> elementes)
        {
            _Elementes.AddRange(elementes);
        }

        private List<GameExpressionBase> _Elementes = new List<GameExpressionBase>();
        internal List<GameExpressionBase> Elementes { get => _Elementes; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object GetValueOrDefault(GameExpressionRuntimeEnvironment env, object defaultValue = null)
        {
            return _Elementes ?? defaultValue;
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

        public override object GetValueOrDefault(GameExpressionRuntimeEnvironment env, object defaultValue = null)
        {
            var ary = Array.GetValueOrDefault(env, null) as IList<GameExpressionBase>;
            if (null == ary)
                return defaultValue;
            //获取索引
            var indexObj = Index.GetValueOrDefault(env, -1);
            bool b = OwHelper.TryGetDecimal(indexObj, out decimal dec);
            if (!b)
                return defaultValue;
            var index = (int)Math.Round(dec);
            if (index < 0 || index >= ary.Count)
                return defaultValue;
            return ary[index]?.GetValueOrDefault(env, defaultValue);
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

        public override object GetValueOrDefault(GameExpressionRuntimeEnvironment env, object defaultValue = null)
        {
            object result;
            var leftObj = Left.GetValueOrDefault(env);
            var rightObj = Right.GetValueOrDefault(env);
            switch (Operator)
            {
                #region 算数运算符

                case "+":
                    if (OwHelper.TryGetDecimal(leftObj, out var left) && OwHelper.TryGetDecimal(rightObj, out var right))
                        result = left + right;
                    else
                        result = defaultValue;
                    break;
                case "-":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left - right;
                    else
                        result = defaultValue;
                    break;
                case "*":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left * right;
                    else
                        result = defaultValue;
                    break;
                case "/":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left / right;
                    else
                        result = defaultValue;
                    break;

                #endregion 算数运算符

                #region 比较运算符
                case ">":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left > right;
                    else
                        result = defaultValue;
                    break;
                case ">=":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left >= right;
                    else
                        result = defaultValue;
                    break;
                case "==":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left == right;
                    else
                        result = defaultValue;
                    break;
                case "!=":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left != right;
                    else
                        result = defaultValue;
                    break;
                case "<":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left < right;
                    else
                        result = defaultValue;
                    break;
                case "<=":
                    if (OwHelper.TryGetDecimal(leftObj, out left) && OwHelper.TryGetDecimal(rightObj, out right))
                        result = left <= right;
                    else
                        result = defaultValue;
                    break;
                #endregion 比较运算符

                #region 逻辑运算符
                case "||":
                    {
                        if (leftObj is bool leftBool && rightObj is bool rightBool)
                            result = leftBool || rightBool;
                        else
                            result = defaultValue;
                    }
                    break;
                case "&&":
                    {
                        if (leftObj is bool leftBool && rightObj is bool rightBool)
                            result = leftBool && rightBool;
                        else
                            result = defaultValue;
                    }
                    break;

                #endregion 逻辑运算符
                default:
                    result = defaultValue;
                    break;
            }
            return result;
        }
    }
}
