using Microsoft.Extensions.ObjectPool;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// 调用<see cref="IDisposable.Dispose"/>。
    /// 应配合 C#8.0 using语法使用。
    /// 对象本身就支持对象池，不要将此对象放在其他池中。
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

            public override DisposerWrapper Create()
            {
                return new DisposerWrapper();
            }

            public override bool Return(DisposerWrapper obj)
            {
                obj.DisposeAction = null;
                obj._Disposed = false;
                obj._IsInPool = true;
                return true;
            }
        }

        private static ObjectPool<DisposerWrapper> Pool { get; } = new DefaultObjectPool<DisposerWrapper>(new DisposerWrapperPolicy(), Math.Max(Environment.ProcessorCount * 4, 16));

        public static DisposerWrapper Create(Action action)
        {
            var result = Pool.Get();
            result._IsInPool = false;
            result.DisposeAction = action;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposerWrapper Create(Action<object> action, object state) =>
             Create(() => action(state));

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
