using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Social;
using Microsoft.EntityFrameworkCore;
using OW.Game.PropertyChange;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OW.Game
{
    public class PropertyManagerOptions
    {
        public PropertyManagerOptions()
        {

        }
    }

    /// <summary>
    /// 属性管理器。管理必要的基础属性，包括重命名等情况。
    /// </summary>
    public class GamePropertyManager : GameManagerBase<PropertyManagerOptions>, IGamePropertyManager
    {
        #region 构造函数相关

        public GamePropertyManager()
        {
            Initializer();
        }

        public GamePropertyManager(IServiceProvider service) : base(service)
        {
            Initializer();
        }

        public GamePropertyManager(IServiceProvider service, PropertyManagerOptions options) : base(service, options)
        {
            Initializer();
        }

        private void Initializer()
        {
            _Id2Datas = new Lazy<Dictionary<string, GamePropertyTemplate>>(() =>
            {
                using var db = World.CreateNewTemplateDbContext();
                return db.Set<GamePropertyTemplate>().AsNoTracking().ToDictionary(c => c.PName);
            }, LazyThreadSafetyMode.ExecutionAndPublication);
            _NoCopyNames = new Lazy<HashSet<string>>(() =>
            {
                return new HashSet<string>(Id2Datas.Values.Where(c => c.IsFix && !c.IsPrefix).Select(c => string.IsNullOrEmpty(c.FName) ? c.PName : c.FName));
            }, LazyThreadSafetyMode.ExecutionAndPublication);
            _NoCopyPrefixNames = new Lazy<HashSet<string>>(() =>
             {
                 return new HashSet<string>(Id2Datas.Values.Where(c => c.IsFix && c.IsPrefix).Select(c => string.IsNullOrEmpty(c.FName) ? c.PName : c.FName));
             }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        #endregion 构造函数相关

        private Lazy<Dictionary<string, GamePropertyTemplate>> _Id2Datas;

        public IReadOnlyDictionary<string, GamePropertyTemplate> Id2Datas => _Id2Datas.Value;

        #region 基础属性名相关

        private Lazy<HashSet<string>> _NoCopyNames;
        /// <summary>
        /// 不必复制的属性全名集合。
        /// </summary>
        public ISet<string> NoCopyNames => _NoCopyNames.Value;

        private Lazy<HashSet<string>> _NoCopyPrefixNames;
        /// <summary>
        /// 不必复制的属性属性名前缀集合。
        /// </summary>
        public ISet<string> NoCopyPrefixNames => _NoCopyPrefixNames.Value;


        string _LevelPropertyName;
        /// <summary>
        /// 级别属性名前缀。
        /// </summary>
        public string LevelPropertyName => _LevelPropertyName ??= _Id2Datas.Value["lv"].FName;

        string _StackUpperLimitPropertyName;
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string StackUpperLimitPropertyName => _StackUpperLimitPropertyName ??= (_Id2Datas.Value.GetValueOrDefault("stc")?.FName ?? "stc");

        string _CapacityPropertyName;
        /// <summary>
        /// 获取容量属性。默认值cap。
        /// </summary>
        public string CapacityPropertyName => _CapacityPropertyName ??= (_Id2Datas.Value.GetValueOrDefault("cap")?.FName ?? "cap");

        private string _CountPropertyName;
        /// <summary>
        /// 
        /// </summary>
        public string CountPropertyName
        {
            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            get => _CountPropertyName ??= (_Id2Datas.Value.GetValueOrDefault("count")?.FName ?? "Count");
        }

        #endregion 基础属性名相关

        #region 基础属性相关

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="pNmaes"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining | MethodImplOptions.Synchronized)]
        public IEnumerable<string> Filter(IEnumerable<string> pNmaes) =>
            pNmaes.Where(c => !NoCopyNames.Contains(c) && !NoCopyPrefixNames.Any(c1 => c.StartsWith(c1)));  //寻找匹配的设置项

        /// <summary>
        /// 对给定字典过滤掉不必要的属性名，返回有效的键值对。
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining | MethodImplOptions.Synchronized)]
        public IEnumerable<KeyValuePair<string, object>> Filter(IReadOnlyDictionary<string, object> dic)
        {
            return dic.Where(c => !NoCopyNames.Contains(c.Key) && !NoCopyPrefixNames.Any(c1 => c.Key.StartsWith(c1)));
        }

        /// <summary>
        /// 获取动态属性，如果没有在指定的对象上找到，则试图在其模板中寻找。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public virtual bool TryGetPropertyWithTemplate(GameThingBase thing, string key, out object result) =>
            DictionaryUtil.TryGetValue(key, out result, thing.Properties, World.ItemTemplateManager.GetTemplateFromeId(thing.ExtraGuid)?.Properties);

        /// <summary>
        /// 获取属性，如果没有则寻找模板内同名属性。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="propertyName"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public virtual bool TryGetDecimalWithTemplate(GameThingBase thing, string propertyName, out decimal result)
        {
            return DictionaryUtil.TryGetDecimal(propertyName, out result, thing.Properties, World.ItemTemplateManager.GetTemplateFromeId(thing.ExtraGuid)?.Properties);
        }

        /// <summary>
        /// 获取指定的属性值并转换为<see cref="decimal"/>,如果找不到，或不能转换则返回指定默认值。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="propertyName"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public virtual bool TryGetDecimalWithFcp(GameThingBase thing, string propertyName, out decimal result)
        {
            if (thing.Name2FastChangingProperty.TryGetValue(propertyName, out var fcp))
            {
                result = fcp.GetCurrentValueWithUtc();
                return true;
            }
            return TryGetDecimalWithTemplate(thing, propertyName, out result);
        }

        #region 设置属性

        /// <summary>
        /// 设置数字属性的增量值。如果是fcp属性则直接将增量加入<see cref="FastChangingProperty.LastValue"/>而不会再次计算。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="propertyName">指定键值不存在或不是数字属性，则视同为0而不会抛出异常。</param>
        /// <param name="diff">可以是负数，但调用者需要保证计算后的正确性，该函数不校验。</param>
        /// <returns></returns>
        public virtual bool AddPropertyWithFcp(GameThingBase thing, string propertyName, decimal diff)
        {
            if (thing.Name2FastChangingProperty.TryGetValue(propertyName, out var fcp))   //若存在快速渐变属性
            {
                fcp.LastValue += diff;
            }
            else
            {
                var oldValue = thing.Properties.GetDecimalOrDefault(propertyName, decimal.Zero);
                thing.Properties[propertyName] = oldValue + diff;
            }
            return true;
        }

        /// <summary>
        /// 设置动态属性。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <param name="oldValue"></param>
        /// <returns>true有旧属性，此时<paramref name="oldValue"/>中包含旧值。false没有原有属性。</returns>
        public virtual bool SetProperty(GameThingBase thing, string propertyName, object value, out object oldValue)
        {
            if (thing.Properties.TryGetValue(propertyName, out oldValue))
                return true;
            thing.Properties[propertyName] = value;
            return false;
        }

        #endregion 设置属性

        /// <summary>
        /// 获取是否可堆叠。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="result"><see cref="decimal.MaxValue"/>表示无限堆叠。非堆叠物品这个值被设置为1。</param>
        /// <returns>true可堆叠，false不可堆叠。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public virtual bool IsStc(GameThingBase thing, out decimal result)
        {
            if (!TryGetDecimalWithFcp(thing, StackUpperLimitPropertyName, out var stc))
            {
                result = 1;
                return false;
            }
            result = stc == -1 ? decimal.MaxValue : stc;
            return true;
        }

        /// <summary>
        /// 获取其数量。
        /// </summary>
        /// <param name="thing"></param>
        /// <returns>数量，未明确定义，则对可堆叠物返回0，不可堆叠物返回1。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public virtual decimal GetCount(GameThingBase thing)
        {
            return TryGetDecimalWithFcp(thing, CountPropertyName, out var count) ? count : (IsStc(thing, out _) ? decimal.Zero : decimal.One);
        }

        /// <summary>
        /// 获取最大堆叠数，不可堆叠的返回1，没有限制则返回<see cref="decimal.MaxValue"/>。
        /// </summary>
        /// <param name="thing"></param>
        /// <returns>不可堆叠的返回1，若没有限制（-1）则返回<see cref="decimal.MaxValue"/>。
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public virtual decimal GetStcOrOne(GameThingBase thing)
        {
            if (!TryGetDecimalWithFcp(thing, StackUpperLimitPropertyName, out var result))  //若没有指定堆叠属性
                return decimal.One;
            else
                return result == -1 ? decimal.MaxValue : result;
        }

        /// <summary>
        /// 获取指定物剩余的可堆叠量。
        /// </summary>
        /// <param name="thing"></param>
        /// <returns>不可堆叠或已满堆叠都会返回0，否则返回剩余的可堆叠数。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public virtual decimal GetRemainderStc(GameThingBase thing)
        {
            if (!IsStc(thing, out var result))  //若没有指定堆叠属性
                return decimal.Zero;
            else
                return result == -1 ? decimal.MaxValue : Math.Max(result - GetCount(thing), 0);
        }

        /// <summary>
        /// 获取作为容器的容量。如果没有指定则返回0，视同非容器。
        /// </summary>
        /// <param name="thing"></param>
        /// <returns>容器的容量,如果没有指定则返回0，视同非容器。不限制容量的-1，转换为<see cref="decimal.MaxValue"/>返回。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public virtual decimal GetCapOrZero(GameThingBase thing)
        {
            if (!TryGetDecimalWithFcp(thing, CapacityPropertyName, out var result))  //若没有指定容量属性
                return decimal.Zero;
            else
                return result == -1 ? decimal.MaxValue : result;
        }

        /// <summary>
        /// 获取孩子集合的接口。当前仅认识<see cref="GameItem"/>,<see cref="GameChar"/>,<see cref="GameGuild"/>派生类。
        /// </summary>
        /// <param name="thing"></param>
        /// <returns>不认识的类将导致返回null，而非抛出异常。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public virtual ICollection<GameItem> GetChildrenCollection(GameThingBase thing)
        {
            if (thing is GameItem gItem)
                return gItem.Children;
            else if (thing is GameChar gChar)
                return gChar.GameItems;
            else if (thing is GameGuild guild)
                return guild.Items;
            else
                return null;
        }

        /// <summary>
        /// 获取剩余容量。
        /// </summary>
        /// <param name="thing"></param>
        /// <returns>剩余的容量，不是容器或已满都返回0，无限容量返回<see cref="decimal.MaxValue"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public virtual decimal GetRemainderCap(GameThingBase thing)
        {
            if (!TryGetDecimalWithFcp(thing, CapacityPropertyName, out var result))  //若没有指定堆叠属性
                return decimal.Zero;
            else
                return result == -1 ? decimal.MaxValue : Math.Max(result - GetChildrenCollection(thing).Count, 0);
        }

        #endregion 基础属性相关
    }

    public static class GamePropertyManagerExtensions
    {
        /// <summary>
        /// 设置一个新值。并追加变化数据（如果需要）。
        /// </summary>
        /// <param name="manager">属性管理器。</param>
        /// <param name="thing"></param>
        /// <param name="propertyName"></param>
        /// <param name="value">新值，即使是null也认为是有效新值。</param>
        /// <param name="changes"></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void SetPropertyAndMarkChanged([NotNull] this GamePropertyManager manager, [NotNull] GameThingBase thing, [NotNull] string propertyName, object value,
            [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            if (changes is null)    //若无需标记变化数据
                thing.Properties[propertyName] = value;
            else //若需要标记变化数据
            {
                var data = GamePropertyChangeItemPool<object>.Shared.Get();
                if (thing.Properties.TryGetValue(propertyName, out var oldValue))  //若存在旧值
                {
                    data.HasOldValue = true;
                    data.OldValue = oldValue;
                }
                data.HasNewValue = true;
                data.NewValue = value;
                data.Object = thing;
                data.PropertyName = propertyName;
                changes.Add(data);
            }
        }


    }
}
