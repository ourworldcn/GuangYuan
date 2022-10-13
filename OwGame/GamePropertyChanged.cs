using Microsoft.Extensions.ObjectPool;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;

namespace OW.Game.PropertyChange
{
    /// <summary>
    /// 属性变化的数据封装类。
    /// </summary>
    /// <typeparam name="T">变化的属性值类型，使用强类型可以避免对值类型拆装箱操作。</typeparam>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class GamePropertyChangeItem<T> : ICloneable
    {
        #region 静态函数

        /// <summary>
        /// 修改一个对象的属性，并正确填写变化数据。
        /// </summary>
        /// <param name="collection">可以是空引用，若null则忽略。</param>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <param name="newValue"></param>
        /// <param name="tag"></param>
        public static void ModifyAndAddChanged([AllowNull] ICollection<GamePropertyChangeItem<T>> collection, ISimpleDynamicProperty<T> obj, string name, T newValue, object tag = null)
        {
            Debug.Assert(name != "Children");
            if (collection is null)
            {
                obj.SetSdp(name, newValue);
            }
            else
            {
                var arg = Create(obj, name, newValue, tag);
                obj.SetSdp(name, newValue);
                collection.Add(arg);
            }
        }

        /// <summary>
        /// 创建并返回一个标识指定变换的对象。
        /// </summary>
        /// <param name="sdep"></param>
        /// <param name="name"></param>
        /// <param name="newValue"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GamePropertyChangeItem<T> Create(ISimpleDynamicProperty<T> sdep, string name, T newValue, object tag = null)
        {
            var result = GamePropertyChangeItemPool<T>.Shared.Get();
            result.Object = sdep; result.PropertyName = name; result.Tag = tag;
            if (sdep.TryGetSdp(name, out var oldValue) && oldValue is T old)
            {
                result.OldValue = old;
                result.HasOldValue = true;
            }
            result.NewValue = newValue;
            result.HasNewValue = true;
            return result;
        }

        #endregion 静态函数

        #region 构造函数及相关

        public GamePropertyChangeItem()
        {
            Initialize();
        }

        public GamePropertyChangeItem(T obj, string name)
        {
            PropertyName = name;
            Object = obj;
            Initialize();
        }

        /// <summary>
        /// 构造函数。
        /// 无论<paramref name="newValue"/>和<paramref name="oldValue"/>给定任何值，<see cref="HasOldValue"/>和<see cref="HasNewValue"/>都设置为true。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public GamePropertyChangeItem(T obj, string name, T oldValue, T newValue)
        {
            Object = obj;
            PropertyName = name;
            OldValue = oldValue;
            NewValue = newValue;
            HasOldValue = HasNewValue = true;
            Initialize();
        }

        void Initialize()
        {

        }

        #endregion 构造函数及相关

        /// <summary>
        /// 指出是什么对象变化了属性。
        /// </summary>
        [JsonIgnore]
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
            return MemberwiseClone();
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

        #region 调试相关

        /// <summary>
        /// 生成在调试器变量窗口中的显示的字符串。
        /// </summary>
        /// <returns></returns>
        private string GetDebuggerDisplay()
        {
            return $"{Object}.{PropertyName} : {{{OldValue}}} -> {{{NewValue}}}";
        }
        #endregion 调试相关
    }

    /// <summary>
    /// 提供可重复使用 <see cref="GamePropertyChangeItem{T}"/> 类型实例的资源池。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GamePropertyChangeItemPool<T> : DefaultObjectPool<GamePropertyChangeItem<T>>
    {
        public static readonly ObjectPool<GamePropertyChangeItem<T>> Shared;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static GamePropertyChangeItemPool()
        {
            if (Shared is null)
                Interlocked.CompareExchange(ref Shared, new GamePropertyChangeItemPool<T>(new SimplePropertyChangedItemPooledObjectPolicy()), null);
        }

        public GamePropertyChangeItemPool(IPooledObjectPolicy<GamePropertyChangeItem<T>> policy) : base(policy)
        {

        }

        public GamePropertyChangeItemPool(IPooledObjectPolicy<GamePropertyChangeItem<T>> policy, int maximumRetained) : base(policy, maximumRetained)
        {
        }

        private class SimplePropertyChangedItemPooledObjectPolicy : DefaultPooledObjectPolicy<GamePropertyChangeItem<T>>
        {
            public override bool Return(GamePropertyChangeItem<T> obj)
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
        public override GamePropertyChangeItem<T> Get()
        {
            var result = base.Get();
            result.DateTimeUtc = DateTime.UtcNow;
            return result;
        }

    }

    /// <summary>
    /// <see cref="GamePropertyChangeItem{T}"/>类的扩展方法封装类。
    /// </summary>
    public static class GamePropertyChangedItemExtensions
    {
        /// <summary>
        /// 修改一个对象的属性，并正确填写变化数据。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="obj"></param>
        /// <param name="name">只能针对简单属性，不可针对集合属性。调试状态下Children会报错。</param>
        /// <param name="newValue"></param>
        /// <param name="tag"></param>
        public static void ModifyAndAddChanged<T>(this ICollection<GamePropertyChangeItem<T>> collection, SimpleDynamicPropertyBase obj, string name, T newValue, object tag = null)
        {
            Debug.Assert(name != "Children");
            var arg = GamePropertyChangeItemPool<T>.Shared.Get();
            arg.Object = obj; arg.PropertyName = name; arg.Tag = tag;
            if (obj.TryGetSdp(name, out var oldValue) && oldValue is T old)
            {
                arg.OldValue = old;
                arg.HasOldValue = true;
            }
            switch (name)
            {
                case nameof(IDbQuickFind.ExtraDecimal):
                    if (obj is IDbQuickFind dbFinder && OwConvert.TryToDecimal(newValue, out var dec))
                        dbFinder.ExtraDecimal = dec;
                    else
                        obj.SetSdp(name, newValue);
                    break;
                case nameof(IDbQuickFind.ExtraString):
                    if (obj is IDbQuickFind dbFinder1)
                        dbFinder1.ExtraString = newValue.ToString();
                    else
                        obj.SetSdp(name, newValue);
                    break;
                default:
                    obj.SetSdp(name, newValue);
                    break;
            }
            arg.NewValue = newValue;
            arg.HasNewValue = true;
            collection.Add(arg);
        }

    }
}