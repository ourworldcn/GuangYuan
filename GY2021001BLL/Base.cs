using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace GY2021001BLL
{
    /// <summary>
    /// 游戏管理类(服务)的基类。
    /// </summary>
    public abstract class GameManagerBase<TOptions>
    {
        private readonly IServiceProvider _ServiceProvider;
        public IServiceProvider Service { get => _ServiceProvider; }

        private readonly TOptions _Options;
        public TOptions Options { get => _Options; }

        private VWorld _VWorld;
        /// <summary>
        /// 获取游戏世界的服务对象。
        /// </summary>
        public VWorld World { get => _VWorld ??= _ServiceProvider.GetService<VWorld>(); }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameManagerBase()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        public GameManagerBase(IServiceProvider service)
        {
            _ServiceProvider = service;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        public GameManagerBase(IServiceProvider service, TOptions options)
        {
            _ServiceProvider = service;
            _Options = options;
        }


    }
}
