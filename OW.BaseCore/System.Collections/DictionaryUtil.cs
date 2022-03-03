using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections
{
    public class DictionaryUtil
    {
        /// <summary>
        /// 在多个字典中存在的第一个值。
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <param name="dics">字典参数数组，特别地，忽略null参数，视同没有指定键。</param>
        /// <returns></returns>
        public static bool TryGetValue<TKey, TValue>(TKey key, out TValue result, params IReadOnlyDictionary<TKey, TValue>[] dics)
        {
            foreach (var item in dics)
                if (null != item && item.TryGetValue(key, out result))
                    return true;
            result = default;
            return false;
        }

        /// <summary>
        /// 在多个字典中存在的第一个值。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <param name="dics">字典参数数组，特别地，忽略null参数，视同没有指定键。</param>
        /// <returns>true成功获取，false没有指定的键</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization|MethodImplOptions.AggressiveInlining)]
        public static bool TryGetDecimal(string key, out decimal result, params IReadOnlyDictionary<string, object>[] dics)
        {
            foreach (var item in dics)
                if (null != item && item.TryGetDecimal(key, out result))
                    return true;
            result = default;
            return false;
        }

        /// <summary>
        /// 在多个字典中存在的第一个值。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <param name="dics">字典参数数组，特别地，忽略null参数，视同没有指定键。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetString(string key, out string result, params IReadOnlyDictionary<string, object>[] dics)
        {
            foreach (var item in dics)
                if (null != item && item.TryGetString(key, out result))
                    return true;
            result = default;
            return false;
        }
    }
}

