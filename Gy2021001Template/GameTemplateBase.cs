using OwGame;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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

    }
}
