/*
 * 对象池的一些简单补充。
 * 对象池仅仅为了存储数据的对象不频繁生成回收，不适合单独成为一个服务。
 */
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

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

    public class StringBuilderPool
    {
        public static readonly ObjectPool<StringBuilder> Shared;

        [MethodImpl(MethodImplOptions.Synchronized)]
        static StringBuilderPool()
        {
            Shared ??= new DefaultObjectPoolProvider().CreateStringBuilderPool();
        }
    }

    /// <summary>
    /// 对象的内存池。
    /// 若类型支持公共方法且签名为Clear()，则在对象回池前会自动调用。
    /// </summary>
    /// <remarks>
    /// 此类有助于避免GC。通常这是服务器编程才会使用到的类。
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    [OwAutoInjection(ServiceLifetime.Singleton, ServiceType = typeof(AutoClearPool<>))]
    public class AutoClearPool<T> : DefaultObjectPool<T> where T : class, new()
    {
        private class CollectionPooledObjectPolicy : DefaultPooledObjectPolicy<T>
        {
            MethodInfo _Clear;

            /// <summary>
            /// 构造函数。
            /// </summary>
            /// <param name="clear">必须是一个无参数且无返回值的实例方法。</param>
            public CollectionPooledObjectPolicy(MethodInfo clear)
            {
                _Clear = clear;
            }

            /// <summary>
            /// 构造函数。
            /// </summary>
            public CollectionPooledObjectPolicy()
            {
                _Clear = typeof(T).GetMethod("Clear", Array.Empty<Type>());
            }


            public override bool Return(T obj)
            {
                if (_Clear is null)
                    return base.Return(obj);
                else
                {
                    var b = base.Return(obj);
                    if (b)
                        _Clear.Invoke(obj, null);
                    return b;
                }
            }
        }

        /// <summary>
        /// 可重复使用 <see cref="GeneralPool{T}"/> 类型实例的资源池的公有实例。
        /// </summary>
        public static readonly ObjectPool<T> Shared;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public AutoClearPool() : base(new CollectionPooledObjectPolicy())
        {

        }

        /// <summary>
        /// 构造函数。
        /// netcore v3.1实测最大容量是是CPU内核数的2倍。12cpu23元素数组。
        /// </summary>
        /// <param name="policy"></param>
        public AutoClearPool(IPooledObjectPolicy<T> policy) : base(policy)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="policy"></param>
        /// <param name="maximumRetained">最大</param>
        public AutoClearPool(IPooledObjectPolicy<T> policy, int maximumRetained) : base(policy, maximumRetained)
        {
        }

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static AutoClearPool()
        {
            var pool = new AutoClearPool<T>(new CollectionPooledObjectPolicy());
            Interlocked.CompareExchange(ref Shared, pool, null);
        }


    }
}

