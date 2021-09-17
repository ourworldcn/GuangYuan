using Microsoft.Extensions.ObjectPool;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// 调用<see cref="IDisposable.Dispose"/>。
    /// 应配合 C#8.0 using语法使用。
    /// 对象本身就支持对象池，不要将此对象放在其他池中。
    /// 若无特别说明，本类非私有成员支持多线程并发调用。
    /// </summary>
    [DebuggerNonUserCode()]
    public sealed class DisposerWrapper : IDisposable
    {
        /// <summary>
        /// 对象池策略类。
        /// </summary>
        private class DisposerWrapperPolicy : PooledObjectPolicy<DisposerWrapper>
        {

            public DisposerWrapperPolicy()
            {
            }

            public override DisposerWrapper Create() =>
                new DisposerWrapper();

            public override bool Return(DisposerWrapper obj)
            {
                obj.DisposeAction = null;
                obj._Disposed = false;
                obj._IsInPool = true;
                return true;
            }
        }

        //private readonly static Action<IEnumerable<IDisposable>> ClearDisposables = c =>
        //{
        //    foreach (var item in c)
        //    {
        //        try
        //        {
        //            item.Dispose();
        //        }
        //        catch (Exception)
        //        {
        //        }
        //    };
        //};

        private static ObjectPool<DisposerWrapper> Pool { get; } = new DefaultObjectPool<DisposerWrapper>(new DisposerWrapperPolicy(), Math.Max(Environment.ProcessorCount * 4, 16));

        public static DisposerWrapper Create(Action action)
        {
            var result = Pool.Get();
            result._IsInPool = false;
            result.DisposeAction = action;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposerWrapper Create<T>(Action<T> action, T state) => Create(() => action(state));

        public static DisposerWrapper Create(IEnumerable<IDisposable> disposers) =>
            Create(c =>
            {
                foreach (var item in (IEnumerable<IDisposable>)c)
                {
                    try
                    {
                        item.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }, disposers);

        /// <summary>
        /// 构造函数。
        /// </summary>
        private DisposerWrapper()
        {

        }

        public Action DisposeAction
        {
            get;
            set;
        }

        private bool _Disposed;
        private bool _IsInPool;

        public void Dispose()
        {
            if (!_IsInPool && !_Disposed)
            {
                DisposeAction?.Invoke();
                _Disposed = true;
                Pool.Return(this);
            }
        }

    }
}
