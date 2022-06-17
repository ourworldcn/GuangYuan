using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using OW.Game;
using OW.Game.Mission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OW.Extensions.Game.Store
{
    /// <summary>
    /// GameChar对象 BinaryArray 属性包含对象的视图类。
    /// </summary>
    public class CharBinaryExProperties
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CharBinaryExProperties()
        {

        }

        /// <summary>
        /// 获取或设置客户端使用的字符串字典。
        /// </summary>
        public Dictionary<string, string> ClientProperties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 持久的变化数据。
        /// </summary>
        public List<ChangeData> ChangeDatas { get; set; } = new List<ChangeData>();

        /// <summary>
        /// 任务状态字典。
        /// </summary>
        public Dictionary<string, MissionState> MissionStates { get; set; } = new Dictionary<string, MissionState>();
    }

    public static class GameThingBaseExtensions
    {
        /// <summary>
        /// 获取创建时间。
        /// </summary>
        /// <param name="thing"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime? GetCreateUtc(this GameThingBase thing) =>
            thing.Properties.TryGetDateTime("CreateUtc", out var result) ? new DateTime?(result) : null;

        /// <summary>
        /// 设置创建时间。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="value">空值表示删除属性。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCreateUtc(this GameThingBase thing, DateTime? value)
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
        /// 如果物品处于某个容器中，则这个成员指示其所处位置号，从0开始，但未必连续,序号相同则顺序随机。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns>没有则返回0.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetOrderNumber(this GameItem gameItem)
        {
            return (int)gameItem.Properties.GetDecimalOrDefault("OrderNumber");
        }

        /// <summary>
        /// 如果物品处于某个容器中，则这个成员指示其所处位置号，从0开始，但未必连续,序号相同则顺序随机。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="value">null表示删除该属性。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetOrderNumber(this GameItem gameItem, int? value)
        {
            if (value.HasValue)
                gameItem.Properties["OrderNumber"] = (decimal)value.Value;
            else
                gameItem.Properties.Remove("OrderNumber");
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
                    if (@this.TryGetPropertyWithFcp(indexName, out var index) || template.Properties.TryGetDecimal(indexName, out index))
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
                var currentVal = @this.GetPropertyOrDefault(key);
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
            if (!@this.TryGetProperty(indexPName, out var resultObj))    //若没有找到索引属性的值
                result = 0;
            else //若找到索引属性
                result = OwConvert.TryToDecimal(resultObj, out var resultDec) ? OwHelper.RoundWithAwayFromZero(resultDec) : -1;
            return result;
        }

        /// <summary>
        /// 获取属性，且考虑是否刷新并写入快速变化属性。
        /// </summary>
        /// <param name="name">要获取值的属性名。</param>
        /// <param name="refreshDate">当有快速变化属性时，刷新时间，如果为null则不刷新。</param>
        /// <param name="writeDictionary">当有快速变化属性时，是否写入<see cref="Properties"/>属性。</param>
        /// <param name="result">属性的当前返回值。对快速变化属性是其<see cref="FastChangingProperty.LastValue"/>,是否在之前刷新取决于<paramref name="refresh"/>参数。</param>
        /// <param name="refreshDatetime">如果是快速变化属性且需要刷新，则此处返回实际的计算时间。
        /// 如果找到的不是快速渐变属性返回<see cref="DateTime.MinValue"/></param>
        /// <returns>true成功找到属性。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetPropertyValueWithFcp(this GameItem gameItem, string name, DateTime? refreshDate, bool writeDictionary, out object result, out DateTime refreshDatetime)
        {
            bool succ;
            if (gameItem.Name2FastChangingProperty.TryGetValue(name, out var fcp)) //若找到快速变化属性
            {
                if (refreshDate.HasValue) //若需要刷新
                {
                    refreshDatetime = refreshDate.Value;
                    result = fcp.GetCurrentValue(ref refreshDatetime);
                }
                else
                {
                    refreshDatetime = DateTime.MinValue;
                    result = fcp.LastValue;
                }
                if (writeDictionary)
                    fcp.ToGameThing(gameItem);
                succ = true;
            }
            else //若是其他属性
            {
                refreshDatetime = DateTime.MinValue;
                succ = gameItem.Properties.TryGetValue(name, out result);
            }
            return succ;
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