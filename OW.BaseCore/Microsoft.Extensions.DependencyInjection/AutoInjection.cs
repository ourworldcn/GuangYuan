using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class OwAutoInjectionAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="serviceLifetime">服务的生存期。</param>
        public OwAutoInjectionAttribute(ServiceLifetime serviceLifetime)
        {
            _ServiceLifetime = serviceLifetime;

        }

        readonly ServiceLifetime _ServiceLifetime;
        /// <summary>
        /// 获取或设置服务的类型。
        /// </summary>
        public ServiceLifetime ServiceLifetime
        {
            get { return _ServiceLifetime; }
        }

        /// <summary>
        /// 服务的类型。
        /// </summary>
        public Type ServiceType { get; set; }
    }

    public static class OwAutoInjectionExtensions
    {
        public static IServiceCollection AutoRegister(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var coll = assemblies.SelectMany(c => c.GetTypes()).Where(c => c.GetCustomAttribute<OwAutoInjectionAttribute>() != null);
            foreach (var item in coll)
            {
                var att = item.GetCustomAttribute<OwAutoInjectionAttribute>();
                switch (att.ServiceLifetime)
                {
                    case ServiceLifetime.Singleton:
                        services.AddSingleton(att.ServiceType, item);
                        break;
                    case ServiceLifetime.Scoped:
                        services.AddScoped(att.ServiceType, item);
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient(att.ServiceType, item);
                        break;
                    default:
                        break;
                }
            }
            return services;
        }
    }
}
