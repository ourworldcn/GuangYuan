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
        #region 属性及相关

        private readonly IServiceProvider _Service;
        public IServiceProvider Service { get => _Service; }

        private readonly TOptions _Options;
        public TOptions Options { get => _Options; }

        private VWorld _VWorld;
        /// <summary>
        /// 获取游戏世界的服务对象。
        /// </summary>
        public VWorld World
        {
            get => _VWorld ??= _Service.GetService<VWorld>();   //一定是单例，所以无所谓并发
        }

        /// <summary>
        /// 同步锁
        /// </summary>
        protected readonly object ThisLocker = new object();

        #endregion 属性及相关

        #region 构造函数

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
            _Service = service;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        public GameManagerBase(IServiceProvider service, TOptions options)
        {
            _Service = service;
            _Options = options;
        }

        #endregion 构造函数

    }
}
