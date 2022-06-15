using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Game
{
    /// <summary>
    /// 游戏管理类(服务)的基类。原则上派生类应该都是单例生存期的服务。
    /// </summary>
    /// <remarks>派生类如果没有特别说明，非私有成员都应该可以支持多线程并发调用。</remarks>
    public abstract class GameManagerBase<TOptions>
    {
        #region 属性及相关

        private readonly IServiceProvider _Services;
        public IServiceProvider Service => _Services;

        private readonly TOptions _Options;
        public TOptions Options => _Options;

        private VWorld _VWorld;
        /// <summary>
        /// 获取游戏世界的服务对象。
        /// </summary>
        public VWorld World
        {
            get => _VWorld ??= _Services.GetRequiredService<VWorld>();   //一定是单例，所以无所谓并发
        }

        /// <summary>
        /// 同步锁对象。
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
        /// <param name="service">配置数据。</param>
        public GameManagerBase(IServiceProvider service)
        {
            _Services = service;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service">使用的服务容器。</param>
        /// <param name="options">配置数据。</param>
        public GameManagerBase(IServiceProvider service, TOptions options)
        {
            _Services = service;
            _Options = options;
        }

        #endregion 构造函数

        Task _InitializeTask;

        /// <summary>
        /// 重载此函数用于异步初始化。
        /// </summary>
        /// <returns>异步初始化的任务对象。</returns>
        protected virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 等待异步初始化结束。
        /// </summary>
        /// <param name="timeout">等待的超时。<see cref="TimeSpan.Zero"/>立即返回结果，<see cref="Timeout.InfiniteTimeSpan"/>则直到完成才返回。</param>
        /// <returns>true已经完成或没有初始化函数，false未完成初始化。</returns>
        public bool WaitInitialize(TimeSpan timeout)
        {
            return _InitializeTask?.Wait(timeout) ?? true;
        }
    }
}
