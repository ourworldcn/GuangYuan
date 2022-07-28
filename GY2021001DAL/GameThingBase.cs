using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using OW.Game.PropertyChange;
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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 快速渐变属性。
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
        /// 将当前值写入字典，不会自己计算更新属性。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="dynamicPropertyBase"></param>
        /// <param name="name"></param>
        /// <param name="classPrefix"></param>
        /// <param name="changes"></param>
        public static void ToDictionary(this FastChangingProperty obj, SimpleDynamicPropertyBase dynamicPropertyBase, string name, string classPrefix = DefaultClassPrefix,
            ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(name));
            if (changes != null)
            {
                changes.ModifyAndAddChanged(dynamicPropertyBase, $"{classPrefix}i{name}", obj.Increment);
                changes.ModifyAndAddChanged(dynamicPropertyBase, $"{classPrefix}d{name}", obj.Delay.TotalSeconds);
                changes.ModifyAndAddChanged(dynamicPropertyBase, $"{classPrefix}m{name}", obj.MaxValue);
                changes.ModifyAndAddChanged(dynamicPropertyBase, $"{classPrefix}c{name}", obj.LastValue);
                changes.ModifyAndAddChanged(dynamicPropertyBase, $"{classPrefix}t{name}", obj.LastDateTime.ToString("s"));
            }
            else
                obj.ToDictionary(dynamicPropertyBase.Properties, name, classPrefix);
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
        #region 获取属性相关

        /// <summary>
        /// 获取模板对象。
        /// </summary>
        /// <param name="thing"></param>
        /// <returns>如果未设置可能返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameThingTemplateBase GetTemplate(this GameThingBase thing) =>
            thing.RuntimeProperties.GetValueOrDefault("Template") as GameThingTemplateBase;

        /// <summary>
        /// 设置模板对象。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="template">设置为null则删除字典中的键。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTemplate(this GameThingBase thing, GameThingTemplateBase template)
        {
            if (template is null)
                thing.RuntimeProperties.Remove("Template", out _);
            else
                thing.RuntimeProperties["Template"] = template;
        }

        /// <summary>
        /// 获取属性，如果没有则寻找模板内同名属性。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="propertyName"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetProperty(this GameThingBase thing, string propertyName, out object result)
        {
            if (thing.Properties.TryGetValue(propertyName, out result))
                return true;
            var tt = thing.GetTemplate();
            if (tt is null)
                return false;
            return tt.Properties.TryGetValue(propertyName, out result);
        }

        /// <summary>
        /// 获取指定属性并从模板(如果有)中寻找，如果都没找到则返回<paramref name="defaultValue"/>
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object GetPropertyOrDefault(this GameThingBase thing, string propertyName, object defaultValue = default) =>
            thing.TryGetProperty(propertyName, out var result) ? result : defaultValue;

        /// <summary>
        /// 获取指定名称的属性，且优先考虑快速渐变属性。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetPropertyWithFcp(this GameThingBase thing, string name, out decimal result)
        {
            if (thing.Name2FastChangingProperty.TryGetValue(name, out var fcp))
            {
                result = fcp.GetCurrentValueWithUtc();
                return true;
            }
            if (!thing.TryGetProperty(name, out var obj))
            {
                result = default;
                return false;
            }
            return OwConvert.TryToDecimal(obj, out result);
        }

        /// <summary>
        /// 获取指定的属性值并转换为<see cref="decimal"/>,如果找不到，或不能转换则返回指定默认值。
        /// </summary>
        /// <param name="propertyName" >
        /// </param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetDecimalWithFcpOrDefault(this GameThingBase obj, string propertyName, decimal defaultVal = decimal.Zero) =>
            obj.TryGetPropertyWithFcp(propertyName, out var result) ? result : defaultVal;


        #endregion 获取属性相关

        /// <summary>
        /// 获取堆叠上限。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns><see cref="decimal.MaxValue"/>如果不可堆叠则为1.无限制是<see cref="decimal.MaxValue"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetStc(this GameItemBase obj)
        {
            var stc = obj.GetDecimalWithFcpOrDefault("stc", 1);
            return stc == -1 ? decimal.MaxValue : stc;
        }

        /// <summary>
        /// 获取指定名称的属性值，如果快变属性存在则返回快变属性的当前值，如果在两处都没有没有找到该名称的属性或无法转化为数值，则返回指定的默认值。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetPropertyWithFcpOrDefalut(this GameThingBase thing, string name, decimal defaultValue = default)
        {
            return thing.TryGetPropertyWithFcp(name, out var result) ? result : defaultValue;
        }

        #region 设置属性
        /// <summary>
        /// 设置属性。如果有同名的gcp属性则首先读取(计算当前值)并设置其最后的值（但不更改最后计算时间）。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        //public static bool SetPropertyWithFcp(this GameThingBase thing,string propertyName,decimal value)
        //{
        //    if(thing.Name2FastChangingProperty.TryGetValue(propertyName,out var fcp))   //若有fcp属性
        //    {
        //       var oldValue= fcp.GetCurrentValueWithUtc();
        //        if(oldValue!=value)
        //        {
        //        }
        //    }
        //    else
        //    {

        //    }
        //}

        #endregion 设置属性
    }

    /// <summary>
    /// 游戏世界内能独立存在的事物的对象的基类。
    /// </summary>
    public abstract class GameThingBase : GameObjectBase, IBeforeSave, IDisposable, IDbQuickFind
    {
        protected GameThingBase()
        {
        }

        protected GameThingBase(Guid id) : base(id)
        {
        }

        public abstract DbContext GetDbContext();

        #region IDbQuickFind接口相关

        /// <summary>
        /// 记录一些额外的信息，通常这些信息用于排序，加速查找符合特定要求的对象。
        /// </summary>
        [MaxLength(64)]
        public string ExtraString { get; set; }

        /// <summary>
        /// 记录一些额外的信息，用于排序搜索使用的字段。
        /// </summary>
        public decimal? ExtraDecimal { get; set; }

        /// <summary>
        /// 模板Id。
        /// </summary>
        [Column("TemplateId")]
        public Guid ExtraGuid { get; set; }

        #endregion IDbQuickFind接口相关

        #region 扩展对象相关

        /// <summary>
        /// 二进制数据。
        /// </summary>
        public byte[] BinaryArray { get; set; }

        object _BinaryObject;
        /// <summary>
        /// <see cref="BinaryArray"/>中存储的对象视图。
        /// </summary>
        /// <remarks>在保存时会调用<see cref="BinaryObject"/>的GetType获取类型。</remarks>
        [NotMapped, JsonIgnore]
        public object BinaryObject
        {
            get
            {
                if (_BinaryObject is null && BinaryArray != null && BinaryArray.Length > 0)
                {
                    using MemoryStream ms = new MemoryStream(BinaryArray);
                    //using BrotliStream bs = new BrotliStream(ms, CompressionLevel.Fastest);
                    string fullName;
                    using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                        fullName = br.ReadString();
                    var type = Type.GetType(fullName);
                    _BinaryObject = JsonSerializer.DeserializeAsync(ms, type).Result;
                }
                return _BinaryObject;
            }
            set => _BinaryObject = value;
        }

        /// <summary>
        /// 获取BinaryObject中的对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetOrCreateBinaryObject<T>() where T : class, new()
        {
            if (BinaryObject is null)
                _BinaryObject = new T();
            return (T)_BinaryObject;
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

        #endregion 快速变化属性相关

        #region 通用扩展属性及相关

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="db"></param>
        public override void PrepareSaving(DbContext db)
        {
            if (null != _Name2FastChangingProperty)   //若需要写入快速变化属性
                foreach (var item in _Name2FastChangingProperty)
                {
                    item.Value.ToDictionary(Properties, item.Key);
                }
            if (_BinaryObject != null)
            {
                var fullName = _BinaryObject.GetType().AssemblyQualifiedName;
                MemoryStream ms;
                using (ms = new MemoryStream())
                {
                    using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
                        bw.Write(fullName);
                    JsonSerializer.SerializeAsync(ms, _BinaryObject, _BinaryObject.GetType()).Wait();
                }
                BinaryArray = ms.ToArray();
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
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Name2FastChangingProperty = null;
                _BinaryObject = null;
                BinaryArray = null;
                base.Dispose(disposing);
            }
        }

    }
}
