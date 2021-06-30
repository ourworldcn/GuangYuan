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
            set
            {
                _PropertiesString = value;
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

        public override string ToString()
        {
            var tmp = Id.ToString();
            return $"{DisplayName}(Properties.Count = {Properties.Count}, Id = {{{tmp[0..4]}...{tmp[^4..^0]}}})";
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
    }
}
