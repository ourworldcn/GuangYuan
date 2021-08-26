﻿using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using OW.Game.Expression;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

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
    /// 服务器代码使用的通用扩展属性类。
    /// </summary>
    public class ExtendPropertyDescriptor
    {
        /// <summary>
        /// 持久化标志。
        /// <see cref="GameExtendProperty.StringValue"/>是该字符串开头，
        /// 且<see cref="GameExtendProperty.IntValue"/>指定了<see cref="GameExtendProperty.Text"/>，开头多少个字符是类型全名，且后跟一个分号.然后是Json序列化的内容。
        /// 则该<see cref="GameExtendProperty"/>对象会被认为是一个需要持久化的属性。
        /// </summary>
        public const string MarkIdString = "a88c6717-4fdc-4cb0-b127-e1799ebf3b35";

        /// <summary>
        /// 试图从<see cref="GameExtendProperty"/>中转化得到<see cref="ExtendPropertyDescriptor"/>对象。
        /// 特别地，本成员使用了反射，因此程序集改名导致原有数据无法读回。
        /// </summary>
        /// <param _Name="obj"></param>
        /// <param _Name="result"></param>
        /// <returns>true成功得到对象，false转化错误。</returns>
        static public bool TryParse(GameExtendProperty obj, out ExtendPropertyDescriptor result)
        {
            if (MarkIdString != obj.StringValue)    //若不是特定标记开头
            {
                result = null;
                return false;
            }
            if (obj.IntValue <= 0 || obj.Text.Length <= obj.IntValue + 1 || obj.Text[obj.IntValue] != ';')   //若格式不正确
            {
                result = null;
                return false;
            }
            var fullName = obj.Text[..obj.IntValue];
            var type = Type.GetType(fullName);
            if (type is null)   //若找不到指定类
            {
                result = null;
                return false;
            }
            var guts = obj.Text[(obj.IntValue + 1)..];
            result = new ExtendPropertyDescriptor()
            {
                Data = string.IsNullOrWhiteSpace(guts) ? default : JsonSerializer.Deserialize(guts, type),
                IsPersistence = true,
                Name = obj.Name,
                Type = type,
            };
            return true;
        }

        /// <summary>
        /// 将当前对象内容填写到指定的<see cref="GameExtendProperty"/>对象中。
        /// </summary>
        /// <param _Name="obj"></param>
        public void FillTo(GameExtendProperty obj)
        {
            var fullName = Type.AssemblyQualifiedName;
            obj.IntValue = fullName.Length;
            obj.Text = $"{fullName};{(Data is null ? null : JsonSerializer.Serialize(Data, Type))}";
            obj.StringValue = MarkIdString;
            obj.Name = Name;
        }

        /// <summary>
        /// 更新或追加对象。
        /// </summary>
        /// <param _Name="srcs"></param>
        /// <param _Name="dests"></param>
        static public void Fill(IEnumerable<ExtendPropertyDescriptor> srcs, ICollection<GameExtendProperty> dests)
        {
            var coll = (from src in srcs
                        where src.IsPersistence
                        join dest in dests
                        on src.Name equals dest.Name into g
                        from tmp in g.DefaultIfEmpty()
                        select (src, dest: tmp)).ToArray();
            foreach (var (src, dest) in coll)  //更新已有对象
            {
                if (dest is null)
                {
                    var tmp = new GameExtendProperty();
                    src.FillTo(tmp);
                    dests.Add(tmp);
                }
                else
                    src.FillTo(dest);
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ExtendPropertyDescriptor()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param _Name="data"></param>
        /// <param _Name="name"></param>
        /// <param _Name="isPersistence"></param>
        /// <param _Name="type"></param>
        public ExtendPropertyDescriptor(object data, string name, bool isPersistence = false, Type type = null)
        {
            Data = data;
            Name = name;
            IsPersistence = isPersistence;
            Type = type ?? data.GetType();
        }

        /// <summary>
        /// 名称，对应<see cref="GameExtendProperty.Name"/>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// <see cref="Data"/>的实际类型，<see cref="Type.FullName"/>会存储在<see cref="GameExtendProperty.StringValue"/>中。前提是该数据需要持久化。
        /// 鉴于二进制序列化过于复杂危险，当前实现使用<see cref="JsonSerializer"/>来完成序列化工作。
        /// </summary>
        public Type Type { get; set; }

        public object Data { get; set; }

        public bool IsPersistence { get; set; }
    }

    public class GameThingPropertyHelper : GamePropertyHelper
    {
        /// <summary>
        /// 获取对象的属性、
        /// </summary>
        /// <param _Name="obj"></param>
        /// <param _Name="propertyName"></param>
        /// <param _Name="defaultValue"></param>
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
        /// <param _Name="obj"></param>
        /// <param _Name="propertyName"></param>
        /// <param _Name="val"></param>
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
        /// <param _Name="name"></param>
        /// <param _Name="classPrefix"></param>
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
        /// <param _Name="obj"></param>
        /// <param _Name="dic"></param>
        /// <param _Name="name">主名称</param>
        /// <param _Name="classPrefix"></param>
        static public void ToDictionary(this FastChangingProperty obj, IDictionary<string, object> dic, string name, string classPrefix = DefaultClassPrefix)
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
        /// <param _Name="obj"></param>
        /// <param _Name="thing"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void ToGameThing(this FastChangingProperty obj, GameItemBase thing)
        {
            obj.ToDictionary(thing.Properties, obj.Name);
        }

        /// <summary>
        /// 从属性集合生成渐变属性对象。
        /// </summary>
        /// <param _Name="dic">至少要有fcpiXXX,fcpdXXX,fcpmXXX三个属性才能生成。</param>
        /// <param _Name="name">主名称，XXX,不带fcpi等前缀。</param>
        /// <returns>渐变属性对象，如果没有足够属性生成则返回null。</returns>
        static public FastChangingProperty FromDictionary(IReadOnlyDictionary<string, object> dic, string name, string classPrefix = DefaultClassPrefix)
        {
            Debug.Assert(!name.StartsWith(classPrefix), $"主名称不能以{classPrefix}开头。");
            if (!dic.TryGetValue($"{classPrefix}i{name}", out var piObj) || !OwHelper.TryGetDecimal(piObj, out var pi)) return null;
            if (!dic.TryGetValue($"{classPrefix}d{name}", out var pdObj) || !OwHelper.TryGetDecimal(pdObj, out var pd)) return null;
            if (!dic.TryGetValue($"{classPrefix}m{name}", out var pmObj) || !OwHelper.TryGetDecimal(pmObj, out var pm)) return null;

            OwHelper.TryGetDecimal(dic.GetValueOrDefault($"{classPrefix}c{name}", 0m), out var pc);
            if (!dic.TryGetValue($"{classPrefix}t{name}", out var tmpl) || !(tmpl is string strl) || !DateTime.TryParse(strl, out var pt))
                pt = DateTime.UtcNow;
            return new FastChangingProperty(TimeSpan.FromSeconds((double)pd), pi, pm, pc, pt) { Name = name };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param _Name="thing"></param>
        /// <param _Name="classPrefix"></param>
        /// <returns></returns>
        static public IEnumerable<FastChangingProperty> FromGameThing(GameObjectBase thing, string classPrefix = DefaultClassPrefix)
        {
            var dic = thing.Properties;
            var startIndex = classPrefix.Length + 1;
            var names = dic.Keys.Where(c => c.StartsWith(classPrefix)).Select(c => c[startIndex..]).Distinct();
            var coll = names.Select(c => FromDictionary(dic, c, classPrefix)).OfType<FastChangingProperty>();
            return coll;

        }

        /// <summary>
        /// 从属性列表中清楚渐变属性涉及到的属性。
        /// </summary>
        /// <param _Name="dic"></param>
        /// <param _Name="name"></param>
        static public void Clear(IDictionary<string, object> dic, string name, string classPrefix = DefaultClassPrefix)
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
        /// <param _Name="obj"></param>
        /// <param _Name="propertyName"></param>
        /// <param _Name="result"></param>
        /// <returns>true指定属性存在且能转换为数值形式；否则返回false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool TryGetDecimalPropertyValue(this GameItemBase obj, string propertyName, out decimal result)
        {
            if (obj.TryGetPropertyValue(propertyName, out var tmp) && OwHelper.TryGetDecimal(tmp, out result))
                return true;
            result = default;
            return false;
        }


        /// <summary>
        /// 获取指定的属性值并转换为<see cref="decimal"/>,如果找不到，或不能转换则返回指定默认值。
        /// </summary>
        /// <param _Name="propertyName" >
        /// </param>
        /// <param _Name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public decimal GetDecimalOrDefault(this GameItemBase obj, string propertyName, decimal defaultVal = decimal.Zero) =>
            obj.TryGetPropertyValue(propertyName, out var stcObj) && OwHelper.TryGetDecimal(stcObj, out var dec) ? dec : defaultVal;

        /// <summary>
        /// 获取堆叠上限。
        /// </summary>
        /// <param _Name="obj"></param>
        /// <returns><see cref="decimal.MaxValue"/>如果不可堆叠则为1.无限制是<see cref="decimal.MaxValue"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public decimal GetStc(this GameItemBase obj)
        {
            var stc = obj.GetDecimalOrDefault("stc", 1);
            return stc == -1 ? decimal.MaxValue : stc;
        }

        /// <summary>
        /// 是否可堆叠。
        /// </summary>
        /// <param _Name="obj"></param>
        /// <param _Name="result">如果是可堆叠对象则返回堆叠最大数量。-1是不受限制。</param>
        /// <returns>true可堆叠，false不可堆叠。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsStc(this GameItemBase obj, out decimal result) =>
            obj.TryGetDecimalPropertyValue("stc", out result);

    }

    /// <summary>
    /// <see cref="GameThingBase"/>用到的服务。
    /// </summary>
    public interface IGameThingHelper
    {
        public GameItemTemplate GetTemplateFromeId(Guid id);

    }

    /// <summary>
    /// 关键行为记录类。
    /// 此类可能放在玩家数据库中也可能放于专用的日志库中，但可能有些游戏内操作需要此数据。
    /// 当前没有启动第三上下文，暂时放在玩家数据库中。
    /// </summary>
    /// <remarks>
    /// <code>
    /// IQueryable<GameActionRecord> query; //一个查询对象
    /// DateTime dt = DateTime.UtcNow.Date;
    /// Guid charId = Guid.NewGuid();
    /// string actionId = "someThing";
    /// var coll = query.Where(c => c.DateTimeUtc >= dt && c.Id == charId && c.ActionId == actionId);
    /// </code>
    /// 索引在此情况下最有用。
    /// </remarks>
    public class GameActionRecord : SimpleExtendPropertyBase
    {
        public GameActionRecord()
        {
        }

        public GameActionRecord(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 主体对象的Id。
        /// </summary>
        public Guid ParentId { get; set; }

        /// <summary>
        /// 行为Id。
        /// </summary>
        [MaxLength(64)]
        public string ActionId { get; set; }

        /// <summary>
        /// 这个行为发生的时间。
        /// </summary>
        /// <value>默认是构造此对象的UTC时间。</value>
        public DateTime DateTimeUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 一个人眼可读的说明。
        /// </summary>
        public string Remark { get; set; }
    }

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

        ObservableCollection<GameExtendProperty> _ExtendProperties;

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

        private void GameExtendPropertiesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    DbContext.Set<GameExtendProperty>().AddRange(e.NewItems.OfType<GameExtendProperty>());
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    DbContext.Set<GameExtendProperty>().RemoveRange(e.OldItems.OfType<GameExtendProperty>());
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    DbContext.Set<GameExtendProperty>().RemoveRange(e.OldItems.OfType<GameExtendProperty>());
                    DbContext.Set<GameExtendProperty>().AddRange(e.NewItems.OfType<GameExtendProperty>());
                    break;
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
                //var removeNames = new HashSet<string>(ExtendProperties.Select(c => c.Name).Except(
                //    _ExtendPropertyDictionary.Where(c => c.Value.IsPersistence).Select(c => c.Key)));    //需要删除的对象名称
                //var removeItems = ExtendProperties.Where(c => removeNames.Contains(c.Name)).ToArray();
                //foreach (var item in removeItems)
                //    ExtendProperties.Remove(item);
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
                base.Dispose(disposing);
            }
        }
    }
}
