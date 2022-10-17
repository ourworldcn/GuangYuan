
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ObjectPool;
using OW.Game.PropertyChange;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;

namespace OW.Game.Store
{
    public interface IEntityWithSingleKey<T>
    {
        T Id { get; set; }

    }

    /// <summary>
    /// 以<see cref="Guid"/>为键类型的实体类的基类。
    /// </summary>
    public abstract class GuidKeyObjectBase : IEntityWithSingleKey<Guid>
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// 会自动用<see cref="Guid.NewGuid"/>生成<see cref="Id"/>属性值。
        /// </summary>
        public GuidKeyObjectBase()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id">指定该实体对象的<see cref="Id"/>属性。</param>
        public GuidKeyObjectBase(Guid id)
        {
            Id = id;
        }

        #endregion 构造函数

        /// <summary>
        /// 主键。
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 0)]
        public Guid Id { get; set; }

        /// <summary>
        /// 如果Id是Guid.Empty则生成新Id,否则立即返回false。
        /// </summary>
        /// <returns>true生成了新Id，false已经有了非空Id。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GenerateIdIfEmpty()
        {
            if (Guid.Empty != Id)
                return false;
            Id = Guid.NewGuid();
            return true;
        }

        #region 减低内存分配速率

        private string _IdString;

        /// <summary>
        /// 获取或设置Id的字符串表现形式。Id的字符串形式"00000000-0000-0000-0000-000000000000"从 a 到 f 的十六进制数字是小写的。
        /// 该属性第一次读取时才初始化。有利于id的池化处理。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public string IdString
        {
            get
            {
                return _IdString ??= Id.ToString();
            }
            internal set
            {
                Id = Guid.Parse(value);
                _IdString = null;
            }
        }

        private string _Base64IdString;

        /// <summary>
        /// 获取或设置Id的Base64字符串表现形式。
        /// 该属性第一次读取时才初始化。并有利于id的池化处理。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public string Base64IdString
        {
            get { return _Base64IdString ??= Id.ToBase64String(); }
            internal set
            {
                Id = OwConvert.ToGuid(value);
                _Base64IdString = value;
            }
        }

        #endregion 减低内存分配速率
    }

    public class DynamicPropertyChangedEventArgs
    {
        public DynamicPropertyChangedEventArgs()
        {

        }

        public DynamicPropertyChangedEventArgs([NotNull] string propertyName, object oldValue)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
        }

        public string PropertyName { get; set; }

        public object OldValue { get; set; }
    }

    public interface INotifyDynamicPropertyChanged
    {
        //
        // 摘要:
        //     Occurs when a property value changes.
        event EventHandler<DynamicPropertyChangedEventArgs> DynamicPropertyChanged;

    }

    /// <summary>
    /// 简单动态扩展属性（Simple dynamic extension properties，Sdep）接口。
    /// </summary>
    /// <typeparam name="T">动态属性值的类型。</typeparam>
    public interface ISimpleDynamicProperty<T>
    {
        /// <summary>
        /// 对属性字符串的解释。键是属性名，字符串类型。值有三种类型，decimal,string,decimal[]。
        /// 特别注意，如果需要频繁计算，则应把用于战斗的属性单独放在其他字典中。该字典因大量操作皆为读取，拆箱问题不大，且非核心战斗才会较多的使用该属性。
        /// 频繁发生变化的战斗属性，请另行生成对象。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public abstract Dictionary<string, T> Properties
        {
            get;
        }

        /// <summary>
        /// 追加或设置动态属性。
        /// 虽然一般动态属性存在于<see cref="Properties"/>中，但派生类可能需要存储在其它位置，可重载此成员以控制读写位置。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public abstract void SetSdp(string name, T value);

        /// <summary>
        /// 获取动态属性。
        /// 虽然一般动态属性存在于<see cref="Properties"/>中，但派生类可能需要存储在其它位置，可重载此成员以控制读写位置。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns>true找到属性并返回，false没有找到指定名称的属性。</returns>
        public abstract bool TryGetSdp(string name, out T value);

        /// <summary>
        /// 移除动态属性。
        /// </summary>
        /// <param name="name"></param>
        /// <returns>true成功移除，false指定属性不存在或是不可移除的属性。</returns>
        public abstract bool RemoveSdp(string name);

        /// <summary>
        /// 获取所有动态属性。
        /// </summary>
        /// <returns>注意在遍历返回集合时一般不可以更改集合，除非实现类有特别说明。</returns>
        public IEnumerable<(string, T)> GetAllSdp();
    }

    /// <summary>
    /// 提供一个基类，包含一个编码为字符串的压缩属性。且该字符串可以理解为一个字典的内容。
    /// </summary>
    public abstract class SimpleDynamicPropertyBase : GuidKeyObjectBase, IBeforeSave, IDisposable, INotifyDynamicPropertyChanged, ISimpleDynamicProperty<object>
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public SimpleDynamicPropertyBase()
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public SimpleDynamicPropertyBase(Guid id) : base(id)
        {
        }

        #region ISimpleDynamicExtensionProperties接口相关

        private Dictionary<string, object> _Properties;
        /// <summary>
        /// 对属性字符串的解释。键是属性名，字符串类型。值有三种类型，decimal,string,decimal[]。
        /// 特别注意，如果需要频繁计算，则应把用于战斗的属性单独放在其他字典中。该字典因大量操作皆为读取，拆箱问题不大，且非核心战斗才会较多的使用该属性。
        /// 频繁发生变化的战斗属性，请另行生成对象。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public Dictionary<string, object> Properties
        {
            get
            {
                if (_Properties is null)
                    lock (this)
                        if (_Properties is null)
                        {
                            var tmp = AutoClearPool<Dictionary<string, object>>.Shared.Get();
                            OwConvert.Copy(PropertiesString, tmp);
                            _Properties = tmp;
                        }
                return _Properties;
            }
        }

        public virtual void SetSdp(string name, object value)
        {
            Properties[name] = value;
        }

        public virtual bool TryGetSdp(string name, out object value)
        {
            return Properties.TryGetValue(name, out value);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public virtual IEnumerable<(string, object)> GetAllSdp()
        {
            return Properties.Select(c => (c.Key, c.Value));
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual bool RemoveSdp(string name)
        {
            return Properties.Remove(name);
        }

        #endregion ISimpleDynamicExtensionProperties接口相关
        private string _PropertiesString;

        /// <summary>
        /// 属性字符串。格式如:atk=20.5,tid=933323D7-3A9B-4B0A-9072-E6AAD3FAC411,def=10|20|30,
        /// 数字，时间，Guid，字符串。
        /// </summary>
        public string PropertiesString
        {
            get => _PropertiesString;
            set
            {
                if (!ReferenceEquals(_PropertiesString, value))
                {
                    _PropertiesString = value;
                    _Properties = null;
                }
            }
        }

        #region 事件及其相关

        protected virtual void OnDynamicPropertyChanged(DynamicPropertyChangedEventArgs e) => DynamicPropertyChanged?.Invoke(this, e);
        public event EventHandler<DynamicPropertyChangedEventArgs> DynamicPropertyChanged;

        public void InvokeDynamicPropertyChanged(DynamicPropertyChangedEventArgs e)
        {
            OnDynamicPropertyChanged(e);
        }
        #endregion  事件及其相关

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="db"><inheritdoc/></param>
        public virtual void PrepareSaving(DbContext db)
        {
            if (_Properties is null) //若未初始化字典
                return; //不变更属性
            _PropertiesString = OwConvert.ToString(Properties); //写入字段而非属性，写入属性导致字典需要重新初始化。
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public bool SuppressSave { get; set; }

        #region IDisposable接口及相关

        private volatile bool _IsDisposed;
        /// <summary>
        /// 对象是否已经被处置。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public bool IsDisposed
        {
            get => _IsDisposed;
            protected set => _IsDisposed = value;
        }

        /// <summary>
        /// 实际处置当前对象的方法。
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                if (null != _Properties)
                {
                    AutoClearPool<Dictionary<string, object>>.Shared.Return(_Properties);
                    _Properties = null;
                }
                _PropertiesString = null;
                _IsDisposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~SimpleDynamicPropertyBase()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// 处置对象。
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable接口及相关

    }

    public static class SimpleDynamicPropertyBaseExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <param name="newValue"></param>
        /// <param name="tag"></param>
        /// <param name="changes">变化数据的集合，如果值变化了，将向此集合追加变化数据对象。若省略或为null则不追加。</param>
        /// <returns>true设置了变化数据，false,新值与旧值没有变化。</returns>
        public static bool SetPropertyAndAddChangedItem(this SimpleDynamicPropertyBase obj, string name, object newValue, [AllowNull] object tag = null,
            [AllowNull] ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            bool result;
            var isExists = obj.TryGetSdp(name, out var oldValue);  //存在旧值
            if (!isExists || !Equals(oldValue, newValue)) //若新值和旧值不相等
            {
                if (null != changes)    //若需要设置变化数据
                {
                    var item = GamePropertyChangeItemPool<object>.Shared.Get();
                    item.Object = obj; item.PropertyName = name; item.Tag = tag;
                    if (isExists)
                        item.OldValue = oldValue;
                    item.HasOldValue = isExists;
                    item.NewValue = newValue;
                    item.HasNewValue = true;
                    changes.Add(item);
                }
                obj.SetSdp(name, newValue);
                result = true;
            }
            else
                result = false;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sdp"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetSdpValueOrDefault<T>(this ISimpleDynamicProperty<T> sdp, string name, T defaultValue = default) =>
            sdp.TryGetSdp(name, out var result) ? result : defaultValue;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sdp"></param>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetSdpDecimal(this ISimpleDynamicProperty<object> sdp, string name, out decimal result)
        {
            if (sdp.TryGetSdp(name, out var obj) && OwConvert.TryToDecimal(obj, out result))
                return true;
            else
            {
                result = default;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetSdpDecimalOrDefault(this ISimpleDynamicProperty<object> sdp, string name, decimal defaultVal = default) =>
             sdp.TryGetSdpDecimal(name, out var result) ? result : defaultVal;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sdp"></param>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetSdpGuid(this ISimpleDynamicProperty<object> sdp, string name, out Guid result)
        {
            if (!sdp.TryGetSdp(name, out var obj))
            {
                result = default;
                return false;
            }
            return OwConvert.TryToGuid(obj, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid GetSdpGuidOrDefault(this ISimpleDynamicProperty<object> sdp, string name, Guid defaultVal = default)
        {
            return sdp.TryGetSdp(name, out var obj) && OwConvert.TryToGuid(obj, out var result) ? result : defaultVal;
        }


        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryGetSdpDateTime(this ISimpleDynamicProperty<object> sdp, string name, out DateTime result)
        {
            if (sdp.TryGetSdp(name, out var obj) && OwConvert.TryGetDateTime(obj, out result))
                return true;
            else
            {
                result = default;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime GetSdpDateTimeOrDefault(this ISimpleDynamicProperty<object> sdp, string name, DateTime defaultVal = default) =>
            sdp.TryGetSdpDateTime(name, out var result) ? result : defaultVal;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sdp"></param>
        /// <param name="name"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static string GetSdpStringOrDefault(this ISimpleDynamicProperty<object> sdp, string name, string defaultVal = default)
        {
            if (!sdp.TryGetSdp(name, out var obj))
            {
                return defaultVal;
            }
            return obj switch
            {
                _ when obj is string => (string)obj,
                _ => obj?.ToString(),
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sdp"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetSdpBooleanOrDefaut(this ISimpleDynamicProperty<object> sdp, string key, bool defaultVal = default)
        {
            if (!sdp.TryGetSdp(key, out var obj))
                return defaultVal;
            return OwConvert.TryToBoolean(obj, out var result) ? result : defaultVal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sdp"></param>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveSdp(this ISimpleDynamicProperty<object> sdp, string key, out object result)
        {
            return !sdp.TryGetSdp(key, out result) ? false : sdp.RemoveSdp(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="sdp"></param>
        /// <param name="dic"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T, TValue>(this ISimpleDynamicProperty<T> sdp, IDictionary<string, TValue> dic) where T : TValue
        {
            foreach (var item in sdp.GetAllSdp())
            {
                dic[item.Item1] = item.Item2;
            }
        }

        /// <summary>
        /// 复制sdp属性。
        /// </summary>
        /// <typeparam name="TSrc"></typeparam>
        /// <typeparam name="TDest"></typeparam>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<TSrc, TDest>(this ISimpleDynamicProperty<TSrc> src, ISimpleDynamicProperty<TDest> dest) where TSrc : TDest
        {
            foreach (var item in src.GetAllSdp())
            {
                dest.SetSdp(item.Item1, item.Item2);
            }
        }
    }

    /// <summary>
    /// 描述虚拟世界内对象关系的通用基类。
    /// </summary>
    /// <remarks>
    /// 以下建议仅针对，联合主键是前面的更容易引发查找的情况：
    /// 通常应使用Id属性指代最长查找的实体——即"我"这一方，Id2可以记录关系对象Id。
    /// </remarks>
    [NotMapped]
    public class GameEntityRelationshipBase : SimpleDynamicPropertyBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameEntityRelationshipBase()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id">实体Id,用于标识主体Id。</param>
        public GameEntityRelationshipBase(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id">实体Id,用于标识主体Id。</param>
        /// <param name="id2"><seealso cref="Id2"/></param>
        /// <param name="keyType"><seealso cref="KeyType"/></param>
        /// <param name="flag"><seealso cref="Flag"/></param>
        public GameEntityRelationshipBase(Guid id, Guid id2, int keyType, int flag) : base(id)
        {
            Id2 = id2;
            KeyType = keyType;
            Flag = flag;
        }

        /// <summary>
        /// 客体实体Id。
        /// </summary>
        public Guid Id2 { get; set; }

        /// <summary>
        /// 与Id Id2组成联合主键。且有自己的单独重复索引。
        /// </summary>
        public int KeyType { get; set; }

        /// <summary>
        /// 一个标志位，具体区别该对象标识的物品或关系状态，具有单独重复索引。
        /// </summary>
        public int Flag { get; set; }

        /// <summary>
        /// 记录额外信息，最长64字符，且有单独重复索引。
        /// </summary>
        [MaxLength(64)]
        public string PropertyString { get; set; }

    }

    public static class PocoLoadingExtensions
    {
        public static TRelated Load<TRelated>(
            this Action<object, string> loader,
            object entity,
            ref TRelated navigationField,
            [CallerMemberName] string navigationName = null)
            where TRelated : class
        {
            loader?.Invoke(entity, navigationName);

            return navigationField;
        }

    }

    /// <summary>
    /// POCO类在被保存前需要调用此接口将一些数据写入可存储的字段中。
    /// </summary>
    public interface IBeforeSave
    {
        /// <summary>
        /// 实体类在被保存前需要调用该成员。应该仅写入自身拥有的直接存储于数据库的简单字段。
        /// 不要引用其他存储于数据库中的实体。否则，需要考虑重载其他实体的该接口方法，保证不会反复提交，或者是有序的保存。
        /// </summary>
        /// <param name="db">该实体类将被保存到的数据库上下文。</param>
        void PrepareSaving(DbContext db);

        /// <summary>
        /// 是否取消<see cref="PrepareSaving"/>的调用。
        /// </summary>
        /// <value>true不会调用保存方法，false(默认值)在保存前调用保存方法。</value>
        bool SuppressSave => false;
    }

    /// <summary>
    /// 创建读取或写入游戏数据相关的服务。
    /// </summary>
    public interface IGameStore
    {
        public GameUserContext CreateNewUserDbContext();

        public GameTemplateContext CreateNewTemplateContext();
    }

    public abstract class GameStoreBase : IGameStore
    {
        public virtual GameTemplateContext CreateNewTemplateContext()
        {
            return new GameTemplateContext(null);
        }

        public virtual GameUserContext CreateNewUserDbContext()
        {
            return new GameUserContext(null);
        }
    }

}