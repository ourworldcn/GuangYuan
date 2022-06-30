
namespace System.Collections.Generic
{
    public static class OwEnumerableExtensions
    {
        /// <summary>
        /// 对 集合 的每个元素执行指定操作。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="action"></param>
        /// <exception cref="ArgumentNullException">action 为 null。</exception>
        /// <exception cref="InvalidOperationException">已修改集合中的某个元素。</exception>
        public static void ForEach<T>(this IEnumerable<T> obj, Action<T> action)
        {
            foreach (var item in obj)
                action(item);
        }
    }
}