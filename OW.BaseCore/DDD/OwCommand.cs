using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OW.DDD
{
    /// <summary>
    /// 命令服务。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class OwCommand:IDisposable
    {
        public OwCommand()
        {

        }

        public OwCommand(IServiceProvider service)
        {
            _Service = service;
        }

        IServiceProvider _Service;
        private bool disposedValue;

        public TR Handle<TI, TR>(TI command)
        {
            var svc = _Service.GetService<ICommandHandler<TI, TR>>();
            return svc.Handle(command);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~OwCommand()
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
    }

    public static class OwCommandExtensions
    {
        public static IServiceCollection Register(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var coll = assemblies.SelectMany(c => c.GetTypes()).Where(c => c.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));
            return services;
        }
    }
}
