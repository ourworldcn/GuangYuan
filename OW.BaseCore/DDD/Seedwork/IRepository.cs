/*
 * Seedwork（适用于域模型的可重用基类和接口）
 * 这是许多开发者在项目之间共享的复制和粘贴重用类型，不是正式框架。 seedwork 可存在于任何层或库中。 但是，如果类和接口的集足够大，可能需要创建单个类库。
 */

namespace OW.DDD
{

    public interface IUnitOfWork
    {

    }

    public interface IRepository<out T> where T : IAggregateRoot
    {
        #region 样例
        /*
        IUnitOfWork UnitOfWork { get; }
         */
        #endregion 样例
    }

    #region 样例
    /*
    public interface IOrderRepository : IRepository<Order>
    {
        Order Add(Order order);

        void Update(Order order);

        Task<Order> GetAsync(int orderId);
    }*/
    #endregion 样例
}
