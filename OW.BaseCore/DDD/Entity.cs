/*
 * Seedwork（适用于域模型的可重用基类和接口）
 * 这是许多开发者在项目之间共享的复制和粘贴重用类型，不是正式框架。 seedwork 可存在于任何层或库中。 但是，如果类和接口的集足够大，可能需要创建单个类库。
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace OW.DDD
{
    public abstract class EntityBase
    {
        //int? _requestedHashCode;
        //int _Id;
        //private List<INotification> _domainEvents;
        //public virtual int Id
        //{
        //    get
        //    {
        //        return _Id;
        //    }
        //    protected set
        //    {
        //        _Id = value;
        //    }
        //}

        //public List<INotification> DomainEvents => _domainEvents;
        //public void AddDomainEvent(INotification eventItem)
        //{
        //    _domainEvents = _domainEvents ?? new List<INotification>();
        //    _domainEvents.Add(eventItem);
        //}
        //public void RemoveDomainEvent(INotification eventItem)
        //{
        //    if (_domainEvents is null) return;
        //    _domainEvents.Remove(eventItem);
        //}

        //public bool IsTransient()
        //{
        //    return this.Id == default(Int32);
        //}

        //public override bool Equals(object obj)
        //{
        //    if (obj == null || !(obj is Entity))
        //        return false;
        //    if (Object.ReferenceEquals(this, obj))
        //        return true;
        //    if (this.GetType() != obj.GetType())
        //        return false;
        //    Entity item = (Entity)obj;
        //    if (item.IsTransient() || this.IsTransient())
        //        return false;
        //    else
        //        return item.Id == this.Id;
        //}

        //public override int GetHashCode()
        //{
        //    if (!IsTransient())
        //    {
        //        if (!_requestedHashCode.HasValue)
        //            _requestedHashCode = this.Id.GetHashCode() ^ 31;
        //        // XOR for random distribution. See:
        //        // https://docs.microsoft.com/archive/blogs/ericlippert/guidelines-and-rules-for-gethashcode
        //        return _requestedHashCode.Value;
        //    }
        //    else
        //        return base.GetHashCode();
        //}
        //public static bool operator ==(Entity left, Entity right)
        //{
        //    if (Object.Equals(left, null))
        //        return (Object.Equals(right, null));
        //    else
        //        return left.Equals(right);
        //}
        //public static bool operator !=(Entity left, Entity right)
        //{
        //    return !(left == right);
        //}
    }

    public interface IEntity
    {
        Guid Id { get; }
    }

    public interface IAggregateRoot : IEntity
    {

    }

    public interface IRepository<out T> where T : IAggregateRoot
    {
        //IUnitOfWork UnitOfWork { get; }
    }

    public interface IUnitOfWork
    {

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

    public class NotificationBase : INotification
    {
        #region 构造函数

        public NotificationBase()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="contextId">上下文id。</param>
        /// <param name="service">使用的服务。</param>
        public NotificationBase(Guid contextId, IServiceProvider service)
        {
            ContextId = contextId;
            Service = service;
        }

        #endregion 构造函数

        /// <summary>
        /// 上下文id,未来应对应处理当前命令的上下文对象id,目前是发送命令的角色id。
        /// </summary>
        public Guid ContextId { get; set; }

        /// <summary>
        /// 使用的服务容器。
        /// </summary>
        public IServiceProvider Service { get; set; }
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

    public abstract class NotificationHandlerBase<T> : INotificationHandler<T> where T : INotification
    {
        public void Handle(object data)
        {
            Handle((T)data);
        }

        public abstract void Handle(T data);
    }

    public abstract class ValueObject
    {
        //    protected static bool EqualOperator(ValueObject left, ValueObject right)
        //    {
        //        if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
        //        {
        //            return false;
        //        }
        //        return ReferenceEquals(left, right) || left.Equals(right);
        //    }

        //    protected static bool NotEqualOperator(ValueObject left, ValueObject right)
        //    {
        //        return !(EqualOperator(left, right));
        //    }

        //    protected abstract IEnumerable<object> GetEqualityComponents();

        //    public override bool Equals(object obj)
        //    {
        //        if (obj == null || obj.GetType() != GetType())
        //        {
        //            return false;
        //        }

        //        var other = (ValueObject)obj;

        //        return this.GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        //    }

        //    public override int GetHashCode()
        //    {
        //        return GetEqualityComponents()
        //            .Select(x => x != null ? x.GetHashCode() : 0)
        //            .Aggregate((x, y) => x ^ y);
        //    }
        //    // Other utility methods
    }

}
