using System;
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

        public Action DisposeAction
        {
            get;
            set;
        }

        public DisposerWrapper(Action disposeAction)
        {
            DisposeAction = disposeAction;
        }

        public DisposerWrapper(Action<object> action,object state)
        {
            DisposeAction = () => action(state);
        }

        public DisposerWrapper(Func<ValueTask> disposeAction)
        {
            DisposeAction = () => disposeAction?.Invoke();
        }

        public void Dispose()
        {
            DisposeAction?.Invoke();
        }

    }
}
