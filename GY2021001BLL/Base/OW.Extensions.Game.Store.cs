using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using OW.Game;
using OW.Game.Mission;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

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
        //    charSummary.Level = (int)gameChar.GetSdpDecimalOrDefault("lv", decimal.Zero);
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
        /// 老版本持久化的变化数据。
        /// </summary>
        public List<ChangesItemSummary> ChangeItems { get; set; } = new List<ChangesItemSummary>();
    }

    /// <summary>
    /// 存储在<see cref="GameChar"/>对象中<see cref="GameObjectBase.JsonObject"/>属性中的对象。
    /// </summary>
    public class CharJsonEntity
    {
        public CharJsonEntity()
        {
            
        }

        public CharJsonEntity(GameChar gc)
        {
            GameChar = gc;
        }

        [JsonIgnore]
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 角色的等级。
        /// </summary>
        public int Lv { get; set; }
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
            thing.TryGetSdpDateTime("CreateUtc", out var result) ? new DateTime?(result) : null;

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
                thing.SetSdp("CreateUtc", value.Value.ToString());
            }
            else
            {
                thing.RemoveSdp("CreateUtc");
            }
        }

        /// <summary>
        /// 客户端要记录的一些属性，这个属性客户端可以随意更改，服务器不使用。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetClientString(this GameThingBase thing)
        {
            var tmp = thing.GetSdpStringOrDefault("ClientString");
            return tmp is null ? null : Uri.UnescapeDataString(tmp);
        }

        /// <summary>
        /// 客户端要记录的一些属性，这个属性客户端可以随意更改，服务器不使用。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetClientString(this GameThingBase thing, string value)
        {
            if (value is null)
                thing.RemoveSdp("ClientString");
            else
                thing.SetSdp("ClientString", Uri.EscapeDataString(value));
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
            return (int)gameItem.GetSdpDecimalOrDefault("OrderNumber");
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
                gameItem.SetSdp("OrderNumber", (decimal)value.Value);
            else
                gameItem.RemoveSdp("OrderNumber");
        }

    }

}