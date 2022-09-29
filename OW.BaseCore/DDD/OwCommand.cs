using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OW.DDD
{
    /// <summary>
    /// 命令服务。
    /// </summary>
    public class OwCommandManager : IDisposable
    {
        public OwCommandManager()
        {

        }

        public OwCommandManager(IServiceProvider service)
        {
            _Service = service;
        }

        IServiceProvider _Service;
        private bool disposedValue;

        public TResult Handle<TCommand, TResult>(TCommand command) where TCommand : ICommand<TCommand>
        {
            var svc = _Service.GetService<ICommandHandler<TCommand, TResult>>();
            if (svc is null)
            {
                OwHelper.SetLastError(120);
                return default(TResult);
            }
            else
                return svc.Handle(command);
        }

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
                disposedValue = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
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
        public static IServiceCollection RegisterCommandHandler(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var coll = from tmp in assemblies.SelectMany(c => c.GetTypes())
                       let i = tmp.FindInterfaces((c1, c2) => c1.IsGenericType && c1.GetGenericTypeDefinition() == typeof(ICommandHandler<,>), null).FirstOrDefault()
                       where i != null && tmp.IsClass && !tmp.IsAbstract
                       select (Type: tmp, @interface: i);
            foreach (var item in coll)
            {
                services.AddScoped(item.@interface, item.Type);
            }
            return services;
        }
        public static IServiceCollection AddCommandManager(this IServiceCollection services)
        {
            return services.AddScoped(typeof(OwCommandManager));
        }
    }

    public abstract class CommandBase<T> : ICommand<T>
    {
    }

    public abstract class CommandResultBase<T> : ICommandResult<T>
    {
        private bool? _HasError;

        /// <summary>
        /// 是否有错误。不设置则使用<see cref="ErrorCode"/>来判定。
        /// </summary>
        /// <value>0没有错误，其它数值含义由应用定义。</value>
        public bool HasError { get => _HasError ??= ErrorCode != 0; set => _HasError = value; }


        /// <summary>
        /// 错误码。
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 调试用的提示性信息。
        /// </summary>
        private string _ErrorMessage;

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage
        {
            get => _ErrorMessage ??= new Win32Exception(ErrorCode).Message;
            set => _ErrorMessage = value;
        }

    }

    public abstract class CommandHandlerBase<TRequest, TResponse> : ICommandHandler<TRequest, TResponse> where TRequest : ICommand<TRequest>
    {
        public abstract TResponse Handle(TRequest command);
    }
}
