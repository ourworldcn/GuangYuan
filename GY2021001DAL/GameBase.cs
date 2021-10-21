using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using OW.Game.Expression;
using OW.Game.Store;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 存储设置的模型类。
    /// </summary>
    public class GameSetting
    {
        [Key]
        public string Name { get; set; }

        public string Val { get; set; }
    }

    public class GameThingPropertyHelper : GamePropertyHelper
    {
        /// <summary>
        /// 获取对象的属性、
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object GetValue(object obj, string propertyName, object defaultValue = default)
        {
            var _ = obj as GameItemBase;
            var dic = _?.Properties;
            return dic == null ? defaultValue : dic.GetValueOrDefault(propertyName, defaultValue);
        }

        /// <summary>
        /// 设置对象的属性。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool SetValue(object obj, string propertyName, object val)
        {
            var _ = obj as GameItemBase;
            var dic = _?.Properties;
            dic[propertyName] = val;
            return true;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class FastChangingPropertyExtensions
    {
        public const string DefaultClassPrefix = "fcp";

        private static string GetDefaultKeyName(string propertyName, string name, string classPrefix = DefaultClassPrefix)
        {
            return propertyName switch
            {
                nameof(FastChangingProperty.MaxValue) => $"{classPrefix}m{name}",
                nameof(FastChangingProperty.Increment) => $"{classPrefix}i{name}",
                nameof(FastChangingProperty.Delay) => $"{classPrefix}d{name}",
                nameof(FastChangingProperty.LastValue) => $"{classPrefix}c{name}",
                nameof(FastChangingProperty.LastDateTime) => $"{classPrefix}t{name}",
                _ => string.Empty,
            };
        }

        /// <summary>
        /// 按指定的主名称和类前缀名称返回所有键的名称。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="classPrefix"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<string> GetKeyNames(string name, string classPrefix = DefaultClassPrefix)
        {
            return new string[]{
                $"{classPrefix}i{name}",
                $"{classPrefix}d{name}",
                $"{classPrefix}m{name}",
                $"{classPrefix}c{name}",
                $"{classPrefix}t{name}",};
        }

        /// <summary>
        /// 将当前值写入字典，不会自己计算更新属性。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="dic"></param>
        /// <param name="name">主名称</param>
        /// <param name="classPrefix"></param>
        public static void ToDictionary(this FastChangingProperty obj, IDictionary<string, object> dic, string name, string classPrefix = DefaultClassPrefix)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(name));
            dic[$"{classPrefix}i{name}"] = obj.Increment;
            dic[$"{classPrefix}d{name}"] = obj.Delay.TotalSeconds;
            dic[$"{classPrefix}m{name}"] = obj.MaxValue;
            dic[$"{classPrefix}c{name}"] = obj.LastValue;
            dic[$"{classPrefix}t{name}"] = obj.LastDateTime.ToString("s");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="thing"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToGameThing(this FastChangingProperty obj, GameItemBase thing)
        {
            obj.ToDictionary(thing.Properties, obj.Tag as string);
        }

        /// <summary>
        /// 从属性集合生成渐变属性对象。
        /// </summary>
        /// <param name="dic">至少要有fcpiXXX,fcpdXXX,fcpmXXX三个属性才能生成。</param>
        /// <param name="name">主名称，XXX,不带fcpi等前缀。</param>
        /// <returns>渐变属性对象，如果没有足够属性生成则返回null。</returns>
        public static FastChangingProperty FromDictionary(IReadOnlyDictionary<string, object> dic, string name, string classPrefix = DefaultClassPrefix)
        {
            Debug.Assert(!name.StartsWith(classPrefix), $"主名称不能以{classPrefix}开头。");
            if (!dic.TryGetValue($"{classPrefix}i{name}", out var piObj) || !OwHelper.TryGetDecimal(piObj, out var pi)) return null;
            if (!dic.TryGetValue($"{classPrefix}d{name}", out var pdObj) || !OwHelper.TryGetDecimal(pdObj, out var pd)) return null;
            if (!dic.TryGetValue($"{classPrefix}m{name}", out var pmObj) || !OwHelper.TryGetDecimal(pmObj, out var pm)) return null;

            OwHelper.TryGetDecimal(dic.GetValueOrDefault($"{classPrefix}c{name}", 0m), out var pc);
            if (!dic.TryGetValue($"{classPrefix}t{name}", out var tmpl) || !(tmpl is string strl) || !DateTime.TryParse(strl, out var pt))
                pt = DateTime.UtcNow;
            return new FastChangingProperty(TimeSpan.FromSeconds((double)pd), pi, pm, pc, pt) { Tag = name };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="classPrefix"></param>
        /// <returns></returns>
        public static IEnumerable<FastChangingProperty> FromDictionary(IReadOnlyDictionary<string, object> dic, string classPrefix = DefaultClassPrefix)
        {
            var startIndex = classPrefix.Length + 1;
            var names = dic.Keys.Where(c => c.StartsWith(classPrefix)).Select(c => c[startIndex..]).Distinct();
            var coll = names.Select(c => FromDictionary(dic, c, classPrefix)).OfType<FastChangingProperty>();
            return coll;

        }

        /// <summary>
        /// 从属性列表中清楚渐变属性涉及到的属性。
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="name"></param>
        public static void Clear(IDictionary<string, object> dic, string name, string classPrefix = DefaultClassPrefix)
        {
            dic.Remove($"{classPrefix}i{name}");
            dic.Remove($"{classPrefix}d{name}");
            dic.Remove($"{classPrefix}m{name}");
            dic.Remove($"{classPrefix}c{name}");
            dic.Remove($"{classPrefix}t{name}");
        }

    }

    /// <summary>
    /// <see cref="GameThingBase"/>的扩展方法封装类。
    /// </summary>
    public static class GameThingBaseExtensions
    {
        /// <summary>
        /// 获取指定属性的数值形式。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="result"></param>
        /// <returns>true指定属性存在且能转换为数值形式；否则返回false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetDecimalPropertyValue(this GameItemBase obj, string propertyName, out decimal result)
        {
            if (obj.TryGetPropertyValue(propertyName, out var tmp) && OwHelper.TryGetDecimal(tmp, out result))
                return true;
            result = default;
            return false;
        }


        /// <summary>
        /// 获取指定的属性值并转换为<see cref="decimal"/>,如果找不到，或不能转换则返回指定默认值。
        /// </summary>
        /// <param name="propertyName" >
        /// </param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetDecimalOrDefault(this GameItemBase obj, string propertyName, decimal defaultVal = decimal.Zero) =>
            obj.TryGetPropertyValue(propertyName, out var stcObj) && OwHelper.TryGetDecimal(stcObj, out var dec) ? dec : defaultVal;

        /// <summary>
        /// 获取堆叠上限。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns><see cref="decimal.MaxValue"/>如果不可堆叠则为1.无限制是<see cref="decimal.MaxValue"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetStc(this GameItemBase obj)
        {
            var stc = obj.GetDecimalOrDefault("stc", 1);
            return stc == -1 ? decimal.MaxValue : stc;
        }

        /// <summary>
        /// 是否可堆叠。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="result">如果是可堆叠对象则返回堆叠最大数量。-1是不受限制。</param>
        /// <returns>true可堆叠，false不可堆叠。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsStc(this GameItemBase obj, out decimal result) =>
            obj.TryGetDecimalPropertyValue("stc", out result);

    }

    /// <summary>
    /// <see cref="GameThingBase"/>用到的服务。
    /// </summary>
    public interface IGameThingHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public GameItemTemplate GetTemplateFromeId(Guid id);

    }

    /// <summary>
    /// 游戏世界内能独立存在的事物的对象的基类。
    /// </summary>
    public abstract class GameThingBase : GameObjectBase, IBeforeSave, IDisposable
    {
        protected GameThingBase()
        {
        }

        protected GameThingBase(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 客户端要记录的一些属性，这个属性客户端可以随意更改，服务器不使用。
        /// </summary>
        public string ClientGutsString { get; set; }

        /// <summary>
        /// 创建该对象的通用协调时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TemplateId { get; set; }

        [NotMapped]
        public abstract DbContext DbContext { get; }

        #region 快速变化属性相关

        private Dictionary<string, FastChangingProperty> _Name2FastChangingProperty;

        /// <summary>
        /// 快速变化属性。
        /// </summary>
        [NotMapped]
        public Dictionary<string, FastChangingProperty> Name2FastChangingProperty
        {
            get
            {
                if (_Name2FastChangingProperty is null)
                {
                    lock (this)
                        if (_Name2FastChangingProperty is null)
                        {
                            var list = FastChangingPropertyExtensions.FromDictionary(Properties);
                            _Name2FastChangingProperty = list.ToDictionary(c => c.Tag as string);
                        }
                }
                return _Name2FastChangingProperty;
            }
        }

        /// <summary>
        /// 移除一个渐变属性。
        /// </summary>
        /// <param name="name"></param>
        /// <returns>移除的渐变属性对象，如果没有找到指定名称的渐变属性对象则返回null。</returns>
        public FastChangingProperty RemoveFastChangingProperty(string name)
        {
            if (Name2FastChangingProperty.Remove(name, out var result))
                FastChangingPropertyExtensions.Clear(Properties, name);
            return result;
        }

        /// <summary>
        /// 刷新所有渐变属性，写入<see cref="SimpleExtendPropertyBase.Properties"/>
        /// </summary>
        public void FcpToProperties()
        {
            var now = DateTime.UtcNow;
            foreach (var item in Name2FastChangingProperty) //刷新渐变属性
            {
                var tmp = now;
                _ = item.Value.GetCurrentValue(ref tmp);
                item.Value.ToDictionary(Properties, item.Key);
            }
        }

        #endregion 快速变化属性相关


        #region 通用扩展属性及相关
        private ConcurrentDictionary<string, ExtendPropertyDescriptor> _ExtendPropertyDictionary;

        /// <summary>
        /// 扩展属性的封装字典。
        /// </summary>
        [NotMapped]
        public ConcurrentDictionary<string, ExtendPropertyDescriptor> ExtendPropertyDictionary
        {
            get
            {
                if (_ExtendPropertyDictionary is null)
                {
                    _ExtendPropertyDictionary = new ConcurrentDictionary<string, ExtendPropertyDescriptor>();
                    foreach (var item in ExtendProperties)
                    {
                        if (ExtendPropertyDescriptor.TryParse(item, out var tmp))
                            ExtendPropertyDictionary[tmp.Name] = tmp;
                    }
                }
                return _ExtendPropertyDictionary;
            }
        }

        /// <summary>
        /// 模板对象。
        /// </summary>
        [NotMapped]
        public GameThingTemplateBase Template { get; set; }

        private ObservableCollection<GameExtendProperty> _ExtendProperties;

        /// <summary>
        /// 通用扩展属性。
        /// </summary>
        [NotMapped]
        public ObservableCollection<GameExtendProperty> ExtendProperties
        {
            get
            {
                if (_ExtendProperties is null)
                {
                    var coll = DbContext.Set<GameExtendProperty>().Where(c => c.Id == Id);
                    _ExtendProperties = new ObservableCollection<GameExtendProperty>(coll);
                    _ExtendProperties.CollectionChanged += GameExtendPropertiesCollectionChanged;
                }
                return _ExtendProperties;
            }
        }

        private void GameExtendPropertiesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems.OfType<GameExtendProperty>())
                    {
                        item.Id = Id;
                    }
                    DbContext.Set<GameExtendProperty>().AddRange(e.NewItems.OfType<GameExtendProperty>());
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    DbContext.Set<GameExtendProperty>().RemoveRange(e.OldItems.OfType<GameExtendProperty>());
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    throw new NotSupportedException();
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    DbContext.Set<GameExtendProperty>().RemoveRange(e.OldItems.OfType<GameExtendProperty>());
                    DbContext.Set<GameExtendProperty>().AddRange(e.NewItems.OfType<GameExtendProperty>());
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="db"></param>
        public override void PrepareSaving(DbContext db)
        {
            if (null != _ExtendPropertyDictionary) //若需要写入
            {
                ExtendPropertyDescriptor.Fill(_ExtendPropertyDictionary.Values, ExtendProperties);
                //TO DO
                //var removeNames = new HashSet<string>(ExtendProperties.Select(c => c.Tag).Except(
                //    _ExtendPropertyDictionary.Where(c => c.Value.IsPersistence).Select(c => c.Key)));    //需要删除的对象名称
                //var removeItems = ExtendProperties.Where(c => removeNames.Contains(c.Tag)).ToArray();
                //foreach (var item in removeItems)
                //    ExtendProperties.Remove(item);
            }
            if (null != _Name2FastChangingProperty)   //若需要写入快速变化属性
                foreach (var item in _Name2FastChangingProperty)
                {
                    item.Value.ToDictionary(Properties, item.Key);
                }

            base.PrepareSaving(db);
        }

        #endregion 通用扩展属性及相关

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _ExtendPropertyDictionary = null;
                _ExtendProperties = null;
                Template = null;
                _Name2FastChangingProperty = null;
                base.Dispose(disposing);
            }
        }

    }
}
