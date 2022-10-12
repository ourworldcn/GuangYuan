using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OW.Game
{
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class GameCommandManager : IDisposable
    {
        public GameCommandManager()
        {

        }

        public GameCommandManager(IServiceProvider service)
        {
            _Service = service;

        }

        IServiceProvider _Service;

        private Dictionary<string, object> items;
        /// <summary>
        /// 当前范围内的一些数据。
        /// </summary>
        public IDictionary<string, object> Items => items ??= AutoClearPool<Dictionary<string, object>>.Shared.Get();


        public void Handle<T>(T command) where T : IGameCommand
        {
            orderNumber = 0;
            var coll = _Service.GetServices<IGameCommandHandler<T>>();
            coll.SafeForEach(c =>
            {
                c.Handle(command);
                orderNumber++;
            });
        }

        private int orderNumber;

        public int OrderNumber { get => orderNumber; set => orderNumber = value; }

        #region IDisposable接口相关

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                if (items != null)
                {
                    AutoClearPool<Dictionary<string, object>>.Shared.Return(items);
                    items = null;
                }
                disposedValue = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameCommandManager()
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
        #endregion IDisposable接口相关
    }

    public static class GameCommandManagerExtensions
    {
        public static IServiceCollection UseGameCommand(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var coll = from tmp in assemblies.SelectMany(c => c.GetTypes())
                       let i = tmp.FindInterfaces((c1, c2) => c1.IsGenericType && c1.GetGenericTypeDefinition() == typeof(IGameCommandHandler<>), null).FirstOrDefault()
                       where i != null && tmp.IsClass && !tmp.IsAbstract
                       select (Type: tmp, @interface: i);
            foreach (var item in coll)
            {
                services.AddScoped(item.@interface, item.Type);
            }
            var b = typeof(IdleCommandHandler).FindInterfaces((c1, c2) => c1.IsGenericType && c1.GetGenericTypeDefinition() == typeof(IGameCommandHandler<>), null);
            return services;
        }
    }

    public interface IGameCommand
    {

    }

    /// <summary>
    /// 命令处理器的基础接口。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IGameCommandHandler<T> where T : IGameCommand
    {
        public void Handle(T command);
    }

    /// <summary>
    /// 
    /// </summary>
    public class IdleCommand : IGameCommand
    {

    }

    public class IdleCommandHandler : IGameCommandHandler<IdleCommand>
    {
        public IdleCommandHandler(IServiceProvider service)
        {

        }

        public void Handle(IdleCommand command)
        {

        }
    }
}
