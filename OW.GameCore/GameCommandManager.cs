using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OW.Game
{
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class GameCommandManager
    {
        public GameCommandManager(IServiceProvider service)
        {
            _Service = service;
        }

        IServiceProvider _Service;

        public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();

        public void Handle<T>(T command) where T : IGameCommand
        {
            var coll = _Service.GetServices<IGameCommandHandler<T>>();
            coll.SafeForEach(c =>
            {
                orderNumber++;
                c.Handle(command);
            });
        }

        private int orderNumber;
        public int OrderNumber { get => orderNumber; set => orderNumber = value; }
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
