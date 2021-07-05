using Gy2021001Template;
using OwGame;
using OwGame.Expression;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

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
    /// 游戏内部事物的基类(非容器)。
    /// </summary>
    public abstract class GameThingBase : GameObjectBase
    {
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
                _PropertiesString = value;
                _Properties = null;
            }
        }

        Dictionary<string, object> _Properties;
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
        /// 获取属性值并强制转化类型。如果不存在指定属性或属性类型不兼容，则返回默认值。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual T GetProperyValue<T>(string name, T defaultValue = default)
        {
            if (Properties.TryGetValue(name, out object obj))
            {
                return (T)Convert.ChangeType(obj, typeof(T));
            }
            return defaultValue;
        }

        /// <summary>
        /// 获取指定名称的属性名。调用<see cref="TryGetPropertyValue(string, out object)"/>来实现。
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetValueOrDefault(string propertyName, object defaultVal = default)
        {
            if (!TryGetPropertyValue(propertyName, out var result))
                result = defaultVal;
            return result;
        }

        /// <summary>
        /// 获取指定的属性值并转换为<see cref="decimal"/>,如果找不到，或不能转换则返回指定默认值。
        /// </summary>
        /// <param name="propertyName" >
        /// <list type="table">
        /// <listheader>
        /// <term>term</term>
        /// <description>F2</description>
        /// </listheader>
        /// <item><term>term</term>
        /// <description>1</description></item>
        /// <item><term>term</term>
        /// <description>2</description></item>
        /// </list>
        /// </param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public decimal GetDecimalOrDefault(string propertyName, decimal defaultVal = decimal.Zero)
        {
            return !TryGetPropertyValue(propertyName, out var obj) || !OwHelper.TryGetDecimal(obj, out var dec) ? defaultVal : dec;
        }

        /// <summary>
        /// 获取指定属性名称的属性值。
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="result"></param>
        /// <returns>true成功返回属性，false未找到属性。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public virtual bool TryGetPropertyValue(string propertyName, out object result)
        {
            bool succ;
            switch (propertyName)
            {
                default:
                    succ = Properties.TryGetValue(propertyName, out result);
                    if (!succ && null != Template)
                        succ = Template.TryGetPropertyValue(propertyName, out result);
                    break;
            }
            return succ;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetDecimal(string propertyName, out decimal result)
        {
            if (TryGetPropertyValue(propertyName, out var obj))
                return OwHelper.TryGetDecimal(obj, out result);
            else
                result = default;
            return false;
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

        Dictionary<string, FastChangingProperty> _Name2FastChangingProperty;

        /// <summary>
        /// 快速变化属性。
        /// </summary>
        [NotMapped]
        public Dictionary<string, FastChangingProperty> Name2FastChangingProperty
        {
            get
            {
                if (null == _Name2FastChangingProperty)
                {
                    lock (this)
                        if (null == _Name2FastChangingProperty)
                        {
                            var coll = Properties.Keys.Where(c => c.StartsWith(FastChangingProperty.ClassPrefix) && c.Length > FastChangingProperty.ClassPrefix.Length + 1).
                                Select(c => c.Substring(FastChangingProperty.ClassPrefix.Length + 1)).Distinct();   //获取快速变化属性的名称集合
                            _Name2FastChangingProperty = coll.Select(c => (Name: c, FastChangingProperty.FromDictionary(Properties, c))).ToDictionary(c => c.Name, c => c.Item2);
                        }
                }
                return _Name2FastChangingProperty;
            }
        }

        /// <summary>
        /// 服务器用通用扩展属性集合。
        /// </summary>
        public virtual List<GameExtendProperty> GameExtendProperties { get; set; }

        #region 事件及相关
        protected virtual void OnSaving(EventArgs e)
        {
            foreach (var item in Name2FastChangingProperty)
            {
                FastChangingProperty.ToDictionary(item.Value, Properties, item.Key);
            }
            PropertiesString = OwHelper.ToPropertiesString(Properties);

        }

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
        /// 模板对象。
        /// </summary>
        [NotMapped]
        public GameThingTemplateBase Template { get; set; }

        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TemplateId { get; set; }

        public void InvokeLoaded(GameThingTemplateBase template)
        {
            Template = template;
        }

        public void InitialCreation(GameThingTemplateBase template)
        {
            Template = template;
        }

        #endregion 事件及相关
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
    /// 帮助<see cref="GameThingBase"/>获取<see cref="GameThingTemplateBase"/>对象的接口。
    /// </summary>
    public interface GameThingTemplateHelper
    {
        public GameThingTemplateBase GetTemplateFromeId(Guid id);

    }
}
