using Gy2021001Template;
using OwGame;
using OwGame.Expression;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GY2021001DAL
{
    public abstract class GameObjectBase : GuidKeyBase
    {
        public GameObjectBase()
        {

        }

        public GameObjectBase(Guid id) : base(id)
        {

        }

        #region 事件及相关

        #endregion 事件及相关
    }

    /// <summary>
    /// 游戏内部事物的基类。
    /// </summary>
    public abstract class GameThingBase : GameObjectBase, IDisposable
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameThingBase()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id"></param>
        public GameThingBase(Guid id) : base(id)
        {

        }

        #endregion 构造函数

        /// <summary>
        /// 客户端要记录的一些属性，这个属性客户端可以随意更改，服务器不使用。
        /// </summary>
        public string ClientGutsString { get; set; }

        /// <summary>
        /// 创建该对象的通用协调时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// <see cref="Properties"/>属性的后备字段。
        /// </summary>
        private string _PropertiesString;

        /// <summary>
        /// 属性字符串。
        /// </summary>
        public string PropertiesString
        {
            get => _PropertiesString;
            set
            {
                if (_PropertiesString != value)
                {
                    _PropertiesString = value;
                    _Properties = null;
                }
            }
        }

        private Dictionary<string, object> _Properties;
        /// <summary>
        /// 对属性字符串的解释。键是属性名，字符串类型。值有三种类型，decimal,string,decimal[]。
        /// 特别注意，如果需要频繁计算，则应把用于战斗的属性单独放在其他字典中。该字典因大量操作皆为读取，拆箱问题不大，且非核心战斗才会较多的使用该系统。
        /// </summary>
        [NotMapped]
        public Dictionary<string, object> Properties
        {
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            get
            {
                if (null == _Properties)
                    lock (this)
                        if (null == _Properties)
                        {
                            _Properties = new Dictionary<string, object>();
                            OwHelper.AnalysePropertiesString(PropertiesString, _Properties);
                        }
                return _Properties;
            }
        }

        /// <summary>
        /// 获取指定名称的属性名。调用<see cref="TryGetPropertyValue(string, out object)"/>来实现。
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetPropertyValueOrDefault(string propertyName, object defaultVal = default) =>
            TryGetPropertyValue(propertyName, out var result) ? result : defaultVal;

        /// <summary>
        /// 获取指定属性名称的属性值。
        /// </summary>
        /// <param name="propertyName">动态属性的名称。</param>
        /// <param name="result">动态属性的值。</param>
        /// <returns>true成功返回属性，false未找到属性。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public virtual bool TryGetPropertyValue(string propertyName, out object result)
        {
            bool succ;
            switch (propertyName)
            {
                default:
                    if (Name2FastChangingProperty.TryGetValue(propertyName, out var fcp))   //若存在渐变属性
                    {
                        succ = true;
                        result = fcp.GetCurrentValueWithUtc();
                    }
                    else
                    {
                        succ = Properties.TryGetValue(propertyName, out result);
                        if (!succ && null != Template)
                            succ = Template.TryGetPropertyValue(propertyName, out result);
                    }
                    break;
            }
            return succ;
        }

        /// <summary>
        /// 设置一个属性。
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="val"></param>
        /// <returns>true，如果属性名存在或确实应该有(基于某种需要)，且设置成功。false，设置成功一个不存在且不认识的属性。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public virtual bool SetPropertyValue(string propertyName, object val)
        {
            bool succ;
            switch (propertyName)
            {
                default:
                    succ = TryGetPropertyValue(propertyName, out var oldVal);
                    if (!succ || !Equals(oldVal, val))
                    {
                        Properties[propertyName] = val;
                        succ = true;
                    }
                    break;
            }
            return succ;
        }

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
                            var list = FastChangingPropertyExtensions.FromGameThing(this);
                            var charId = (this as GameItem)?.GameChar?.Id ?? Guid.Empty;
                            foreach (var item in list)
                            {
                                item.Tag = (charId, Id);    //设置Tag
                            }
                            _Name2FastChangingProperty = list.ToDictionary(c => c.Name);
                        }
                }
                return _Name2FastChangingProperty;
            }
        }

        /// <summary>
        /// 获取属性，且考虑是否刷新并写入快速变化属性。
        /// </summary>
        /// <param name="name">要获取值的属性名。</param>
        /// <param name="refreshDate">当有快速变化属性时，刷新时间，如果为null则不刷新。</param>
        /// <param name="writeDictionary">当有快速变化属性时，是否写入<see cref="Properties"/>属性。</param>
        /// <param name="result">属性的当前返回值。对快速变化属性是其<see cref="FastChangingProperty.LastValue"/>,是否在之前刷新取决于<paramref name="refresh"/>参数。</param>
        /// <param name="refreshDatetime">如果是快速变化属性且需要刷新，则此处返回实际的计算时间。
        /// 如果找到的不是快速渐变属性返回<see cref="DateTime.MinValue"/></param>
        /// <returns>true成功找到属性。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public virtual bool TryGetPropertyValueWithFcp(string name, DateTime? refreshDate, bool writeDictionary, out object result, out DateTime refreshDatetime)
        {
            bool succ;
            if (Name2FastChangingProperty.TryGetValue(name, out var fcp)) //若找到快速变化属性
            {
                if (refreshDate.HasValue) //若需要刷新
                {
                    refreshDatetime = refreshDate.Value;
                    result = fcp.GetCurrentValue(ref refreshDatetime);
                }
                else
                {
                    refreshDatetime = DateTime.MinValue;
                    result = fcp.LastValue;
                }
                if (writeDictionary)
                    fcp.ToGameThing(this);
                succ = true;
            }
            else //若是其他属性
            {
                refreshDatetime = DateTime.MinValue;
                succ = Properties.TryGetValue(name, out result);
            }
            return succ;
        }

        /// <summary>
        ///  获取属性，若是快速变化属性时会自动用当前时间刷新且写入<see cref="Properties"/>。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetPropertyValueWithFcp(string name, out object result)
        {
            DateTime dt = DateTime.UtcNow;
            return TryGetPropertyValueWithFcp(name, dt, true, out result, out _);
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
        #endregion 快速变化属性相关

        #region 扩展属性相关

        /// <summary>
        /// 服务器用通用扩展属性集合。
        /// </summary>
        public virtual List<GameExtendProperty> ExtendProperties { get; } = new List<GameExtendProperty>();

        /// <summary>
        /// 获取或创建一个指定名称的<see cref="GameExtendProperty"/>对象。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="creator">创建器。</param>
        /// <returns>获取或创建的对象。返回时创建的对象已经被加入了集合，且设置了必要导航属性。</returns>
        public GameExtendProperty GetOrAddExtendProperty(string name, Func<string, GameExtendProperty> creator)
        {
            var result = ExtendProperties.FirstOrDefault(c => c.Name == name);
            if (result is null)
            {
                result = creator(name);
                result.GameThing = this;
                result.ParentId = Id;
                ExtendProperties.Add(result);
            }
            return result;
        }


        private ConcurrentDictionary<string, object> _TemporaryDictionary;

        /// <summary>
        /// 这里记录一些临时属性。
        /// 这些属性不会在保存时持久化，也不会在创建或加载时初始化。
        /// </summary>
        public ConcurrentDictionary<string, object> TemporaryDictionary => _TemporaryDictionary ??= new ConcurrentDictionary<string, object>();

        #endregion 扩展属性相关

        #region 事件及相关
        protected virtual void OnSaving(EventArgs e)
        {
            try
            {
                Saving?.Invoke(this, e);
            }
            finally
            {
                foreach (var item in Name2FastChangingProperty)
                {
                    FastChangingPropertyExtensions.ToDictionary(item.Value, Properties, item.Key);
                }
                PropertiesString = OwHelper.ToPropertiesString(Properties);
            }
        }

        public event EventHandler Saving;

        /// <summary>
        /// 通知该实例，即将保存到数据库。
        /// </summary>
        /// <param name="e"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeSaving(EventArgs e)
        {
            OnSaving(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services">服务容器，必须有<see cref="IGameThingHelper"/>服务。</param>
        public void InvokeLoaded(IServiceProvider services)
        {
            var helper = services.GetService(typeof(IGameThingHelper)) as IGameThingHelper;
            Template = helper.GetTemplateFromeId(TemplateId);
        }


        /// <summary>
        /// 引发<see cref="Created"/>事件。
        /// </summary>
        /// <param name="services">服务容器，必须有<see cref="IGameThingHelper"/>服务。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeCreated(IServiceProvider services)
        {
            var helper = services.GetService(typeof(IGameThingHelper)) as IGameThingHelper;
            Template = helper.GetTemplateFromeId(TemplateId);
        }

        #endregion 事件及相关

        /// <summary>
        /// 模板对象。
        /// </summary>
        [NotMapped]
        public GameThingTemplateBase Template { get; set; }

        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TemplateId { get; set; }

        #region IDisposable接口相关

        private bool _IsDisposed;

        /// <summary>
        /// 对象是否已经被处置。
        /// </summary>
        protected bool IsDisposed => _IsDisposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _TemporaryDictionary = null;
                _IsDisposed = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameThingBase()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion IDisposable接口相关
    }

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
    /// 客户端使用通用扩展属性类。
    /// </summary>
    public class GameClientExtendProperty : GuidKeyBase
    {
        public GameClientExtendProperty()
        {

        }

        /// <summary>
        /// 获取或设置所属对象Id。
        /// </summary>
        public Guid ParentId { get; set; }

        /// <summary>
        /// 获取或设置键的名字，同一个所属对象下不能有多个同名设置，否则，行为未知。
        /// </summary>
        [StringLength(64)]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置值。
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// 服务器内部使用的通用扩展属性。
    /// </summary>
    public class GameExtendProperty
    {
        public GameExtendProperty()
        {

        }

        public GameExtendProperty(string name)
        {
            Name = name;
        }

        [ForeignKey(nameof(GameThing))]
        public Guid ParentId { get; set; }

        public virtual GameThingBase GameThing { get; set; }

        [MaxLength(64)]
        public string Name { get; set; }

        [MaxLength(256)]
        public string StringValue { get; set; }

        public int IntValue { get; set; }

        public decimal DecimalValue { get; set; }

        public double DoubleValue { get; set; }

        public string Text { get; set; }
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
            var _ = obj as GameThingBase;
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
            var _ = obj as GameThingBase;
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
        /// <param name="obj"></param>
        /// <param name="thing"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void ToGameThing(this FastChangingProperty obj, GameThingBase thing)
        {
            obj.ToDictionary(thing.Properties, obj.Name);
        }

        /// <summary>
        /// 从属性集合生成渐变属性对象。
        /// </summary>
        /// <param name="dic">至少要有fcpiXXX,fcpdXXX,fcpmXXX三个属性才能生成。</param>
        /// <param name="name">主名称，XXX,不带fcpi等前缀。</param>
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
        /// <param name="thing"></param>
        /// <param name="classPrefix"></param>
        /// <returns></returns>
        static public IEnumerable<FastChangingProperty> FromGameThing(GameThingBase thing, string classPrefix = DefaultClassPrefix)
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
        /// <param name="dic"></param>
        /// <param name="name"></param>
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool TryGetDecimalPropertyValue(this GameThingBase obj, string propertyName, out decimal result)
        {
            if (obj.TryGetPropertyValue(propertyName, out var tmp))
                return OwHelper.TryGetDecimal(tmp, out result);
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
        static public decimal GetDecimalOrDefault(this GameThingBase @this, string propertyName, decimal defaultVal = decimal.Zero) =>
            !@this.TryGetPropertyValue(propertyName, out var obj) || !OwHelper.TryGetDecimal(obj, out var dec) ? defaultVal : dec;

        /// <summary>
        /// 获取堆叠上限。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns><see cref="decimal.MaxValue"/>无限制，如果不可堆叠则为0.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public decimal GetStc(this GameThingBase obj)
        {
            var stc = obj.GetDecimalOrDefault("stc", 0);
            return stc == -1 ? decimal.MaxValue : stc;
        }

    }

    /// <summary>
    /// <see cref="GameThingBase"/>用到的服务。
    /// </summary>
    public interface IGameThingHelper
    {
        public GameItemTemplate GetTemplateFromeId(Guid id);

    }

}
