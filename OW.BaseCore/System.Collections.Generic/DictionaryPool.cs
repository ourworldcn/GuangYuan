/*
 * 对象池的一些简单补充。
 * 对象池仅仅为了存储数据的对象不频繁生成回收，不适合单独成为一个服务。
 */
using Microsoft.Extensions.ObjectPool;
using System.Threading;

namespace System.Collections.Generic
{
    /// <summary>
    /// 提供可重复使用 <see cref="Dictionary{TKey, TValue}"/> 类型实例的资源池。
    /// 一般情况使用此类不会提高性能，除非极其频繁的创建销毁字典导致GC的压力过大。
    /// </summary>
    public class DictionaryPool<TKey, TValue> : AutoClearPool<Dictionary<TKey, TValue>>
    {
        public DictionaryPool(IPooledObjectPolicy<Dictionary<TKey, TValue>> policy) : base(policy)
        {
        }

        public DictionaryPool(IPooledObjectPolicy<Dictionary<TKey, TValue>> policy, int maximumRetained) : base(policy, maximumRetained)
        {
        }
    }

}
