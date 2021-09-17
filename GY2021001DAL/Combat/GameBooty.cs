using OW.Game.Store;
using System;

namespace GuangYuan.GY001.UserDb.Combat
{
    /// <summary>
    /// 战利品记录。
    /// </summary>
    public class GameBooty : GameObjectBase
    {
        public GameBooty()
        {
        }

        public GameBooty(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 所属战斗。
        /// </summary>
        public Guid ParentId { get; set; }

        /// <summary>
        /// 所属角色。
        /// </summary>
        public Guid CharId { get; set; }

        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 数量。
        /// </summary>
        public decimal Count { get; set; }

    }

    
}
