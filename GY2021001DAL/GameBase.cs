﻿using Gy2021001Template;
using OwGame;
using OwGame.Expression;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace GY2021001DAL
{
    public class GameObjectBase : GuidKeyBase
    {
        public GameObjectBase()
        {

        }

        public GameObjectBase(Guid id) : base(id)
        {

        }

        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TemplateId { get; set; }

        [NotMapped]
        GameTemplateBase Template { get; set; }

        public virtual T GetProperyValue<T>(string name, T defaultValue = default)
        {
            return name switch
            {
                "Id" => Id is T id ? id : defaultValue,
                "TId" => TemplateId is T id ? id : defaultValue,
                nameof(TemplateId) => TemplateId is T id ? id : defaultValue,
                _ => defaultValue,
            };
        }

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
        /// 特别注意，如果需要频繁计算，则应把用于战斗的属性单独放在其他字典中。该字典因大量操作皆为读取，反装箱问题不大。
        /// </summary>
        [NotMapped]
        public Dictionary<string, object> Properties
        {
            get
            {
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
        public override T GetProperyValue<T>(string name, T defaultValue = default)
        {
            if (Properties.TryGetValue(name, out object obj))
            {
                return (T)Convert.ChangeType(obj, typeof(T));
            }
            return base.GetProperyValue(name, defaultValue);
        }

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
    /// 通用扩展属性类。
    /// </summary>
    public class GameExtendProperty : GuidKeyBase
    {
        public GameExtendProperty()
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

}
