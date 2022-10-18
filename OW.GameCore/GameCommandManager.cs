using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
            return services;
        }
    }

    /// <summary>
    /// 游戏命令对象的专用标记接口。
    /// </summary>
    public interface IGameCommand
    {

    }

    /// <summary>
    /// 游戏命令处理器的基础接口。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IGameCommandHandler<T> where T : IGameCommand
    {
        /// <summary>
        /// 命令处理函数。
        /// </summary>
        /// <param name="command"></param>
        public abstract void Handle(T command);
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class GameCommandBase : IGameCommand
    {
        #region IResultWorkData接口相关

        private bool? _HasError;

        /// <summary>
        /// 是否有错误。不设置则使用<see cref="ErrorCode"/>来判定。
        /// </summary>
        /// <value>0没有错误，其它数值含义由应用定义。</value>
        public bool HasError { get => _HasError ??= ErrorCode != 0; set => _HasError = value; }

        /// <summary>
        /// 错误码，参见 ErrorCodes。
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

        #endregion IResultWorkData接口相关

    }

    public static class GameCommandBaseExtensions
    {
        /// <summary>
        /// 从<see cref="VWorld"/>对象获取错误信息。
        /// </summary>
        /// <param name="obj"></param>
        public static void FillErrorFromWorld(this GameCommandBase obj)
        {
            obj.ErrorCode = OwHelper.GetLastError();
            obj.DebugMessage = OwHelper.GetLastErrorMessage();
            obj.HasError = 0 != obj.ErrorCode;
        }

        /// <summary>
        /// 从另一个对象填充错误。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="src"></param>
        public static void FillErrorFrom(this GameCommandBase obj, GameCommandBase src)
        {
            obj.ErrorCode = src.ErrorCode;
            obj.DebugMessage = src.DebugMessage;
            obj.HasError = src.HasError;
        }
    }

    public abstract class GameCommandHandlerBase<T> : IGameCommandHandler<T> where T : IGameCommand
    {
        public GameCommandHandlerBase()
        {

        }

        public abstract void Handle(T command);
    }
}
