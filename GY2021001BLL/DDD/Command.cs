using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.DDD;
using OW.Game;
using OW.Game.PropertyChange;
using System;
using System.Collections.Generic;
using System.Text;

namespace GuangYuan.GY001.BLL.DDD
{
    /// <summary>
    /// 指定当前角色的命令。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class GameCharCommand<T> : WithChangesCommand<T>
    {
        protected GameCharCommand()
        {
        }

        protected GameCharCommand(GameChar gameChar, ICollection<GamePropertyChangeItem<object>> changes) : base(changes)
        {
            GameChar = gameChar;
        }

        public GameChar GameChar { get; set; }

    }

    public abstract class GameCharCommandHandler<TRequest, TResponse> : GameCommandHandler<TRequest, TResponse> where TRequest : ICommand<TRequest>
    {
        #region 构造函数

        public GameCharCommandHandler()
        {

        }

        public GameCharCommandHandler(VWorld world)
        {
            World = world;
        }

        #endregion 构造函数

        public VWorld World { get; set; }

        /// <summary>
        /// 使用默认超时试图锁定<see cref="GameChar"/>用户。
        /// </summary>
        /// <returns></returns>
        public IDisposable Lock(GameChar gameChar)
        {
            try
            {
                return World.CharManager.LockAndReturnDisposer(gameChar.GameUser);
            }
            catch (Exception err)
            {
                var logger = World.Service?.GetService<ILogger<GameCharGameContext>>();
                logger?.LogWarning($"锁定单个角色时出现异常——{err.Message}");
                return null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                World = null;
                base.Dispose(disposing);
            }
        }
    }

}
