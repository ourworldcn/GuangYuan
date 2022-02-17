using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OW.Extensions.Game.Store
{
    public static class GameThingBaseExtensions
    {
        /// <summary>
        /// 客户端要记录的一些属性，这个属性客户端可以随意更改，服务器不使用。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetClientString(this GameThingBase thing)
        {
            var tmp = thing.Properties.GetStringOrDefault("ClientString");
            return tmp is null ? null : Uri.UnescapeDataString(tmp);
        }

        /// <summary>
        /// 客户端要记录的一些属性，这个属性客户端可以随意更改，服务器不使用。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetClientString(this GameThingBase thing, string value)
        {
            if (value is null)
                thing.Properties.Remove("ClientString");
            else
                thing.Properties["ClientString"] = Uri.EscapeDataString(value);
        }

    }

    /// <summary>
    /// <see cref="GameItem"/>类的扩展方法封装类。
    /// </summary>
    public static class GameItemExtensions
    {
        /// <summary>
        /// 获取创建时间。
        /// </summary>
        /// <param name="thing"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime? GetCreateUtc(this GameItem thing) =>
            thing.Properties.TryGetDateTime("CreateUtc", out var result) ? new DateTime?(result) : null;

        /// <summary>
        /// 设置创建时间。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="value">空值表示删除属性。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCreateUtc(this GameItem thing, DateTime? value)
        {
            if (value.HasValue) //若有指定值
            {
                thing.Properties["CreateUtc"] = value.Value.ToString();
            }
            else
            {
                thing.Properties.Remove("CreateUtc");
            }
        }

        /// <summary>
        /// 如果物品处于某个容器中，则这个成员指示其所处位置号，从0开始，但未必连续,序号相同则顺序随机。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetOrderNumber(this GameItem gameItem)
        {
            return (int)gameItem.Properties.GetDecimalOrDefault("OrderNumber");
        }

        /// <summary>
        /// 如果物品处于某个容器中，则这个成员指示其所处位置号，从0开始，但未必连续,序号相同则顺序随机。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetOrderNumber(this GameItem gameItem, int value)
        {
            gameItem.Properties["OrderNumber"] = (decimal)value;
        }

        #region 待重构代码
        /// <summary>
        /// 换新模板。
        /// </summary>
        /// <param name="this"></param>
        /// <param name="template"></param>
        public static void ChangeTemplate(this GameItem @this, GameItemTemplate template)
        {
            var keysBoth = @this.Properties.Keys.Intersect(template.Properties.Keys).ToArray();
            var keysNew = template.Properties.Keys.Except(keysBoth).ToArray();
            foreach (var key in keysNew)    //新属性
            {
                var newValue = template.GetPropertyValue(key);
                if (newValue is decimal[] ary)   //若是一个序列属性
                {
                    var indexName = template.GetIndexPropertyName(key); //索引属性名
                    if ((@this.TryGetPropertyValue(indexName, out var indexObj) || template.TryGetPropertyValue(indexName, out indexObj)) &&
                        OwConvert.TryToDecimal(indexObj, out var index))
                    {
                        index = Math.Round(index, MidpointRounding.AwayFromZero);
                        @this.SetPropertyValue(key, ary[(int)index]);
                    }
                    else
                        @this.SetPropertyValue(key, ary[0]);
                }
                else
                    @this.SetPropertyValue(key, newValue);
            }
            foreach (var key in keysBoth)   //遍历两者皆有的属性
            {
                var currentVal = @this.GetPropertyValueOrDefault(key);
                var oldVal = @this.GetTemplate().GetPropertyValue(key);    //模板值
                if (oldVal is decimal[] ary && OwConvert.TryToDecimal(currentVal, out var currentDec))   //若是一个序列属性
                {
                    var lv = @this.GetIndexPropertyValue(key);    //当前等级
                    var nVal = currentDec - ary[lv] + template.GetSequencePropertyValueOrDefault<decimal>(key, lv); //求新值
                    @this.SetPropertyValue(key, nVal);
                }
                else if (OwConvert.TryToDecimal(currentVal, out var dec)) //若是一个数值属性
                {
                    OwConvert.TryToDecimal(@this.GetTemplate().GetPropertyValue(key, 0), out var nDec);    //当前模板中该属性
                    OwConvert.TryToDecimal(template.GetPropertyValue(key), out var tDec);
                    var nVal = dec - nDec + tDec;
                    @this.SetPropertyValue(key, nVal);
                }
                else //其他类型属性
                {
                    @this.SetPropertyValue(key, template.GetPropertyValue(key));
                }
            }
            @this.TemplateId = template.Id;
            @this.SetTemplate((GameThingTemplateBase)template);
        }

        /// <summary>
        /// 获取指定属性的当前级别索引值。
        /// </summary>
        /// <param name="this"></param>
        /// <param name="name"></param>
        /// <returns>如果不是序列属性或索引属性值不是数值类型则返回-1。如果没有找到索引属性返回0。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexPropertyValue(this GameItem @this, string name)
        {
            if (!@this.GetTemplate().TryGetPropertyValue(name, out var tVal) || !(tVal is decimal[]))  //若不是序列属性
                return -1;
            var indexPName = @this.GetTemplate().GetIndexPropertyName(name);   //其索引属性名
            int result;
            if (!@this.TryGetPropertyValue(indexPName, out var resultObj))    //若没有找到索引属性的值
                result = 0;
            else //若找到索引属性
                result = OwConvert.TryToDecimal(resultObj, out var resultDec) ? OwHelper.RoundWithAwayFromZero(resultDec) : -1;
            return result;
        }

        #endregion 待重构代码
    }

    public static class GameCharBaseExtensions
    {

    }

    public static class GameUserBaseExtensions
    {

    }
}