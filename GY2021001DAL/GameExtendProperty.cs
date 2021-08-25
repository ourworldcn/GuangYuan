
using OW.Game.Store;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 服务器内部使用的通用扩展属性。
    /// <see cref="ParentId"/> 和 <see cref="Name"/> 组成联合主键。
    /// </summary>
    public class GameExtendProperty : GameObjectBase
    {
        /// <summary>
        /// 客户端属性使用的键名。
        /// <see cref="Name"/>是该值的，表示由客户端使用，服务器不会使用该对象。
        /// </summary>
        public const string ClientPropertyName = "d51b3d58-2dec-4d24-b85d-a57aafe10dd7";

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public GameExtendProperty()
        {

        }
        
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="name"></param>
        public GameExtendProperty(string name)
        {
            Name = name;
        }

        private string _Name;
        /// <summary>
        /// 属性的名称。
        /// </summary>
        [MaxLength(64)]
        public string Name
        {
            get
            {
                return _Name;
            }

            set
            {
                if (value.Length > 64)
                    throw new ArgumentException("最长仅能支持64个字符。", nameof(value));
                _Name = value;
            }
        }

        private string _StringValue;

        /// <summary>
        /// 短文本属性，可以索引加速查找。
        /// </summary>
        [MaxLength(256)]
        public string StringValue
        {
            get => _StringValue;
            set
            {
                if (value.Length > 256)
                    throw new ArgumentException("最长仅能支持256个字符。", nameof(value));
                _StringValue = value;
            }
        }

        public int IntValue { get; set; }

        public decimal DecimalValue { get; set; }

        public double DoubleValue { get; set; }

        /// <summary>
        /// 长文本属性，无法索引。
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 日期属性。
        /// </summary>
        /// <value>默认值是创建此对象是的UTC时间。</value>
        public DateTime DateTimeValue { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 一个<see cref="Guid"/>值。
        /// </summary>
        public Guid? GuidValue { get; set; }
    }

}