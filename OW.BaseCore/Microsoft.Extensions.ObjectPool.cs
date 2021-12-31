using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Extensions.ObjectPool
{
    public class DictionaryPooledObjectPolicy<TKey, TValue> : DefaultPooledObjectPolicy<Dictionary<TKey, TValue>>
    {
        public override bool Return(Dictionary<TKey, TValue> obj)
        {
            obj.Clear();
            return true;
        }
    }

    /// <summary>
    /// 提供可重复使用 <see cref="Dictionary{TKey, TValue}"/> 类型实例的资源池。
    /// </summary>
    public class DictionaryPool<TKey, TValue>
    {
        /// <summary>
        /// 可重复使用 <see cref="Dictionary{TKey, TValue}"/> 类型实例的资源池的公有实例。
        /// </summary>
        public static readonly ObjectPool<Dictionary<TKey, TValue>> Shared;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        static DictionaryPool()
        {
            Shared ??= new DefaultObjectPool<Dictionary<TKey, TValue>>(new DictionaryPooledObjectPolicy<TKey, TValue>(), 128);
        }
    }

    public class StringBuilderPool
    {
        public static readonly ObjectPool<StringBuilder> Shared;

        [MethodImpl(MethodImplOptions.Synchronized)]
        static StringBuilderPool()
        {
            Shared ??= new DefaultObjectPoolProvider().CreateStringBuilderPool();

        }
    }
}