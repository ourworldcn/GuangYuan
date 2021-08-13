using OW.Game;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 社交关系对象。
    /// 一些附属信息放在<see cref="GameSocialBase.Properties"/>中。
    /// 本对象的Id是主体Id。
    /// 主体和客体对象是联合主键。
    /// 当前以如下方法判断具体状态:
    /// <code>
    ///             GameSocialRelationship sr;
    ///             if(sr.Friendliness<-5)  //若是黑名单
    ///             {
    ///             }
    ///             else
    ///             {
    ///                 var confirmed = sr.Properties.GetValueOrDefault(SocialConstant.ConfirmedFriendPName, decimal.Zero);
    ///                 if (confirmed == decimal.Zero) //若在申请好友中
    ///                 {
    ///                 }
    ///                 else if(sr.Friendliness>5)//若已经是好友
    ///                 {
    ///                 }
    ///                 else    //一般关系，不是黑白名单，但两人可能曾经是有社交关系的，此项通常可以忽略。未来有复杂社交关系时，此处有更多判断和意义
    ///                 {
    ///                 }
    ///             }
    /// </code>
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
        /// 客体实体Id。
        /// </summary>
        public Guid ObjectId { get; set; }

        /// <summary>
        /// 左看右的友好度。
        /// 小于-5则是黑名单，大于5是好友。目前这个字段仅使用-6和6两个值。
        /// </summary>
        public sbyte Friendliness { get; set; } = 0;

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
        public static void Fill(GameChar gameChar, CharSummary charSummary, IQueryable<GameActionRecord> records)
        {
            charSummary.Id = gameChar.Id;
            charSummary.DisplayName = gameChar.DisplayName;
            charSummary.Level = (int)gameChar.Properties.GetDecimalOrDefault("lv", decimal.Zero);
            charSummary.CombatCap = 4000;
            var ary = records.Where(c => c.ParentId == gameChar.Id && (c.ActionId == "Logout" || c.ActionId == "Login")).
                 OrderByDescending(c => c.DateTimeUtc).Take(1).ToArray();
            charSummary.LastLogoutDatetime = ary.Length == 0 || ary[0].ActionId == "Logout" ? new DateTime?() : ary[0].DateTimeUtc;
        }

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
    }

    /// <summary>
    /// 好友信息摘要类。
    /// </summary>
    public class FrientSummary : CharSummary
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public FrientSummary()
        {
        }

        /// <summary>
        /// 用指定角色填充好友摘要信息。
        /// </summary>
        /// <param name="gameChar">角色对象。</param>
        /// <param name="charSummary">好友摘要对象。</param>
        /// <param name="records">查询操作记录的接口。</param>
        public static void Fill(GameChar gameChar, FrientSummary charSummary, IQueryable<GameActionRecord> records)
        {
            CharSummary.Fill(gameChar, charSummary, records);
        }

    }

    /// <summary>
    /// 正在申请成为自己好友的人的信息摘要类。
    /// </summary>
    public class RequestingSummary : CharSummary
    {

    }
}
