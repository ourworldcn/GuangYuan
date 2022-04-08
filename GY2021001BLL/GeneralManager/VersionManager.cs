using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Runtime
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    sealed class OwVersionAttribute : Attribute
    {
        /// <summary>
        /// 版本的字符串表示形式。
        /// </summary>
        readonly string _VersionString;

        // This is a positional argument
        public OwVersionAttribute(string versionString)
        {
            _VersionString = versionString;


        }

        public string PositionalString
        {
            get { return _VersionString; }
        }

        // This is a named argument
        public int NamedInt { get; set; }

        public Version GetVersion()
        {
            return new Version(_VersionString);
        }
    }

    /// <summary>
    /// 基于版本号的数据升级管理辅助类。
    /// </summary>
    public class VersionManager : IHostedService
    {
        public VersionManager()
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var methods = GetType().GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var coll = from tmp in methods
                       let attr = tmp.GetCustomAttribute<OwVersionAttribute>()
                       where null != attr
                       orderby attr.GetVersion()
                       select (tmp, attr);
            foreach (var item in coll)
            {

            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

    }
}
