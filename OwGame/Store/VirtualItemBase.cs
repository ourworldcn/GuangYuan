using System;

namespace OW.Game.Store
{
    /// <summary>
    /// 物品的抽象接口。
    /// </summary>
    public interface IVirtualItem<T> : IVirtualThing<T> where T : IEntityWithSingleKey<Guid>
    {
        /// <summary>
        /// 物品的数量。习惯性把不可数物品的数量置为1(<see cref="decimal.One"/>)。
        /// </summary>
        public decimal Count { get; set; }

        /// <summary>
        /// 作为容器时的最大容量。
        /// </summary>
        public decimal MaxCapacity { get; set; }

    }

    public abstract class VirtualItemBase<T> : VirtualThingBase<T>, IVirtualItem<T> where T : GuidKeyObjectBase
    {
        public VirtualItemBase()
        {

        }

        protected VirtualItemBase(Guid id) : base(id)
        {
        }

        public abstract decimal Count { get; set; }

        public abstract decimal MaxCapacity { get; set; }
    }

    public class VirtualItem : VirtualItemBase<VirtualItem>, IVirtualItem<VirtualItem>
    {
        public VirtualItem()
        {
        }

        public VirtualItem(Guid id) : base(id)
        {
        }

        public override decimal Count { get; set; }
        public override decimal MaxCapacity { get; set; }
    }
}
