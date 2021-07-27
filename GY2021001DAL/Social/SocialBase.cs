using OwGame;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace GY2021001DAL
{
    public abstract class GameSocialBase : GameObjectBase
    {
        public GameSocialBase()
        {

        }

        public GameSocialBase(Guid id) : base(id)
        {
        }

        private readonly object _ThisLocker = new object();

        /// <summary>
        /// 同步锁。
        /// </summary>
        [NotMapped]
        public object ThisLocker => _ThisLocker;

        /// <summary>
        /// <see cref="Properties"/>属性的后备字段。
        /// </summary>
        private string _PropertiesString;

        /// <summary>
        /// 属性字符串。
        /// </summary>
        public string PropertyString
        {
            get => _PropertiesString;
            set => _PropertiesString = value;
        }

        private Dictionary<string, object> _Properties;
        /// <summary>
        /// 对属性字符串的解释。键是属性名，字符串类型。值有三种类型，decimal,string,decimal[]。
        /// 特别注意，如果需要频繁计算，则应把用于战斗的属性单独放在其他字典中。该字典因大量操作皆为读取，拆箱问题不大，且非核心战斗才会较多的使用该系统。
        /// </summary>
        [NotMapped]
        public Dictionary<string, object> Properties
        {
            get
            {
                if (null == _Properties)
                    lock (ThisLocker)
                        if (null == _Properties)
                        {
                            _Properties = new Dictionary<string, object>();
                            OwHelper.AnalysePropertiesString(PropertyString, _Properties);
                        }
                return _Properties;
            }
        }


    }

}
