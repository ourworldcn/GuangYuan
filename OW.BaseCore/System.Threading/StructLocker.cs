/*
 * 包含一些简单的类。
 */

namespace System.Threading
{
    /// <summary>
    /// 唯一锁定对象。对值类型，使用装箱的第一个对象作为锁定的对象。对引用类型则使用第一个对象进行锁定。
    /// 第一个是针对在本对象一个实例内而言。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FirstObjectLocker<T> : UniqueObjectLocker<T, object> where T : struct
    {
        public static readonly FirstObjectLocker<T> Default;

        static FirstObjectLocker()
        {
            var locker = new FirstObjectLocker<T>();
            Interlocked.CompareExchange(ref Default, locker, null);
        }

        public FirstObjectLocker() : base(c => c)
        {
        }
    }
}