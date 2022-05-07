﻿using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace OW.Game.Expression
{
    public class GameExpression
    {
        /// <summary>
        /// 操作符。
        /// </summary>
        private static readonly string[] op = new string[] { "gtq", "gt", "ltq", "lt", "eq", "neq" };

        /// <summary>
        /// 操作符。
        /// </summary>
        private string _Operator;

        /// <summary>
        /// 
        /// </summary>
        public string Operator => _Operator;

        /// <summary>
        /// 操作数。
        /// </summary>
        private List<object> _Operand;

        public List<object> Operand => _Operand ??= new List<object>();


        public object Compute(GameChar gameChar)
        {
            Expression<Func<GameThingBase, string, decimal>> s = (c1, c2) => gameChar.Properties.GetDecimalOrDefault(c2, default);
            var sss = s.Compile().Invoke(gameChar, "");
            object result;
            switch (_Operator)
            {
                case "gtq":
                    result = gameChar.Properties.GetDecimalOrDefault(_Operand[0] as string) >= (decimal)_Operand[1];
                    break;
                default:
                    result = null;
                    break;
            }
            return result;
        }
    }

    public class BinaryGameExpression : GameExpression
    {
        public BinaryGameExpression()
        {
        }
    }

    public class GameCharExpression : GameExpression
    {
        public static Expression<Func<SimpleDynamicPropertyBase, string, decimal>> _GetDecimalProperty = (gc, key) => gc.Properties.GetDecimalOrDefault(key, default);

        public static Func<SimpleDynamicPropertyBase, string, decimal> SS;
        static GameCharExpression()
        {
            SS = _GetDecimalProperty.Compile();
            Expression<Func<SimpleDynamicPropertyBase, string, decimal>> dsd = (gc, key) => SS(gc, key);
        }

        protected bool Invoke(GameChar gameChar)
        {
            object result = false;
            switch (Operator)
            {
                case "gtq":
                    result = gameChar.GetDecimalWithFcpOrDefault(Operand[0] as string) >= (decimal)Operand[1];
                    break;
                case "gt":
                    result = gameChar.GetDecimalWithFcpOrDefault(Operand[0] as string) > (decimal)Operand[1];
                    break;
                case "ltq":
                    result = gameChar.GetDecimalWithFcpOrDefault(Operand[0] as string) <= (decimal)Operand[1];
                    break;
                case "lt":
                    result = gameChar.GetDecimalWithFcpOrDefault(Operand[0] as string) < (decimal)Operand[1];
                    break;
                case "eq":
                    result = gameChar.GetDecimalWithFcpOrDefault(Operand[0] as string) == (decimal)Operand[1];
                    break;
                case "neq":
                    result = gameChar.GetDecimalWithFcpOrDefault(Operand[0] as string) != (decimal)Operand[1];
                    break;
                default:
                    break;
            }
            return (bool)result;
        }
    }

    /// <summary>
    /// 条件对象。
    /// </summary>
    public class GameValidation
    {
        #region 静态成员

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyValue">Item1是算子（注意要去掉前缀如:rq），Item2是参数字符串。</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParse((string, object) keyValue, out GameValidation result)
        {
            result = new GameValidation();
            if (!Operators.Contains(keyValue.Item1))
                return false;
            result.Operator = keyValue.Item1;
            var str = keyValue.Item2 as string;
            if (string.IsNullOrWhiteSpace(str))
                return false;
            var ary = str.Split(OwHelper.SemicolonArrayWithCN);
            if (ary.Length < 2 || ary.Length > 4)   //若参数过多或过少
                return false;
            if (!GameReference.TryParse(ary.AsSpan(0, ary.Length - 1), out var gr))
                return false;
            if (!OwConvert.TryToDecimal(ary[^1], out var val))
                return false;
            result.Value = val;
            result.GameReference = gr;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="prefix"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static void Fill(IReadOnlyDictionary<string, object> dic, [AllowNull] string prefix, ICollection<GameValidation> collection)
        {
            var coll = dic.GetValuesWithoutPrefix(prefix);
            foreach (var item in coll)
            {
                var tps = from tmp in item
                          join key in Operators
                          on tmp.Item1 equals key
                          let str = tmp.Item2 as string
                          where !string.IsNullOrWhiteSpace(str)
                          select (tmp.Item1, tmp.Item2);
                foreach (var tp in tps) //获取条件对象
                {
                    if (TryParse(tp, out var result))
                        collection.Add(result);
                }
            }
        }

        /// <summary>
        /// 可识别的运算符数组。
        /// </summary>
        public static readonly string[] Operators = new string[] { "gtq", "gt", "eq", "neq", "ltq", "lt" };

        #endregion 静态成员

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameValidation()
        {
            //ptid tid pn val
        }

        #region 属性

        /// <summary>
        /// 左算子。
        /// </summary>
        public GameReference GameReference { get; set; }

        /// <summary>
        /// 设置或获取比较运算符。
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// 设置或获取要比较的值。
        /// </summary>
        public decimal Value { get; set; }

        #endregion 属性

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public bool IsValid(GameChar gameChar)
        {
            if (!OwConvert.TryToDecimal(GameReference.GetValue(gameChar), out var val))
                val = default;
            bool result;
            switch (Operator)
            {
                case "gtq": //大于或等于
                    result = val >= Value;
                    break;
                case "gt":  //大于
                    result = val > Value;
                    break;
                case "eq":  //等于
                    result = val == Value;
                    break;
                case "neq": //不等于
                    result = val != Value;
                    break;
                case "ltq": //小于或等于
                    result = val <= Value;
                    break;
                case "lt":  //小于
                    result = val < Value;
                    break;
                default:
                    result = false;
                    break;
                    //throw new ArgumentException($"不认识的比较运算符,{keyValue}", nameof(prefix));
            }
            return result;
        }
    }

    public class GameReference
    {
        #region 静态成员

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str">分号分割的对象属性引用字符串,结构可能是：容器模板id;模板id;属性名。</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParse(string str, out GameReference result)
        {
            var ary = str.Split(OwHelper.SemicolonArrayWithCN);
            return TryParse(ary.AsSpan(), out result);
        }

        public static bool TryParse(ReadOnlySpan<string> strs, out GameReference result)
        {
            result = new GameReference();
            switch (strs.Length)
            {
                case 3:
                    if (!Guid.TryParse(strs[0], out var ptid))
                        return false;
                    if (!Guid.TryParse(strs[1], out var tid))
                        return false;
                    if (string.IsNullOrWhiteSpace(strs[2]))
                        return false;
                    result.ParentTemplateId = ptid;
                    result.TemplateId = tid;
                    result.PropertyName = strs[2];
                    break;
                case 2:
                    if (!Guid.TryParse(strs[0], out tid))
                        return false;
                    if (string.IsNullOrWhiteSpace(strs[1]))
                        return false;
                    result.TemplateId = tid;
                    result.PropertyName = strs[1];
                    break;
                case 1:
                    if (string.IsNullOrWhiteSpace(strs[0]))
                        return false;
                    result.TemplateId = ProjectConstant.CharTemplateId;
                    result.PropertyName = strs[0];
                    break;
                default:
                    return false;
            }
            return true;
        }

        #endregion 静态成员

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameReference()
        {
        }

        #region 属性

        /// <summary>
        /// 设置或获取父容器模板id。如果为空则不限定父容器模板id。
        /// </summary>
        public Guid? ParentTemplateId { get; set; }

        /// <summary>
        /// 设置或获取对象的模板id。
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 设置或获取限定的属性名。
        /// </summary>
        public string PropertyName { get; set; }

        #endregion 属性

        /// <summary>
        /// 获取值。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public object GetValue(GameChar gameChar)
        {
            GameThingBase gt;
            if (ParentTemplateId is null)   //若不限定容器
            {
                gt = TemplateId == ProjectConstant.CharTemplateId ? gameChar as GameThingBase : gameChar.AllChildren.FirstOrDefault(c => c.TemplateId == TemplateId);
            }
            else //若限定容器
            {
                gt = ParentTemplateId == ProjectConstant.CharTemplateId ? gameChar.GameItems.FirstOrDefault(c => c.TemplateId == TemplateId) :
                    gameChar.AllChildren.FirstOrDefault(c => c.TemplateId == ParentTemplateId.Value)?.Children.FirstOrDefault(c => c.TemplateId == TemplateId);
            }
            return gt?.GetDecimalWithFcpOrDefault(PropertyName);
        }

        /// <summary>
        /// 设置引用对象引用属性的值。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="value"></param>
        public void SetValue(GameChar gameChar, object value)
        {
            GameThingBase gt;
            if (ParentTemplateId is null)   //若不限定容器
            {
                gt = TemplateId == ProjectConstant.CharTemplateId ? gameChar as GameThingBase : gameChar.AllChildren.FirstOrDefault(c => c.TemplateId == TemplateId);
            }
            else //若限定容器
            {
                gt = ParentTemplateId == ProjectConstant.CharTemplateId ? gameChar.GameItems.FirstOrDefault(c => c.TemplateId == TemplateId) :
                    gameChar.AllChildren.FirstOrDefault(c => c.TemplateId == ParentTemplateId.Value)?.Children.FirstOrDefault(c => c.TemplateId == TemplateId);
            }
            if (null != gt)
                gt.Properties[PropertyName] = value;
        }
    }
}