using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 分散存储的对象。
    /// </summary>
    [Table("FreeGameThing")]
    public class FreeGameThing : GameThingBase
    {
        public FreeGameThing()
        {
        }

        public FreeGameThing(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 并发令牌。
        /// </summary>
        [ConcurrencyCheck]
        public byte[] Timestamp { get; set; }

        public override DbContext GetDbContext()
        {
            return default;
        }
    }
}
