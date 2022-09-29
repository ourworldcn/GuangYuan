using Microsoft.Extensions.DependencyInjection;
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

    public abstract class NotificationBase : INotification
    {
        #region 构造函数

        public NotificationBase()
        {

        }

        #endregion 构造函数

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
    /// 该实现不专注于跨服务器边界的实施，仅考虑单机单进程单应用程序域(AppDomain)内的实现，并以此为前提假设提供更多的功能。
    /// </summary>
    public class OwEventBus : IDisposable
    {
        #region 构造函数相关

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service">范围服务，需要此接口，以获取其它事件处理接口。</param>
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

        /// <summary>
        /// 增加一个事件数据。
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="notificationType">保留未用。</param>
        public void Add(INotification eventData, Type notificationType = null)
        {
            _Datas.Enqueue(eventData);
        }

        /// <summary>
        /// 引发队列中所有事件。
        /// </summary>
        public void Raise()
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

        #region IDisposable接口及相关

        private bool _IsDisposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Datas = null;
                _IsDisposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~OwEventBus()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable接口及相关
    }

    public static class EventBusManagerExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddOwEventBus(this IServiceCollection services)
        {
            return services.AddScoped<OwEventBus>();
        }

        public static IServiceCollection RegisterNotificationHandler(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var types = assemblies.SelectMany(c => c.GetTypes()).Where(c => c.IsClass && !c.IsAbstract && typeof(INotificationHandler).IsAssignableFrom(c));
            foreach (var type in types)
            {
                var inter = type.FindInterfaces((type, obj) => type.GenericTypeArguments?.Length == 1 ? typeof(INotificationHandler<>).MakeGenericType(type.GenericTypeArguments[0]).IsAssignableFrom(type) : false, null).FirstOrDefault();
                if (null != inter)
                    services.AddScoped(inter, type);
            }
            return services;
        }
    }

}
