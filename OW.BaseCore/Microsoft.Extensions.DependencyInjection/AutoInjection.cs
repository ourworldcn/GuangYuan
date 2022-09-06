using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{

    public interface IAutoInjectService
    {
        /// <summary>
        /// 是否应调用<see cref="Registration(IServiceCollection)"/>自行注册。
        /// true调用函数注册，false则使用<see cref="ServiceType"/>和<see cref="ImplementationType"/>属性注册。
        /// </summary>
        virtual public bool IsSelfRegistration => false;

        IServiceCollection Registration(IServiceCollection services) => services;

        /// <summary>
        /// 获取服务类型。
        /// </summary>
        virtual Type ServiceType => GetType();

        /// <summary>
        /// 获取实现类型。
        /// </summary>
        virtual Type ImplementationType => GetType();
    }

    public interface ISingletonService : IAutoInjectService
    {

    }

    public class MyClass : ISingletonService
    {
        public Type ImplementationType => GetType();

        IServiceCollection Registration(IServiceCollection services) => services;

    }

    public static class OwAutoInjectionExtensions
    {
        public static IServiceCollection AutoRegister(this IServiceCollection services)
        {
            var coll = AppDomain.CurrentDomain.GetAssemblies().SelectMany(c => c.GetTypes()).Where(c => c is IAutoInjectService);
            foreach (var item in coll)
            {
                //if (item.IsSelfRegistration)
                //    item.Registration(services);
                //else if (item is ISingletonService)
                //{
                //    services.AddSingleton(item.ServiceType, item.ImplementationType);
                //}
            }
            return services;
        }
    }
}
