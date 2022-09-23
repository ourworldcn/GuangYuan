﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OW.DDD;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OW.Game
{

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

    public abstract class NotificationHandlerBase<T> : INotificationHandler<T> where T : INotification
    {
        public void Handle(object data)
        {
            Handle((T)data);
        }

        public abstract void Handle(T data);
    }

    public class EventBusManagerOptions : IOptions<EventBusManagerOptions>
    {
        public EventBusManagerOptions Value => this;
    }

    /// <summary>
    /// 事件总线服务的实现。
    /// 该实现不专注于跨服务器边界的实施，仅考虑单机单进程内的实现，并以此为前提假设提供更多的功能。
    /// </summary>
    public class OwEventBus
    {
        #region 构造函数相关

        public OwEventBus(IServiceProvider service)
        {
            _Service = service;
            Initializer();
        }

        void Initializer()
        {

        }

        #endregion 构造函数相关

        IServiceProvider _Service;

        ConcurrentQueue<INotification> _Datas = new ConcurrentQueue<INotification>();

        public void AddData(INotification eventData)
        {
            _Datas.Enqueue(eventData);
        }

        public void RaiseEvent()
        {
            while (_Datas.TryDequeue(out var item))
            {
                var type = typeof(INotificationHandler<>).MakeGenericType(item.GetType());
                var svc = _Service.GetServices(type).OfType<INotificationHandler>();
                try
                {
                    svc.SafeForEach(c => c.Handle(item));
                }
                catch (Exception)
                {
                }
            }
        }
    }

    public static class EventBusManagerExtensions
    {
        public static IServiceCollection AddOwEventBus(this IServiceCollection services)
        {
            return services.AddSingleton<OwEventBus>();
        }

        public static IServiceCollection RegisterNotificationHandler(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var types = assemblies.SelectMany(c => c.GetExportedTypes()).Where(c => c.IsClass && !c.IsAbstract && typeof(INotificationHandler).IsAssignableFrom(c));
            foreach (var type in types)
            {
                var inter = type.FindInterfaces((type, obj) => type.GenericTypeArguments?.Length == 1 ? typeof(INotificationHandler<>).MakeGenericType(type.GenericTypeArguments[0]).IsAssignableFrom(type) : false, null).FirstOrDefault();
                if (null != inter)
                    services.AddSingleton(inter, type);
            }
            return services;
        }
    }

    public class GameProp : NotificationBase
    {

        public GameProp(Guid contextId, IServiceProvider service) : base(contextId, service)
        {
        }
    }

    public class MyClass : INotificationHandler<GameProp>
    {
        public MyClass(OwEventBus eventBus)
        {

        }

        public void Handle(object data)
        {
        }
    }
}
