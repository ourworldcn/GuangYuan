using Microsoft.Extensions.ObjectPool;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// 帮助调用清理代码帮助器。应配合 C#8.0 using语法使用。
    /// 对象本身就支持对象池，不要将此对象放在其他池中。
    /// </summary>
    //[DebuggerNonUserCode()]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                List<Exception> exceptions = new List<Exception>();
                foreach (var item in c)
                {
                    try
                    {
                        item.Dispose();
                    }
                    catch (Exception err)
                    {
                        exceptions.Add(err);
                    }
                }
                AggregateException aggregate;
                if (exceptions.Count > 0)
                    aggregate = new AggregateException(exceptions);
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

    /// <summary>
    /// 清理代码帮助器结构。实测比使用对象池要快20%左右。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly ref struct DisposeHelper<T>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="action">要运行的清理函数。</param>
        /// <param name="state">清理函数的参数。</param>
        public DisposeHelper(Action<T> action, T state)
        {
            Action = action;
            State = state;
        }

        public readonly Action<T> Action;
        public readonly T State;

        /// <summary>
        /// 判断此结构是不是一个空结构。
        /// </summary>
        public readonly bool IsEmpty { get => Action is null; }

        /// <summary>
        /// 处置函数。
        /// </summary>
        public readonly void Dispose()
        {
            try
            {
                if (null != Action)
                    Action(State);
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message);
            }
        }

    }

    public static class DisposeHelper
    {
        //public static bool Create(out DisposeHelper helper)
        //{
        //    helper = new DisposeHelper(null, null);
        //    return true;
        //}

        //public static ref DisposeHelper tt(ref DisposeHelper dh)
        //{
        //    return ref dh;
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static DisposeHelper<T> Create<T>(Action<T> action, T state) =>
            new DisposeHelper<T>(action, state);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static DisposeHelper<T> Create<T>(Func<T, TimeSpan, bool> lockFunc, Action<T> unlockFunc, T state, TimeSpan timeout) =>
            lockFunc(state, timeout) ? new DisposeHelper<T>(unlockFunc, state) : new DisposeHelper<T>(null, default);
    }
}
