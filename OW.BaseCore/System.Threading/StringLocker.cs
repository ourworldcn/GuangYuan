/*
 * 包含一些简单的类。
 */
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    /// <summary>
    /// 唯一字符串的全局锁的帮助类。
    /// 比较字符串的方法是<see cref="StringComparison.InvariantCulture"/>_使用区分区域性的排序规则和固定区域性比较字符串。
    /// </summary>
    public static class StringLocker
    {
        private static readonly ConcurrentDictionary<string, string> _Data = new ConcurrentDictionary<string, string>(StringComparer.InvariantCulture);

        /// <summary>
        /// 清理字符串拘留池中没有锁定的对象。
        /// </summary>
        public static void TrimExcess()
        {
            string uniStr;
            foreach (var item in _Data.Keys)
            {
                uniStr = IsInterned(item);
                if (uniStr is null || !Monitor.TryEnter(uniStr, TimeSpan.Zero))
                    continue;
                try
                {
                    if (ReferenceEquals(IsInterned(uniStr), uniStr))
                        _Data.TryRemove(uniStr, out _);
                }
                finally
                {
                    Monitor.Exit(uniStr);
                }
            }
        }

        /// <summary>
        /// 如果 key 在暂存池中，则返回对它的引用；否则返回 null。
        /// </summary>
        /// <param name="str">测试值相等的字符串。</param>
        /// <returns>如果 key 值相等的实例在暂存池中，则返回池中对象的引用；否则返回 null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string IsInterned(string str) => _Data.TryGetValue(str, out var tmp) ? tmp : null;

        /// <summary>
        /// 检索对指定 String 的引用。
        /// </summary>
        /// <param name="str"></param>
        /// <returns>如果暂存了 str值相等的实例在暂存池中，则返回池中的引用；否则返回对值为 key 的字符串的新引用，并加入池中。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Intern(string str) => _Data.GetOrAdd(str, str);

        /// <summary>
        /// 锁定字符串在当前应用程序域内的唯一实例。
        /// </summary>
        /// <param name="str">试图锁定的字符串的值，返回时可能变为池中原有对象，或无变化，锁是加在该对象上的</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryEnter(ref string str, TimeSpan timeout)
        {
            str = Intern(str);
            var start = DateTime.UtcNow;
            if (!Monitor.TryEnter(str, timeout))
                return false;
            while (!ReferenceEquals(str, IsInterned(str)))
            {
                Monitor.Exit(str);
                var tmp = OwHelper.ComputeTimeout(start, timeout);
                if (tmp == TimeSpan.Zero)   //若超时
                    return false;
                str = Intern(str);
                if (!Monitor.TryEnter(str, tmp))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// <seealso cref="TryEnter(ref string, TimeSpan)"/>
        /// </summary>
        /// <param name="str"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnter(string str, TimeSpan timeout) => TryEnter(ref str, timeout);

        /// <summary>
        /// <seealso cref="Monitor.IsEntered(object)"/>
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsEntered(string str)
        {
            str = IsInterned(str);
            if (str is null)
                return false;
            return Monitor.IsEntered(str);
        }

        /// <summary>
        /// 在字符串在当前应用程序域内的唯一实例上进行解锁。
        /// </summary>
        /// <param name="str"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Exit(string str)
        {
            var uniStr = IsInterned(str);
            Monitor.Exit(uniStr);
        }
    }

}