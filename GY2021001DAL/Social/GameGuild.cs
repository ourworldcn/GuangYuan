using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace GuangYuan.GY001.UserDb.Social
{
    /// <summary>
    /// 工会内权限。
    /// </summary>
    public enum GuildDivision
    {
        待批准 = 0,
        见习会员 = 10,
        执事 = 14,
        会长 = 20,
    }

    /// <summary>
    /// 记录工会的数据结构。
    /// </summary>
    public class GameGuild : GameThingBase
    {
        public GameGuild()
        {
        }

        public GameGuild(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 行会的名字。
        /// </summary>
        [MaxLength(64)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 直接下属物品或建筑。
        /// </summary>
        [NotMapped]
        public List<GameItem> Items { get; } = new List<GameItem>();

        public override DbContext GetDbContext()
        {
            return RuntimeProperties.TryGetValue("DbContext", out var db) ? db as DbContext : null;
        }

    }
}
