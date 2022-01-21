﻿/*
 * 文件放置游戏专用的一些基础类
 */
using Microsoft.Extensions.ObjectPool;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System
{
    public static class OwHelper
    {
        /// <summary>
        /// 中英文逗号数组。分割字符串常用此数组，避免生成新对象。
        /// </summary>
        public static readonly char[] CommaArrayWithCN = new char[] { ',', '，' };

        /// <summary>
        /// 中英文分号数组。分割字符串常用此数组，避免生成新对象。
        /// </summary>
        public static readonly char[] SemicolonArrayWithCN = new char[] { ';', '；' };

        /// <summary>
        /// 中英文冒号数组。分割字符串常用此数组，避免生成新对象。
        /// </summary>
        public static readonly char[] ColonArrayWithCN = new char[] { ':', '：' };

        /// <summary>
        /// 中英文双引号。
        /// </summary>
        public static readonly char[] DoubleQuotesWithCN = new char[] { '"', '“', '”' };

        /// <summary>
        /// 路径分隔符。
        /// </summary>
        public static readonly char[] PathSeparatorChar = new char[] { '\\', '/' };

        static OwHelper()
        {
        }
        #region 属性

        #endregion 属性

        /// <summary>
        /// 复制字典。
        /// </summary>
        /// <typeparam name="Tkey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <param name="predicate">过滤器，返回false则不会复制，省略或者为null，则不调用过滤器。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<Tkey, TValue>(IReadOnlyDictionary<Tkey, TValue> src, IDictionary<Tkey, TValue> dest, Func<Tkey, bool> predicate = null)
        {
            if (predicate is null)
                foreach (var item in src)
                    dest[item.Key] = item.Value;
            else
                foreach (var item in src)
                {
                    if (!predicate(item.Key))
                        continue;
                    dest[item.Key] = item.Value;
                }
        }

        /// <summary>
        /// 四舍五入取整。
        /// </summary>
        /// <param name="result"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundWithAwayFromZero(decimal result) => (int)Math.Round(result, MidpointRounding.AwayFromZero);

        /// <summary>
        /// 遍历一个树结构的所有子项。深度优先算法遍历子树。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="roots">多个根的节点集合。</param>
        /// <param name="getChildren">从每个节点获取其所有子节点的委托。</param>
        /// <returns>一个可枚举集合，包含所有根下的所有节点。</returns>
        public static IEnumerable<T> GetAllSubItemsOfTree<T>(IEnumerable<T> roots, Func<T, IEnumerable<T>> getChildren)
        {
            Stack<T> gameItems = new Stack<T>(roots);
            while (gameItems.TryPop(out T result))
            {
                foreach (var item in getChildren(result))
                    gameItems.Push(item);
                yield return result;
            }
        }

        /// <summary>
        /// 遍历一个树结构的所有子项。广度优先算法遍历子树。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="getChildren">从每个节点获取其所有子节点的委托。</param>
        /// <param name="roots">多个根的节点对象。</param>
        /// <returns>一个可枚举集合，包含所有根下的所有节点(不含根节点)。</returns>
        public static IEnumerable<T> GetAllSubItemsOfTreeWithBfs<T>(Func<T, IEnumerable<T>> getChildren, params T[] roots)
        {
            Queue<T> gameItems = new Queue<T>(roots);
            while (gameItems.TryDequeue(out T result))
            {
                foreach (var item in getChildren(result))
                {
                    gameItems.Enqueue(item);
                    yield return item;
                }
            }
        }

        /// <summary>
        /// 分拣左右两个序列中的元素到三个集合中，三个集合的条件如下：仅左侧序列拥有的元素，两个序列都有的元素，仅右侧序列拥有的元素。
        /// 如果序列中有重复元素则分别计数。结果集合中元素顺序不稳定。
        /// </summary>
        /// <typeparam name="TLeft"></typeparam>
        /// <typeparam name="TRight"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="right"></param>
        /// <param name="getLeftKey"></param>
        /// <param name="getRightKey"></param>
        /// <param name="leftOnly">追加仅包含在左侧序列元素的集合，可以是null,则忽略。</param>
        /// <param name="boths">追加两个序列都包含的元素集合，可以是null,则忽略。</param>
        /// <param name="rightOnly">追加仅包含在右侧序列元素的集合，可以是null,则忽略。</param>
        public static void ApartWithWithRepeated<TLeft, TRight, TKey>(this IEnumerable<TLeft> source, IEnumerable<TRight> right, Func<TLeft, TKey> getLeftKey, Func<TRight, TKey> getRightKey,
            ICollection<TLeft> leftOnly, ICollection<(TLeft, TRight)> boths, ICollection<TRight> rightOnly)
        {
            bool b = source.Count() > right.Count();
            var leftDic = (from tmp in source
                           group tmp by getLeftKey(tmp) into g
                           select (g.Key, g.ToList())).ToDictionary(c => c.Key, c => c.Item2);
            foreach (var item in right)
            {
                var key = getRightKey(item);    //右序列元素的键
                if (leftDic.TryGetValue(key, out List<TLeft> leftLst))  //若两者皆有
                {
                    var tmp = leftLst[^1];
                    leftLst.RemoveAt(leftLst.Count - 1);
                    boths?.Add((tmp, item));
                    if (leftLst.Count <= 0)
                        leftDic.Remove(key);
                }
                else //仅右侧序列有
                {
                    rightOnly?.Add(item);
                }
            }
            //追加左侧序列独有元素
            if (null != leftOnly)
                foreach (var item in leftDic.SelectMany(c => c.Value))
                    leftOnly.Add(item);
        }

        /// <summary>
        /// 在一组相对概率中选择一个元素。
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="getProb">所有元素要是非负数。序列不可为空，不可全为0(此时行为未知)</param>
        /// <param name="rnd">随机数，要在区间[0,1)中。</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static TSource RandomSelect<TSource>(IEnumerable<TSource> source, Func<TSource, decimal> getProb, double rnd)
        {
            if (rnd < 0 || rnd >= 1)
                throw new ArgumentOutOfRangeException(nameof(rnd), "要在区间[0,1)中");
            decimal tmp = 0;
            bool hasNoneZero = false;
            var innerSeq = source.OrderByDescending(c => getProb(c)).Select(c =>
            {
                var tmpProb = getProb(c);
                if (tmpProb < 0)
                    throw new ArgumentOutOfRangeException(nameof(source), "所有元素要是非负数。");
                else if (tmpProb > 0)
                    hasNoneZero = true;
                tmp += tmpProb;
                return (Prob: tmp, Data: c);
            }).ToArray();
            if (!hasNoneZero)
                throw new ArgumentException("序列所有相对概率数都是0。", nameof(source));
            var seed = (decimal)rnd * innerSeq[^1].Prob;

            var (Prob, Data) = innerSeq.First(c => c.Prob >= seed);
            return Data;
        }

        /// <summary>
        /// 用<see cref="DateTime.UtcNow"/>计算超时剩余时间。
        /// </summary>
        /// <param name="start">起始时间点。使用UTC时间。</param>
        /// <param name="timeout">超时值，可以是<see cref="Timeout.InfiniteTimeSpan"/></param>
        /// <returns><see cref="TimeSpan.Zero"/>表示超时，否则是剩余的时间。
        /// 如果<paramref name="timeout"/>是<see cref="Timeout.InfiniteTimeSpan"/>，则立即返回<see cref="Timeout.InfiniteTimeSpan"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpan ComputeTimeout(DateTime start, TimeSpan timeout)
        {
            if (Timeout.InfiniteTimeSpan == timeout)
                return Timeout.InfiniteTimeSpan;
            var ts = start + timeout - DateTime.UtcNow;
            return ts <= TimeSpan.Zero ? TimeSpan.Zero : ts;
        }

        /// <summary>
        /// 计算剩余时间间隔，若<paramref name="end"/>在<paramref name="start"/>之前则返回<see cref="TimeSpan.Zero"/>。
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpan ComputeTimeout(DateTime start, DateTime end) =>
            start >= end ? TimeSpan.Zero : end - start;

        /// <summary>
        /// 按顺序锁定一组对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">已经排序的枚举接口，按枚举顺序逐个锁定元素（避免乱序死锁）。</param>
        /// <param name="locker">可以引发异常，视同失败。此时必须未锁定。</param>
        /// <param name="unlocker">解锁函数。<inheritdoc/></param>
        /// <param name="timeout"><see cref="TimeSpan.Zero"/>表示用参数尝试锁定各个对象只要有一个失败就立即返回。
        /// <see cref="Timeout.InfiniteTimeSpan"/>表示永不超时，通常这不是一个好的工程代码。</param>
        /// <returns>解锁接口。</returns>
        public static IDisposable LockWithOrder<T>([NotNull] IOrderedEnumerable<T> source, [NotNull] Func<T, TimeSpan, bool> locker, [NotNull] Action<T> unlocker, TimeSpan timeout)
        {
            DateTime start = DateTime.UtcNow;
            Stack<IDisposable> stack = new Stack<IDisposable>();  //辅助堆栈，用于回滚
            bool succ = true;   //成功标志
            try
            {
                foreach (var item in source)
                {
                    var ts = ComputeTimeout(start, timeout);
                    if (locker(item, ts)) //若成功
                        stack.Push(DisposerWrapper.Create(unlocker, item));
                    else //若失败
                    {
                        succ = false;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                succ = false;
            }
            if (succ)    //若成功
            {
                return DisposerWrapper.Create(stack);
            }
            else
            {
                using var tmp = DisposerWrapper.Create(stack);  //解锁已经锁定对象。
                return null;
            }
        }

        /// <summary>
        /// 按顺序锁定一组对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">使用默认排序方法对元素进行去重和排序。</param>
        /// <param name="locker">可以引发异常，视同失败。此时必须未锁定。没有锁定成功应返回null。</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static IDisposable LockWithOrder<T>([NotNull] IOrderedEnumerable<T> source, [NotNull] Func<T, TimeSpan, IDisposable> locker, TimeSpan timeout)
        {
            DateTime start = DateTime.UtcNow;
            Stack<IDisposable> stack = new Stack<IDisposable>();  //辅助堆栈，用于回滚
            bool succ = true;   //成功标志
            try
            {
                foreach (var item in source)
                {
                    var ts = ComputeTimeout(start, timeout);
                    var tmp = locker(item, ts);
                    if (null != tmp) //若成功
                        stack.Push(tmp);
                    else //若失败
                    {
                        succ = false;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                succ = false;
            }
            if (succ)    //若成功
            {
                return DisposerWrapper.Create(stack);
            }
            else
            {
                using var tmp = DisposerWrapper.Create(stack);  //解锁已经锁定对象。
                return null;
            }

        }

    }

}