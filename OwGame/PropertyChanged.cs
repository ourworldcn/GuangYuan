using Microsoft.Extensions.ObjectPool;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OW.Game
{
    /// <summary>
    /// 属性变化的数据封装类。
    /// </summary>
    /// <typeparam name="T">变化的属性值类型，使用强类型可以避免对值类型拆装箱操作。</typeparam>
    public class GamePropertyChangedItem<T> : ICloneable
    {
        public GamePropertyChangedItem()
        {

        }

        public GamePropertyChangedItem(T obj, string name)
        {
            PropertyName = name;
            Object = obj;
        }

        /// <summary>
        /// 构造函数。
        /// 无论<paramref name="newValue"/>和<paramref name="oldValue"/>给定任何值，<see cref="HasOldValue"/>和<see cref="HasNewValue"/>都设置为true。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public GamePropertyChangedItem(T obj, string name, T oldValue, T newValue)
        {
            Object = obj;
            PropertyName = name;
            OldValue = oldValue;
            NewValue = newValue;
            HasOldValue = HasNewValue = true;
        }

        /// <summary>
        /// 指出是什么对象变化了属性。
        /// </summary>
        public object Object { get; set; }

        /// <summary>
        /// 属性的名字。事件发送者和处理者约定好即可，也可能是对象的其他属性名，如Children可以表示集合变化。
        /// </summary>
        public string PropertyName { get; set; }

        #region 旧值相关

        /// <summary>
        /// 指示<see cref="OldValue"/>中的值是否有意义。
        /// </summary>
        public bool HasOldValue { get; set; }

        /// <summary>
        /// 获取或设置旧值。
        /// </summary>
        public T OldValue { get; set; }

        /// <summary>
        /// 试图获取旧值。
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetOldValue([MaybeNullWhen(false)] out T result)
        {
            result = HasOldValue ? OldValue : default;
            return HasOldValue;
        }
        #endregion 旧值相关

        #region 新值相关

        /// <summary>
        /// 指示<see cref="NewValue"/>中的值是否有意义。
        /// </summary>
        public bool HasNewValue { get; set; }

        /// <summary>
        /// 新值。
        /// </summary>
        public T NewValue { get; set; }

        /// <summary>
        /// 试图获取新值。
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetNewValue([MaybeNullWhen(false)] out T result)
        {
            result = HasNewValue ? NewValue : default;
            return HasNewValue;
        }

        /// <summary>
        /// 获取一个浅表副本。返回对象从池中获取。
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var result = GamePropertyChangedItemPool<T>.Shared.Get();
            result.Object = Object;
            result.PropertyName = PropertyName;
            result.OldValue = OldValue;
            result.HasOldValue = HasOldValue;
            result.NewValue = NewValue;
            result.HasNewValue = HasNewValue;
            result.Tag = Tag;
            result.DateTimeUtc = DateTimeUtc;
            return result;
        }

        #endregion 新值相关

        /// <summary>
        /// 属性发生变化的时间点。Utc计时。
        /// </summary>
        public DateTime DateTimeUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 事件发起方可以在这里记录一些额外信息。
        /// </summary>
        public object Tag { get; set; }
    }

    /// <summary>
    /// 提供可重复使用 <see cref="GamePropertyChangedItem{T}"/> 类型实例的资源池。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GamePropertyChangedItemPool<T> : DefaultObjectPool<GamePropertyChangedItem<T>>
    {
        public static readonly ObjectPool<GamePropertyChangedItem<T>> Shared;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static GamePropertyChangedItemPool()
        {
            if (Shared is null)
                Interlocked.CompareExchange(ref Shared, new GamePropertyChangedItemPool<T>(new SimplePropertyChangedItemPooledObjectPolicy()), null);
        }

        public GamePropertyChangedItemPool(IPooledObjectPolicy<GamePropertyChangedItem<T>> policy) : base(policy)
        {

        }

        public GamePropertyChangedItemPool(IPooledObjectPolicy<GamePropertyChangedItem<T>> policy, int maximumRetained) : base(policy, maximumRetained)
        {
        }

        private class SimplePropertyChangedItemPooledObjectPolicy : DefaultPooledObjectPolicy<GamePropertyChangedItem<T>>
        {
            public override bool Return(GamePropertyChangedItem<T> obj)
            {
                obj.Object = default;
                obj.PropertyName = default;
                obj.HasOldValue = default;
                obj.OldValue = default;
                obj.HasNewValue = default;
                obj.NewValue = default;
                obj.DateTimeUtc = default;
                obj.Tag = default;
                return true;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public override GamePropertyChangedItem<T> Get()
        {
            var result = base.Get();
            result.DateTimeUtc = DateTime.UtcNow;
            return result;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class GamePropertyChangedItemExtensions
    {
        public static void PostDynamicPropertyChanged<T>(this ICollection<GamePropertyChangedItem<T>> collection, SimpleDynamicPropertyBase obj, string name, object newValue, object tag)
        {
            var arg = GamePropertyChangedItemPool<object>.Shared.Get();
            arg.Object = obj; arg.PropertyName = name; arg.Tag = tag;
            if (obj.Properties.TryGetValue(name, out var oldValue))
            {
                arg.OldValue = oldValue;
                arg.HasOldValue = true;
            }
            obj.Properties[name] = newValue;
            arg.NewValue = newValue;
            arg.HasNewValue = true;
        }

    }
}