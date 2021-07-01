using OwGame;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gy2021001Template
{
    public abstract class GameTemplateBase : GuidKeyBase
    {
        public GameTemplateBase()
        {

        }

        public GameTemplateBase(Guid id) : base(id)
        {

        }

        /// <summary>
        /// 服务器不是用该属性。仅用于人读备注。
        /// </summary>
        [Column("备注", Order = 90)]
        public string Remark { get; set; }

        private IServiceProvider _Service;

        internal protected IServiceProvider Service => _Service;

        public void SetService(IServiceProvider service)
        {
            _Service = service;
        }

    }

    /// <summary>
    /// 虚拟事物对象的模板类。
    /// </summary>
    public abstract class GameThingTemplateBase : GameTemplateBase
    {
        /// <summary>
        /// 
        /// </summary>
        public GameThingTemplateBase()
        {

        }

        public GameThingTemplateBase(Guid id) : base(id)
        {

        }

        /// <summary>
        /// 显示名称。
        /// </summary>
        public string DisplayName { get; set; }

        private string _PropertiesString;

        /// <summary>
        /// 属性字符串。
        /// </summary>
        public string PropertiesString
        {
            get => _PropertiesString;
            set => _PropertiesString = value;
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
        /// 该模板创建对象应有的子模板Id字符串集合。用逗号分割。
        /// </summary>
        public string ChildrenTemplateIdString { get; set; }

        private List<Guid> _ChildrenTemplateIds;

        /// <summary>
        /// 该模板创建对象应有的子模板Id集合。
        /// </summary>
        [NotMapped]
        public List<Guid> ChildrenTemplateIds
        {
            get
            {
                if (null == _ChildrenTemplateIds)
                    lock (this)
                        if (null == _ChildrenTemplateIds)
                        {
                            if (string.IsNullOrWhiteSpace(ChildrenTemplateIdString))
                                _ChildrenTemplateIds = new List<Guid>();
                            else
                                _ChildrenTemplateIds = ChildrenTemplateIdString.Split(OwHelper.CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries).Select(c => Guid.Parse(c)).ToList();
                        }
                return _ChildrenTemplateIds;
            }
        }

        /// <summary>
        /// 获取属性值。
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public virtual object GetPropertyValue(string propertyName, object defaultVal = default)
        {
            object result;
            switch (propertyName)
            {
                case "Id":
                    result = Id;
                    break;
                default:
                    result = Properties.GetValueOrDefault(propertyName, defaultVal);
                    break;
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool TryGetPropertyValue(string propertyName, out object result)
        {
            bool succ;
            switch (propertyName)
            {
                default:
                    succ = Properties.TryGetValue(propertyName, out result);
                    break;
            }
            return succ;
        }

        /// <summary>
        /// 获取指定名称指定级别的序列属性的值。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level"></param>
        /// <param name="result"></param>
        /// <returns>true返回值，false,指定名称的属性不是序列属性，或指定级别超出限制。</returns>
        public bool TryGetValueWithLevel<T>(string name, int level, out T result)
        {
            var ary = GetSequenceProperty<T>(name);
            if (level < 0 || null == ary || level >= ary.Length)
            {
                result = default;
                return false;
            }
            result = ary[level];
            return true;
        }

        /// <summary>
        /// 获取指定名称的属性的最大等级（基于0）.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>指定名称序列属性最大等级，null指定名称不是序列属性。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int? GetMaxLevel(string name) => (Properties.GetValueOrDefault(name) as Array)?.Length;

        /// <summary>
        /// 试图获取指定的序列属性的值。
        /// </summary>
        /// <param name="name"></param>
        /// <returns>序列属性（数组表示），null表示不存在指定名称的属性或其不是指定元素类型的序列属性。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] GetSequenceProperty<T>(string name) => Properties.GetValueOrDefault(name) as T[];

        string[] _SequencePropertyNames;
        /// <summary>
        /// 获取所有序列属性名的数组。
        /// </summary>
        public IEnumerable<string> SequencePropertyNames
        {
            get
            {
                lock (this)
                    if (null == _SequencePropertyNames)
                    {
                        _SequencePropertyNames = Properties.Where(c => c.Value is Array).Select(c => c.Key).ToArray();
                    }
                return _SequencePropertyNames;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks></remarks>
        /// <returns></returns>
        public override string ToString()
        {
            var tmp = Id.ToString();
            return $"{DisplayName}(Properties.Count = {Properties.Count}, Id = {{{tmp[0..4]}...{tmp[^4..^0]}}})";
        }
    }
}
