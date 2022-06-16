using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 社交关系对象。
    /// 一些附属信息放在<see cref="GameSocialBase.Properties"/>中。
    /// 本对象的Id是主体Id。
    /// 主体和客体对象是联合主键。<see cref="GameEntityRelationshipBase.KeyType"/> SocialKeyTypes 枚举类型。
    /// 当前以如下方法判断具体状态:
    /// </summary>
    public class GameSocialRelationship : GameSocialBase
    {
        #region 扩展属性名称定义
        #endregion 扩展属性名称定义

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameSocialRelationship()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id">指定Id。</param>
        public GameSocialRelationship(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="id2"></param>
        /// <param name="flag"></param>
        /// <param name="keyType"></param>
        public GameSocialRelationship(Guid id, Guid id2, int keyType, int flag) : base(id, id2, keyType, flag)
        {
        }

    }

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
        /// 角色战力。
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
    }

    /// <summary>
    /// 正在申请成为自己好友的人的信息摘要类。
    /// </summary>
    public class RequestingSummary : CharSummary
    {

    }
}
