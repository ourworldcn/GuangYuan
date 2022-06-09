using Microsoft.Extensions.ObjectPool;
using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace System.IO
{
    public class MemoryStreamPool : DefaultObjectPool<MemoryStream>
    {
        public static readonly MemoryStreamPool Shared;

        static MemoryStreamPool()
        {
            Interlocked.CompareExchange(ref Shared, new MemoryStreamPool(new MemoryStreamPoolPolicy()), null);
        }

        public class MemoryStreamPoolPolicy : DefaultPooledObjectPolicy<MemoryStream>
        {
            public MemoryStreamPoolPolicy()
            {
            }

            /// <summary>
            /// 还入对象必须是<see cref="MemoryStream.MemoryStream"/>构造函数构造的。
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Return(MemoryStream obj)
            {
                if (!obj.CanRead || !obj.CanSeek || !obj.CanWrite)
                    return false;
                try
                {
                    obj.SetLength(0);
                }
                catch (Exception err)
                {
                    Trace.WriteLine(err.Message);
                    return false;
                }
                return base.Return(obj);
            }
        }

        public MemoryStreamPool(IPooledObjectPolicy<MemoryStream> policy) : base(policy)
        {
        }

        public MemoryStreamPool(IPooledObjectPolicy<MemoryStream> policy, int maximumRetained) : base(policy, maximumRetained)
        {
        }
    }
}