using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    /// <summary>
    /// 调用<see cref="IDisposable.Dispose"/> 或 <see cref="IAsyncDisposable.DisposeAsync"/>的帮助器。
    /// </summary>
    [DebuggerNonUserCode()]
    public class DisposerWrapper : IDisposable
    {
        public DisposerWrapper(Action disposeAction)
        {
            DisposeAction = disposeAction;
        }

        public DisposerWrapper(Action<object> action, object state)
        {
            DisposeAction = () => action(state);
        }

        public DisposerWrapper(Func<ValueTask> disposeAction)
        {
            DisposeAction = () => disposeAction?.Invoke();
        }

        public Action DisposeAction
        {
            get;
            set;
        }

        bool _Disposed;

        public void Dispose()
        {
            if (!_Disposed)
            {
                DisposeAction?.Invoke();
                _Disposed = true;
            }
        }

    }
}
