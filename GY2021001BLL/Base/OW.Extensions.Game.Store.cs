using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using OW.Game;
using OW.Game.Mission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace OW.Extensions.Game.Store
{
    /// <summary>
    /// 角色信息摘要类。
    /// </summary>
    public class CharSummary
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CharSummary()
        {

        }

        /// <summary>
        /// 用指定角色填充角色摘要信息。
        /// </summary>
        /// <param name="gameChar">角色对象。</param>
        /// <param name="charSummary">角色摘要对象。</param>
        /// <param name="records">查询操作记录的接口。</param>
        //public static void Fill(GameChar gameChar, CharSummary charSummary, IQueryable<GameActionRecord> records)
        //{
        //    charSummary.Id = gameChar.Id;
        //    charSummary.DisplayName = gameChar.DisplayName;
        //    charSummary.Level = (int)gameChar.Properties.GetDecimalOrDefault("lv", decimal.Zero);
        //    charSummary.CombatCap = 4000;
        //    var ary = records.Where(c => c.ParentId == gameChar.Id && (c.ActionId == "Logout" || c.ActionId == "Login")).
        //         OrderByDescending(c => c.DateTimeUtc).Take(1).ToArray();
        //    charSummary.LastLogoutDatetime = ary.Length == 0 || ary[0].ActionId == "Logout" ? new DateTime?() : ary[0].DateTimeUtc;
        //}

        /// <summary>
        /// 角色的Id。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 角色的昵称。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 角色等级。
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 角色战力。目前就是推关战力。
        /// </summary>
        public decimal CombatCap { get; set; }

        /// <summary>
        /// 最后一次下线时间。空表示当前在线。
        /// </summary>
        public DateTime? LastLogoutDatetime { get; set; }

        /// <summary>
        /// 家园内展示动物的集合。
        /// </summary>
        public List<GameItem> HomelandShows { get; } = new List<GameItem>();

        /// <summary>
        /// 头像索引号。默认为0。
        /// </summary>
        public int IconIndex { get; set; }

        /// <summary>
        /// 玉米田中的金币数量
        /// </summary>
        public decimal GoldOfStore { get; set; }

        /// <summary>
        /// 金币数量
        /// </summary>
        public decimal Gold { get; set; }

        /// <summary>
        /// 木材数量。
        /// </summary>
        public decimal Wood { get; set; }

        /// <summary>
        /// 树林中的木材数量。
        /// </summary>
        public decimal WoodOfStore { get; set; }

        /// <summary>
        /// pvp积分。
        /// </summary>
        public decimal PvpScores { get; set; }

        /// <summary>
        /// 主基地等级。
        /// </summary>
        public int MainBaseLevel { get; set; }
    }

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

        /// <summary>
        /// 老版本持久化的变化数据。
        /// </summary>
        public List<ChangesItemSummary> ChangeItems { get; set; } = new List<ChangesItemSummary>();
    }

    /// <summary>
    /// 可以持久序列化的变化数据。
    /// </summary>
    [DataContract]
    public class ChangesItemSummary
    {
        /// <summary>
        /// 转换为摘要类。
        /// </summary>
        /// <param name="obj"></param>
        public static explicit operator ChangesItemSummary(ChangeItem obj)
        {
            var result = new ChangesItemSummary()
            {
                ContainerId = obj.ContainerId,
                DateTimeUtc = obj.DateTimeUtc,
            };
            result.AddIds.AddRange(obj.Adds.Select(c => c.Id));
            result.RemoveIds.AddRange(obj.Removes);
            result.ChangeIds.AddRange(obj.Changes.Select(c => c.Id));
            return result;
        }

        /// <summary>
        /// 从摘要类恢复完整对象。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public static List<ChangeItem> ToChangesItem(IEnumerable<ChangesItemSummary> objs, GameChar gameChar)
        {
            var results = new List<ChangeItem>();
            var dic = gameChar.AllChildren.ToDictionary(c => c.Id);
            foreach (var obj in objs)
            {
                var result = new ChangeItem()
                {
                    ContainerId = obj.ContainerId,
                    DateTimeUtc = obj.DateTimeUtc
                };
                result.Adds.AddRange(obj.AddIds.Select(c => dic.GetValueOrDefault(c)).Where(c => c != null));
                result.Changes.AddRange(obj.ChangeIds.Select(c => dic.GetValueOrDefault(c)).Where(c => c != null));
                result.Removes.AddRange(obj.AddIds);
                results.Add(result);
            }
            return results;
        }

        [DataMember]
        public Guid ContainerId { get; set; }

        [DataMember]
        public List<Guid> AddIds { get; set; } = new List<Guid>();

        [DataMember]
        public List<Guid> RemoveIds { get; set; } = new List<Guid>();

        [DataMember]
        public List<Guid> ChangeIds { get; set; } = new List<Guid>();

        [DataMember]
        public DateTime DateTimeUtc { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
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
        /// <param name="gameItem"></param>
        /// <param name="template"></param>
        public static void ChangeTemplate(this GameItem gameItem, GameItemTemplate template)
        {
            var keysBoth = gameItem.Properties.Keys.Intersect(template.Properties.Keys).ToArray();
            var keysNew = template.Properties.Keys.Except(keysBoth).ToArray();
            foreach (var key in keysNew)    //新属性
            {
                var newValue = template.Properties.GetValueOrDefault(key);
                if (newValue is decimal[] ary)   //若是一个序列属性
                {
                    var indexName = template.GetIndexPropertyName(key); //索引属性名
                    if (gameItem.TryGetPropertyWithFcp(indexName, out var index) || template.Properties.TryGetDecimal(indexName, out index))
                    {
                        index = Math.Round(index, MidpointRounding.AwayFromZero);
                        gameItem.SetPropertyValue(key, ary[(int)index]);
                    }
                    else
                        gameItem.SetPropertyValue(key, ary[0]);
                }
                else
                    gameItem.SetPropertyValue(key, newValue);
            }
            foreach (var key in keysBoth)   //遍历两者皆有的属性
            {
                var currentVal = gameItem.GetPropertyOrDefault(key);
                var oldVal = gameItem.GetTemplate().Properties.GetValueOrDefault(key);    //模板值
                if (oldVal is decimal[] ary && OwConvert.TryToDecimal(currentVal, out var currentDec))   //若是一个序列属性
                {
                    var lv = gameItem.GetIndexPropertyValue(key);    //当前等级
                    var nVal = currentDec - ary[lv] + template.GetSequencePropertyValueOrDefault<decimal>(key, lv); //求新值
                    gameItem.SetPropertyValue(key, nVal);
                }
                else if (OwConvert.TryToDecimal(currentVal, out var dec)) //若是一个数值属性
                {
                    OwConvert.TryToDecimal(gameItem.GetTemplate().Properties.GetValueOrDefault(key, 0), out var nDec);    //当前模板中该属性
                    OwConvert.TryToDecimal(template.Properties.GetValueOrDefault(key), out var tDec);
                    var nVal = dec - nDec + tDec;
                    gameItem.SetPropertyValue(key, nVal);
                }
                else //其他类型属性
                {
                    gameItem.SetPropertyValue(key, template.Properties.GetValueOrDefault(key));
                }
            }
            gameItem.ExtraGuid = template.Id;
            gameItem.SetTemplate((GameThingTemplateBase)template);
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

}