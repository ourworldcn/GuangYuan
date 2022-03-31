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

    public readonly ref struct DisposeHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="action"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposeHelper Create<TState>(Action<TState> action, TState state)
        {
            var result = new DisposeHelper(c => action((TState)c), state);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lockFunc"></param>
        /// <param name="unlockFunc"></param>
        /// <param name="obj"></param>
        /// <param name="timout"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposeHelper Create<T>(Func<T, TimeSpan, bool> lockFunc, Action<T> unlockFunc, T obj, TimeSpan timout)
        {
            if (!lockFunc(obj, timout))
                return new DisposeHelper(null, null);
            return new DisposeHelper(c => unlockFunc((T)c), obj);
        }

        public DisposeHelper(Action<object> action, object state)
        {
            _Action = action;
            _State = state;
        }

        private readonly Action<object> _Action;
        private readonly object _State;

        /// <summary>
        /// 判断此结构是不是一个空结构。
        /// </summary>
        public readonly bool IsEmpty { get => _Action is null; }

        /// <summary>
        /// 处置函数。
        /// </summary>
        public readonly void Dispose()
        {
            try
            {
                if (null != _Action)
                    _Action(_State);
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message);
            }
        }

    }

    public static class DisposeUtil
    {
        public static bool Create(out DisposeHelper helper)
        {
            helper = new DisposeHelper(null, null);
            return true;
        }

        public static ref DisposeHelper tt(ref DisposeHelper dh)
        {
            return ref dh;
        }

        public static void testc()
        {
            var ss = new DisposeHelper(null, null);
            ref var s = ref tt(ref ss);
        }
    }
}
