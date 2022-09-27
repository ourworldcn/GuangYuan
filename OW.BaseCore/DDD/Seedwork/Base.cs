/*
 * Seedwork（适用于域模型的可重用基类和接口）
 * 这是许多开发者在项目之间共享的复制和粘贴重用类型，不是正式框架。 seedwork 可存在于任何层或库中。 但是，如果类和接口的集足够大，可能需要创建单个类库。
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace OW.DDD
{

    public interface IEntity
    {
        Guid Id { get; }
    }

    //public interface IOrderRepository : IRepository<Order>
    //{
    //    Order Add(Order order);

    //    void Update(Order order);

    //    Task<Order> GetAsync(int orderId);
    //}

    /// <summary>
    /// 实现此接口的类就被认为是发布的事件类。确切的说是事件数据。
    /// </summary>
    public interface INotification
    {
        //public string UserId { get; }
        //public string UserName { get; }
        //public int CardTypeId { get; }
        //public string CardNumber { get; }
        //public string CardSecurityNumber { get; }
        //public string CardHolderName { get; }
        //public DateTime CardExpiration { get; }
        //public Order Order { get; }

        //public OrderStartedDomainEvent(Order order, string userId, string userName,
        //                               int cardTypeId, string cardNumber,
        //                               string cardSecurityNumber, string cardHolderName,
        //                               DateTime cardExpiration)
        //{
        //    Order = order;
        //    UserId = userId;
        //    UserName = userName;
        //    CardTypeId = cardTypeId;
        //    CardNumber = cardNumber;
        //    CardSecurityNumber = cardSecurityNumber;
        //    CardHolderName = cardHolderName;
        //    CardExpiration = cardExpiration;
        //}
    }

    public interface INotificationHandler
    {
        public abstract void Handle(object data);

    }

    /// <summary>
    /// 实现此接口的类就被认为是订阅 <typeparamref name="T"/> 事件的类。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INotificationHandler<T> : INotificationHandler where T : INotification
    {

    }

}
