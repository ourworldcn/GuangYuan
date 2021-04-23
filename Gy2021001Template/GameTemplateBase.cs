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

        private Dictionary<string, float> _NumberProperties;

        private string _PropertiesString;

        private Dictionary<string, float[]> _SequenceProperties;

        private Dictionary<string, string> _StringProperties;

        /// <summary>
        /// 拥有的槽模板Id集合字符串，用逗号分割。
        /// </summary>
        public string SlotTemplateIdsString { get; set; }

        private List<Guid> _SlotTemplateIds;

        /// <summary>
        /// 槽模板Id集合。
        /// </summary>
        public List<Guid> SlotTemplateIds
        {
            get
            {
                lock (this)
                    if (null == _SlotTemplateIds)
                    {
                        _SlotTemplateIds = SlotTemplateIdsString.Split(',').Select(c => Guid.Parse(c)).ToList();
                    }
                return _SlotTemplateIds;
            }
        }

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

        /// <summary>
        /// 属性字符串。
        /// </summary>
        public string PropertiesString
        {
            get => _PropertiesString;
            set
            {
                RefreshProperties(value);
                _PropertiesString = value;
            }
        }

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

        /// <summary>
        /// 清除动态属性。
        /// </summary>
        protected void ClearProperties()
        {
            _NumberProperties = null;
            _SequenceProperties = null;
            _StringProperties = null;
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
    }
}
