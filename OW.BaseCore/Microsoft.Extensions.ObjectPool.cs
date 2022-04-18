/*
 * 对象池的一些简单补充。
 * 对象池仅仅为了存储数据的对象不频繁生成回收，不适合单独成为一个服务。
 */
using System.Buffers;
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
    /// 一般情况使用此类不会提高性能，除非极其频繁的创建销毁字典导致GC的压力过大。
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