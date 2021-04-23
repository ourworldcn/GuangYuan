using OwGame;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    }

    /// <summary>
    /// 游戏内部事物的基类(非容器)。
    /// </summary>
    public abstract class GameThingBase : GameObjectBase
    {
        private string _PropertiesString;
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
        /// 创建该对象的通用协调时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 属性字符串。
        /// </summary>
        public string PropertiesString
        {
            get => _PropertiesString;
            set
            {
                _PropertiesString = value;
                ClearProperties();
            }
        }

        /// <summary>
        /// 刷新属性字符串。
        /// </summary>
        /// <param name="value"></param>
        protected void RefreshProperties(string value)
        {
            _StringProperties = new Dictionary<string, string>();
            _NumberProperties = new Dictionary<string, float>();
            _SequenceProperties = new Dictionary<string, float[]>();
            OwHelper.AnalysePropertiesString(value, _StringProperties, _NumberProperties, _SequenceProperties);
        }

        /// <summary>
        /// 清除动态属性。
        /// </summary>
        protected void ClearProperties()
        {
            _NumberProperties = null;
            _SequenceProperties = null;
            _StringProperties = null;
        }

        private Dictionary<string, float> _NumberProperties;
        /// <summary>
        /// 数字属性。
        /// </summary>
        [NotMapped]
        public Dictionary<string, float> NumberProperties
        {
            get
            {
                lock (this)
                {
                    if (null == _NumberProperties)
                        RefreshProperties(PropertiesString);
                }
                return _NumberProperties;
            }
        }

        private Dictionary<string, float[]> _SequenceProperties;
        /// <summary>
        /// 序列属性。
        /// </summary>
        [NotMapped]
        public Dictionary<string, float[]> SequenceProperties
        {
            get
            {
                lock (this)
                {
                    if (null == _SequenceProperties)
                        RefreshProperties(PropertiesString);
                }
                return _SequenceProperties;
            }
        }

        private Dictionary<string, string> _StringProperties;
        /// <summary>
        /// 字符串属性。
        /// </summary>
        [NotMapped]
        public Dictionary<string, string> StringProperties
        {
            get
            {
                lock (this)
                {
                    if (null == _StringProperties)
                        RefreshProperties(PropertiesString);
                }
                return _StringProperties;
            }
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
}
