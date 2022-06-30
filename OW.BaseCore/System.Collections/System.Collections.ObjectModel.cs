
using System.Collections.Generic;

namespace System.Collections.ObjectModel
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// 移除与指定的谓词所定义的条件相匹配的所有元素。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="match">用于定义要移除的元素应满足的条件。</param>
        /// <returns>移除的元素数。</returns>
        public static int RemoveAll<T>(this Collection<T> obj, Predicate<T> match)
        {
            var result = 0;
            for (int i = obj.Count - 1; i >= 0; i--)
            {
                if (match(obj[i]))
                {
                    obj.RemoveAt(i);
                    result++;
                }
            }
            return result;
        }


    }


}