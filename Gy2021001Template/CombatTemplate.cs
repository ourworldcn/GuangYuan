using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace GuangYuan.GY001.TemplateDb
{
    /// <summary>
    /// 掉落限制表。
    /// </summary>
    [Table("掉落限制")]
    public class DungeonLimit : GameTemplateBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public DungeonLimit()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id"></param>
        public DungeonLimit(Guid id) : base(id)
        {
        }


        /// <summary>
        /// 关卡Id。
        /// </summary>
        [Column("关卡Id", Order = 10)]
        public Guid DungeonId { get; set; }

        /// <summary>
        /// 物品Id。
        /// </summary>
        [Column("物品Id", Order = 15)]
        public Guid ItemTemplateId { get; set; }

        /// <summary>
        /// 组标识。
        /// </summary>
        [Column("组号", Order = 20)]
        public int GroupNumber { get; set; }

        /// <summary>
        /// 最大掉落数量。
        /// </summary>
        [Column("最大掉落数量", Order = 25)]
        public decimal MaxCount { get; set; }

        /// <summary>
        /// 本组最大掉落数量。
        /// </summary>
        [Column("本组最大掉落数量", Order = 30)]
        public decimal MaxCountOfGroup { get; set; }

    }
}
