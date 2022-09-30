using OW.DDD;
using OW.Game.PropertyChange;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OW.Game
{
    public abstract class GameCommand<T> : CommandBase<T>
    {
    }

    public abstract class GameCommandResult<T> : CommandResultBase<T>
    {
    }

    public abstract class GameCommandHandler<TRequest, TResponse> : CommandHandlerBase<TRequest, TResponse>, IDisposable where TRequest : ICommand<TRequest>
    {
        #region IDisposable相关

        private bool _IsDisposed;

        public bool IsDisposed { get => _IsDisposed; }

        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _IsDisposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameCommandHandler()
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
        #endregion IDisposable相关
    }

    public abstract class WithChangesCommand<T> : GameCommand<T>
    {
        #region 构造函数
        public WithChangesCommand()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="changes">可以设置为空，以指示无需记录属性变化。</param>
        public WithChangesCommand(ICollection<GamePropertyChangeItem<object>> changes)
        {
            _Changes = changes;
        }

        #endregion 构造函数

        private ICollection<GamePropertyChangeItem<object>> _Changes;

        /// <summary>
        /// 变换数据。事件处理程序可以向此集合添加数据，且应假设该集合存在了0..n条数据。没有极其充分的理由不可删除或更改已存在的数据。
        /// </summary>
        public ICollection<GamePropertyChangeItem<object>> Changes => _Changes ??= new List<GamePropertyChangeItem<object>>();

    }

    public abstract class WithChangesCommandResult<T> : GameCommandResult<T>
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public WithChangesCommandResult()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="changes">指定该对象的<see cref="Changes"/>属性使用此集合。</param>
        public WithChangesCommandResult(ICollection<GamePropertyChangeItem<object>> changes)
        {
            _Changes = changes;
        }

        #endregion 构造函数

        private ICollection<GamePropertyChangeItem<object>> _Changes;
        /// <summary>
        /// 变换数据。事件处理程序可以向此集合添加数据，且应假设该集合存在了0..n条数据。没有极其充分的理由不可删除或更改已存在的数据。
        /// </summary>
        public ICollection<GamePropertyChangeItem<object>> Changes => _Changes ??= new List<GamePropertyChangeItem<object>>();
    }

}
