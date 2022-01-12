using OW.Game.Store;
using System;

namespace GuangYuan.GY001.UserDb.Combat
{
    /// <summary>
    /// 战利品记录。
    /// <see cref="SimpleDynamicPropertyBase.Properties"/>记录了物品信息，如tid是模板id,count是数量(可能是负数)。
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
        /// 所属战斗对象的Id。
        /// </summary>
        public Guid ParentId { get; set; }

        /// <summary>
        /// 所属角色(参与战斗的角色Id)。
        /// </summary>
        public Guid CharId { get; set; }

    }

    
}
