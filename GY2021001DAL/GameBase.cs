using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            if (!dic.TryGetValue($"{classPrefix}i{name}", out var piObj) || !OwConvert.TryToDecimal(piObj, out var pi)) return null;
            if (!dic.TryGetValue($"{classPrefix}d{name}", out var pdObj) || !OwConvert.TryToDecimal(pdObj, out var pd)) return null;
            if (!dic.TryGetValue($"{classPrefix}m{name}", out var pmObj) || !OwConvert.TryToDecimal(pmObj, out var pm)) return null;

            OwConvert.TryToDecimal(dic.GetValueOrDefault($"{classPrefix}c{name}", 0m), out var pc);
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
            if (obj.TryGetPropertyValue(propertyName, out var tmp) && OwConvert.TryToDecimal(tmp, out result))
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
            obj.TryGetPropertyValue(propertyName, out var stcObj) && OwConvert.TryToDecimal(stcObj, out var dec) ? dec : defaultVal;

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
        /// 模板Id。
        /// </summary>
        public Guid TemplateId { get; set; }

        [NotMapped]
        [JsonIgnore]
        public abstract DbContext DbContext { get; }

        /// <summary>
        /// 记录一些额外的信息，通常这些信息用于排序，加速查找符合特定要求的对象。此字段被索引。
        /// </summary>
        [MaxLength(64)]
        public string ExPropertyString { get; set; }

        /// <summary>
        /// 用于排序搜索使用的字段。
        /// </summary>
        public decimal? OrderbyDecimal { get; set; }

        #region 扩展对象相关

        /// <summary>
        /// 二进制数据。
        /// </summary>
        public byte[] BinaryArray { get; set; }

        /// <summary>
        /// 指定<see cref="BinaryArray"/>中存储的对象类型。
        /// 成功调用<see cref="GetBinaryObjectOrDefault{T}(T)"/>可以自动设置该字段。
        /// 未能设置该值则对象不会被自动序列化到数据库中。
        /// </summary>
        Type _BinaryObjectType;

        /// <summary>
        /// 暂存<see cref="BinaryArray"/>内存储的对象表示形式。
        /// </summary>
        object _BinaryObject;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="creator">null或省略则调用默认构造函数创建新对象。</param>
        /// <returns>返回的对象更改后在保存时可以自动保存最新值，但值类型无法自动保存，建议这里仅保存引用型可以Json序列化的对象。</returns>
        public T GetBinaryObject<T>(Func<T> creator = null)
        {
            if (_BinaryObjectType is null)   //若尚未初始化
            {
                if (BinaryArray is null || BinaryArray.Length <= 0)
                    _BinaryObject = creator is null ? TypeDescriptor.CreateInstance(null, typeof(T), null, null) : creator();
                else
                    _BinaryObject = JsonSerializer.Deserialize<T>(BinaryArray);
                _BinaryObjectType = typeof(T);
            }
            return (T)_BinaryObject;    //若试图更改为不可转化的类型 则抛出异常
        }

        #endregion 扩展对象相关

        #region 快速变化属性相关

        private Dictionary<string, FastChangingProperty> _Name2FastChangingProperty;

        /// <summary>
        /// 快速变化属性。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
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

        /// <summary>
        /// 获取指定名称的属性值，如果快变属性存在则返回快变属性的当前值，如果在两处都没有没有找到该名称的属性或无法转化为数值，则返回指定的默认值。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal GetPropertyWithFcpOrDefalut(string name, decimal defaultValue = default)
        {
            return Name2FastChangingProperty.TryGetValue(name, out var fcp) ? fcp.GetCurrentValueWithUtc() : Properties.GetDecimalOrDefault(name, defaultValue);
        }
        #endregion 快速变化属性相关

        #region 通用扩展属性及相关
        private ConcurrentDictionary<string, ExtendPropertyDescriptor> _ExtendPropertyDictionary;

        /// <summary>
        /// 扩展属性的封装字典。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
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

        private ObservableCollection<GameExtendProperty> _ExtendProperties;

        bool _ExtendPropertiesInited;
        /// <summary>
        /// 通用扩展属性。
        /// </summary>
        [NotMapped]
        public ObservableCollection<GameExtendProperty> ExtendProperties
        {
            get
            {
                if (_ExtendPropertiesInited)    //为使用json反序列化需要，强制被设置null后，不能返回有效实例，否则报错
                    return _ExtendProperties;
                if (_ExtendProperties is null && DbContext != null)
                {
                    try
                    {
                        var coll = DbContext.Set<GameExtendProperty>().Where(c => c.Id == Id);
                        _ExtendProperties = new ObservableCollection<GameExtendProperty>(coll);
                        _ExtendProperties.CollectionChanged += GameExtendPropertiesCollectionChanged;
                    }
                    catch (Exception)
                    {

                    }
                }
                else if (DbContext == null)
                {
                    _ExtendProperties = new ObservableCollection<GameExtendProperty>();
                    _ExtendProperties.CollectionChanged += GameExtendPropertiesCollectionChanged;
                }
                return _ExtendProperties;
            }
            set
            {
                if (null != _ExtendProperties)
                    _ExtendProperties.CollectionChanged -= GameExtendPropertiesCollectionChanged;
                _ExtendProperties = value;
                if (null != _ExtendProperties)
                {
                    DbContext?.AddRange(value);
                    value.CollectionChanged += GameExtendPropertiesCollectionChanged;
                }
                _ExtendPropertiesInited = true;
            }
        }

        private void GameExtendPropertiesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems.OfType<GameExtendProperty>())
                    {
                        item.Id = Id;
                    }
                    DbContext.Set<GameExtendProperty>().AddRange(e.NewItems.OfType<GameExtendProperty>());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    DbContext.Set<GameExtendProperty>().RemoveRange(e.OldItems.OfType<GameExtendProperty>());
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotSupportedException();
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
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
            if (null != _BinaryObjectType) //若需持久化对象值
            {
                if (_BinaryObject is null)
                {
                    BinaryArray = null;
                }
                else
                {
                    BinaryArray = JsonSerializer.SerializeToUtf8Bytes(_BinaryObject, _BinaryObjectType);
                }
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
                _Name2FastChangingProperty = null;
                _BinaryObject = null;
                base.Dispose(disposing);
            }
        }

    }
}
