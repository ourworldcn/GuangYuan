/*
 * 包含一些简单的类。
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    /// <summary>
    /// 将指定类型实例唯一化为一个单例对象(相对与本对象实例)，然后针对此唯一对象锁定。
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue">必须是一个引用类型。</typeparam>
    public class UniqueObjectLocker<TKey, TValue> where TValue : class
    {
        private readonly ConcurrentDictionary<TKey, TValue> _Data;

        Func<TKey, TValue> _Key2Value;

        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// 自动使用TypeDescriptor.GetConverter(typeof(TKey))的转换器，转换为 (TValue)td.ConvertTo(c, typeof(TValue))
        /// </summary>
        protected UniqueObjectLocker()
        {
            _Data = new ConcurrentDictionary<TKey, TValue>();
            var td = TypeDescriptor.GetConverter(typeof(TKey));
            if (!td.CanConvertTo(typeof(TValue)))
                throw new InvalidOperationException($"无法自动将{typeof(TKey)}转换为{typeof(TValue)}");
            _Key2Value = c => (TValue)td.ConvertTo(c, typeof(TValue));
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="key2Value"></param>
        /// <param name="comparer">比较键(TKey)相等性的接口。</param>
        protected UniqueObjectLocker(Func<TKey, TValue> key2Value, IEqualityComparer<TKey> comparer)
        {
            _Data = new ConcurrentDictionary<TKey, TValue>(comparer);
            _Key2Value = key2Value;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="key2Value"></param>
        protected UniqueObjectLocker(Func<TKey, TValue> key2Value)
        {
            _Data = new ConcurrentDictionary<TKey, TValue>();
            _Key2Value = key2Value;
        }

        #endregion 构造函数

        /// <summary>
        /// 清理字符串拘留池中没有锁定的对象。
        /// </summary>
        /// <remarks>建议在合适的实际使用后台线程后台清理，函数会逐个锁定项(<see cref="TimeSpan.Zero"/>)，然后驱逐，只要无法锁定项就会略过。</remarks>
        public void TrimExcess()
        {
            TValue value;
            foreach (var item in _Data.Keys)
            {
                value = IsInterned(item);
                if (value is null || !Monitor.TryEnter(value, TimeSpan.Zero))
                    continue;
                try
                {
                    if (ReferenceEquals(IsInterned(item), value))   //若的确是试图删除的对象
                        _Data.TryRemove(item, out _);
                }
                finally
                {
                    Monitor.Exit(value);
                }
            }
        }

        /// <summary>
        /// 如果 key 在暂存池中，则返回对它的引用；否则返回 null。
        /// </summary>
        /// <param name="key">测试值相等的字符串。</param>
        /// <returns>如果 key 值相等的实例在暂存池中，则返回池中对象的引用；否则返回 null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue IsInterned(TKey key) => _Data.TryGetValue(key, out var tmp) ? tmp : default;

        /// <summary>
        /// 检索对指定 String 的引用。
        /// </summary>
        /// <param name="key"></param>
        /// <returns>如果暂存了 str值相等的实例在暂存池中，则返回池中的引用；否则返回对值为 key 的字符串的新引用，并加入池中。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue Intern(TKey key) => _Data.GetOrAdd(key, _Key2Value);

        /// <summary>
        /// 锁定字符串在当前应用程序域内的唯一实例。
        /// </summary>
        /// <param name="key">试图锁定的字符串的值，返回时可能变为池中原有对象，或无变化，锁是加在该对象上的</param>
        /// <param name="timeout"></param>
        /// <param name="value">返回实际被锁定的对象。</param>
        /// <returns>true成功锁定，false超时。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool TryEnter(TKey key, TimeSpan timeout, out TValue value)
        {
            value = Intern(key);
            var start = DateTime.UtcNow;
            if (!Monitor.TryEnter(value, timeout))
                return false;
            while (!ReferenceEquals(value, IsInterned(key)))    //若因并发问题已经变换过键绑定的对象
            {
                Monitor.Exit(value);
                var tmp = OwHelper.ComputeTimeout(start, timeout);
                if (tmp == TimeSpan.Zero)   //若超时
                    return false;
                value = Intern(key);
                if (!Monitor.TryEnter(value, tmp))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// <seealso cref="TryEnter(ref string, TimeSpan)"/>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnter(TKey key, TimeSpan timeout) => TryEnter(key, timeout, out _);

        /// <summary>
        /// <seealso cref="Monitor.IsEntered(object)"/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsEntered(TKey key)
        {
            var value = IsInterned(key);
            if (value is null)
                return false;
            return Monitor.IsEntered(value);
        }

        /// <summary>
        /// 在字符串在当前应用程序域内的唯一实例上进行解锁。
        /// </summary>
        /// <param name="key"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exit(TKey key)
        {
            var value = IsInterned(key);
            Monitor.Exit(value);
        }
    }
}