using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GuangYuan.GY001.UserDb.Social
{
    /// <summary>
    /// 记录联盟的数据结构。此类实装，需要世界架构确定。
    /// </summary>
    public class GameAlliance : GameThingBase
    {
        public GameAlliance()
        {
        }

        public GameAlliance(Guid id) : base(id)
        {
        }

        //private List<GameGuild> guildCollection = new List<GameGuild>();

        ///// <summary>
        ///// 游戏工会。
        ///// </summary>
        //public virtual List<GameGuild> GuildCollection { get => guildCollection; set => guildCollection = value; }

        public override DbContext GetDbContext()
        {
            return RuntimeProperties.TryGetValue("DbContext", out var db) ? db as DbContext : null;
        }
    }
}
