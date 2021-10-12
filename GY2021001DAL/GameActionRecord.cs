using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 关键行为记录类。
    /// 此类可能放在玩家数据库中也可能放于专用的日志库中，但可能有些游戏内操作需要此数据。
    /// 当前没有启动第三上下文，暂时放在玩家数据库中。
    /// </summary>
    /// <remarks>
    /// <code>
    /// IQueryable<GameActionRecord> query; //一个查询对象
    /// DateTime dt = DateTime.UtcNow.Date;
    /// Guid charId = Guid.NewGuid();
    /// string actionId = "someThing";
    /// var coll = query.Where(c => c.DateTimeUtc >= dt && c.Id == charId && c.ActionId == actionId);
    /// </code>
    /// 索引在此情况下最有用。
    /// </remarks>
    public class GameActionRecord : SimpleDynamicPropertyBase
    {
        public GameActionRecord()
        {
        }

        public GameActionRecord(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 主体对象的Id。
        /// </summary>
        public Guid ParentId { get; set; }

        /// <summary>
        /// 行为Id。
        /// </summary>
        [MaxLength(64)]
        public string ActionId { get; set; }

        /// <summary>
        /// 这个行为发生的时间。
        /// </summary>
        /// <value>默认是构造此对象的UTC时间。</value>
        public DateTime DateTimeUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 一个人眼可读的说明。
        /// </summary>
        public string Remark { get; set; }
    }
}
