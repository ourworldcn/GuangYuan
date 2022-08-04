using GuangYuan.GY001.UserDb;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OW.Game.PropertyChange
{
    /// <summary>
    /// 封装属性变化数据的一些补充方法。
    /// </summary>
    public static class GamePropertyChangeItemExtensions
    {
        /// <summary>
        /// 设置一个新值。并追加变化数据（如果需要）。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="propertyName"></param>
        /// <param name="value">新值，即使是null也认为是有效新值。</param>
        /// <param name="changes"></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void SetPropertyAndMarkChanged(this GameThingBase thing, string propertyName, object value, [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            if (changes is null)
                thing.Properties[propertyName] = value;
            else
            {
                var data = GamePropertyChangeItemPool<object>.Shared.Get();
                if (thing.Properties.TryGetValue(propertyName, out var oldValue))  //若存在旧值
                {
                    data.HasOldValue = true;
                    data.OldValue = oldValue;
                }
                thing.Properties[propertyName] = value;
                data.HasNewValue = true;
                data.NewValue = value;
                data.Object = thing;
                data.PropertyName = propertyName;
                changes.Add(data);
            }
        }

        /// <summary>
        /// 增加属性变化项，以反映指定的一组物品刚刚增加到了容器中。函数不会校验对象的结构，仅按参数构建变化数据。
        /// </summary>
        /// <param name="container"></param>
        /// <param name="gItems"></param>
        /// <param name="changes"></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void MarkAddChildren(this ICollection<GamePropertyChangeItem<object>> changes, GameThingBase container, IEnumerable<GameItem> gItems)
        {
            var pName = container switch
            {
                _ when container is GameChar => nameof(GameChar.GameItems),
                _ when container is GameItem => nameof(GameItem.Children),
                _ => throw new ArgumentException("无法处理的参数类型。", nameof(container)),
            };
            foreach (var c in gItems)
            {
                var result = GamePropertyChangeItemPool<object>.Shared.Get();
                result.Object = container;
                result.PropertyName = pName;
                result.HasNewValue = true;
                result.NewValue = c;
                changes?.Add(result);
            }
        }

        /// <summary>
        /// 增加属性变化项，以反映指定的一组物品刚刚增加到了容器中。
        /// </summary>
        /// <param name="container"></param>
        /// <param name="gItems"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarkAddChildren(this ICollection<GamePropertyChangeItem<object>> changes, GameThingBase container, params GameItem[] gItems)
        {
            changes.MarkAddChildren(container, gItems.AsEnumerable());
        }

        /// <summary>
        /// 构造一组变化数据，描述指定的一组物品被移出容器。函数不会校验对象的结构，仅按参数构建变化数据。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="gItems">将被移出父容器的一组物品。调用时尚未移除。</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void MarkRemoveChildren([NotNull] this ICollection<GamePropertyChangeItem<object>> obj, GameThingBase container, IEnumerable<GameItem> gItems)
        {
            var pName = container switch
            {
                _ when container is GameChar => nameof(GameChar.GameItems),
                _ when container is GameItem => nameof(GameItem.Children),
                _ => throw new ArgumentException("无法处理的参数类型。", nameof(container)),
            };
            foreach (var item in gItems)
            {
                var change = GamePropertyChangeItemPool<object>.Shared.Get();
                change.HasOldValue = true;
                change.OldValue = item;
                change.Object = container;
                change.PropertyName = pName;
                obj.Add(change);
            }
        }

        /// <summary>
        /// 构造一组变化数据，描述指定的一组物品被移出容器。函数不会校验对象的结构，仅按参数构建变化数据。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="container"></param>
        /// <param name="gItems"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarkRemoveChildren([NotNull] this ICollection<GamePropertyChangeItem<object>> obj, GameThingBase container, params GameItem[] gItems)
        {
            obj.MarkRemoveChildren(container, gItems.AsEnumerable());
        }

        /// <summary>
        /// 测试变化数据是否是一个集合变化。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCollectionChanged(this GamePropertyChangeItem<object> obj)
        {
            return obj.Object is GameItem && obj.PropertyName == nameof(GameItem.Children) || obj.Object is GameChar && obj.PropertyName == nameof(GameChar.GameItems);
        }

        /// <summary>
        /// 测试变化数据是否代表集合添加了数据。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCollectionAdded(this GamePropertyChangeItem<object> obj) =>
            obj.IsCollectionChanged() && obj.HasNewValue && !obj.HasOldValue;

        /// <summary>
        /// 测试变化数据是否代表集合移除了元素的数据。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCollectionRemoved(this GamePropertyChangeItem<object> obj) =>
            obj.IsCollectionChanged() && obj.HasOldValue && !obj.HasNewValue;

        /// <summary>
        /// 测试该变化数据是否是描述数量增加的变化。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAdd(this GamePropertyChangeItem<object> obj)
        {
            if (obj.HasOldValue && OwConvert.TryToDecimal(obj.OldValue, out var oVal) && obj.HasNewValue && OwConvert.TryToDecimal(obj.NewValue, out var nVal))
                return oVal < nVal;
            return false;
        }

        /// <summary>
        /// 将<see cref="GamePropertyChangedItem{object}"/>表示的变化数据转变为<see cref="ChangeItem"/>表示形式。
        /// </summary>
        /// <remarks>这会丢失一些精确的信息。</remarks>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public static void CopyTo(this IEnumerable<GamePropertyChangeItem<object>> src, ICollection<ChangeItem> dest)
        {
            foreach (var item in src.Where(c => !c.IsCollectionChanged() && ((c.Object as GameItem)?.GetContainerId().HasValue ?? false) && c.Object is GameItem).GroupBy(c => (GameItem)c.Object)) //复制非集合属性变化的数据
            {
                dest.AddToChanges(item.Key);
            }
            foreach (var item in src.Where(c => c.IsCollectionAdded() && c.Object is GameThingBase))   //复制增加的集合元素数据
            {
                dest.AddToAdds(((GameThingBase)item.Object).Id, (GameItem)item.NewValue);
            }
            foreach (var item in src.Where(c => c.IsCollectionRemoved() && c.Object is GameThingBase))    //复制删除集合元素的数据
            {
                dest.AddToRemoves(((GameThingBase)item.Object).Id, ((GameItem)item.OldValue).Id);
            }
        }
    }

}
