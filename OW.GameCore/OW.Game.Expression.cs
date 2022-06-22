using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.ObjectPool;
using OW.Game.PropertyChange;
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

    /// <summary>
    /// 
    /// </summary>
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

}
