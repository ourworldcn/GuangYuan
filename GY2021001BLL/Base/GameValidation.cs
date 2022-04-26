using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace OW.Game
{
    public class GameValidation
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyValue">Item1是算子（注意要去掉前缀如:rq），Item2是参数字符串。</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParse((string, object) keyValue, out GameValidation result)
        {
            result = new GameValidation();
            switch (keyValue.Item1)
            {
                case "gtq": //大于或等于
                case "gt":  //大于
                case "eq":  //等于
                case "neq": //不等于
                case "ltq": //小于或等于
                case "lt":  //小于
                    result.Operator = keyValue.Item1;
                    break;
                default:
                    return false;
                    //throw new ArgumentException($"不认识的比较运算符,{keyValue}", nameof(prefix));
            }
            var str = keyValue.Item2 as string;
            if (string.IsNullOrWhiteSpace(str))
                return false;
            var ary = str.Split(OwHelper.SemicolonArrayWithCN);
            switch (ary.Length)
            {
                case 4:
                    if (!Guid.TryParse(ary[0], out var ptid))
                        return false;
                    if (!Guid.TryParse(ary[1], out var tid))
                        return false;
                    if (string.IsNullOrWhiteSpace(ary[2]))
                        return false;
                    if (!OwConvert.TryToDecimal(ary[3], out var val))
                        return false;
                    result.ParentTemplateId = ptid;
                    result.TemplateId = tid;
                    result.PropertyName = ary[2];
                    result.Value = val;
                    break;
                case 3:
                    if (!Guid.TryParse(ary[0], out tid))
                        return false;
                    if (string.IsNullOrWhiteSpace(ary[1]))
                        return false;
                    if (!OwConvert.TryToDecimal(ary[2], out val))
                        return false;
                    result.TemplateId = tid;
                    result.PropertyName = ary[1];
                    result.Value = val;
                    break;
                case 2: //默认为角色的对象
                    if (string.IsNullOrWhiteSpace(ary[0]))
                        return false;
                    if (!OwConvert.TryToDecimal(ary[1], out val))
                        return false;
                    result.TemplateId = ProjectConstant.CharTemplateId;
                    result.PropertyName = ary[0];
                    result.Value = val;
                    break;
                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public bool IsValid(GameChar gameChar)
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
            if (gt is null)
                return false;
            var val = gt.GetDecimalWithFcpOrDefault(PropertyName);
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

        /// <summary>
        /// 设置或获取比较运算符。
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// 设置或获取要比较的值。
        /// </summary>
        public decimal Value { get; set; }
    }
}
