
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Game.PropertyChange;
using System;
using System.Collections.Generic;

namespace OW.Game
{
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class GameCommandContext : IDisposable
    {
        private bool disposedValue;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        public GameCommandContext(IServiceProvider service)
        {
            Service = service;
            // IHttpContextAccessor
        }

        /// <summary>
        /// 获取或设置当前的服务容器，通常是一个范围的容器。
        /// </summary>
        public IServiceProvider Service { get; set; }

        public string Token { get; set; }

        private GameChar _GameChar;

        /// <summary>
        /// 获取发布当前命令的用户。可能是空。
        /// </summary>
        public GameChar GameChar
        {
            get
            {
                if (_GameChar is null)
                {
                    _GameChar = Service.GetRequiredService<GameCharManager>().GetGameCharFromToken(Token);
                }
                return _GameChar;
            }
            set => _GameChar = value;
        }

        /// <summary>
        /// 被创建的时间，一般用于标记命令开始处理的时间。
        /// </summary>
        public DateTime UtcNow { get; set; } = DateTime.UtcNow;

        #region IDisposable接口相关

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
                Service = null;
                disposedValue = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameCommandContext()
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

    public static class GameCommandContextExtensions
    {
        /// <summary>
        /// 使用默认超时试图锁定<see cref="GameChar"/>用户。
        /// </summary>
        /// <returns></returns>
        public static IDisposable LockUser(this GameCommandContext commandContext)
        {
            try
            {
                return commandContext.Service.GetRequiredService<VWorld>().CharManager.LockAndReturnDisposer(commandContext.GameChar.GameUser);
            }
            catch (Exception err)
            {
                var logger = commandContext.Service?.GetService<ILogger<GameCharGameContext>>();
                logger?.LogWarning($"锁定单个角色时出现异常——{err.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取命令管理器。
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static GameCommandManager GetCommandManager(this GameCommandContext service) => service.Service.GetService<GameCommandManager>();
    }

    public abstract class WithChangesGameCommandBase : GameCommandBase
    {
        public WithChangesGameCommandBase()
        {

        }

        private List<GamePropertyChangeItem<object>> _Changes;
        /// <summary>
        /// 获取变化集合。
        /// </summary>
        public List<GamePropertyChangeItem<object>> Changes { get => _Changes ??= new List<GamePropertyChangeItem<object>>(); }
    }
}