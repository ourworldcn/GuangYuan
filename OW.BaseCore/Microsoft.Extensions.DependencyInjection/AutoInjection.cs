using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 自动将类注册为服务。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class OwAutoInjectionAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="lifetime">服务的生存期。</param>
        public OwAutoInjectionAttribute(ServiceLifetime lifetime)
        {
            _Lifetime = lifetime;
        }

        readonly ServiceLifetime _Lifetime;
        /// <summary>
        /// 获取或设置服务的类型。
        /// </summary>
        public ServiceLifetime Lifetime
        {
            get { return _Lifetime; }
        }

        /// <summary>
        /// 服务的类型。可能返回null,表示使用实现类相同类型的服务类型。
        /// </summary>
        public Type ServiceType { get; set; }
    }

    public static class OwAutoInjectionExtensions
    {
        /// <summary>
        /// 自动注册指定程序集内的服务类型，这些类型必须是用<see cref="OwAutoInjectionAttribute"/>标记的可实例化类。
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static IServiceCollection AutoRegister(this IServiceCollection services, IEnumerable<Assembly> assemblies = null)
        {
            assemblies ??= AppDomain.CurrentDomain.GetAssemblies();
            var coll = assemblies.SelectMany(c => c.GetTypes()).Where(c => c.GetCustomAttribute<OwAutoInjectionAttribute>() != null);
            foreach (var item in coll)
            {
                var att = item.GetCustomAttribute<OwAutoInjectionAttribute>();
                switch (att.Lifetime)
                {
                    case ServiceLifetime.Singleton:
                        services.AddSingleton(att.ServiceType ?? item, item);
                        break;
                    case ServiceLifetime.Scoped:
                        services.AddScoped(att.ServiceType ?? item, item);
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient(att.ServiceType ?? item, item);
                        break;
                    default:
                        break;
                }
            }
            return services;
        }
    }
}
