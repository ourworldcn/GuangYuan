using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace OW.Game.Store
{
    public class VirtualThingExtraPropertiesBase
    {
        public VirtualThingExtraPropertiesBase()
        {

        }

        public Dictionary<string, string> DictionaryPrpperties { get; set; } = new Dictionary<string, string>();

    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class VirtualThingBase<T> : DbTreeNodeBase<T> where T : GuidKeyObjectBase
    {
        public VirtualThingBase()
        {
        }

        public VirtualThingBase(Guid id) : base(id)
        {
        }


    }

    /// <summary>
    /// 存储游戏世界树状事物的基本类。
    /// </summary>
    [Table("VirtualThings")]
    public class VirtualThing : VirtualThingBase<VirtualThing>
    {
        public VirtualThing()
        {
        }

        public VirtualThing(Guid id) : base(id)
        {
        }

        #region 析构及处置对象相关

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Timestamp = null;
                base.Dispose(disposing);
            }
        }
        #endregion 析构及处置对象相关

        private byte[] _Timestamp;

        /// <summary>
        /// 时间戳。
        /// </summary>
        [Timestamp]
        [JsonIgnore]
        public byte[] Timestamp { get => _Timestamp; set => _Timestamp = value; }

        /// <summary>
        /// 扩展的二进制大对象。
        /// </summary>
        private byte[] _BinaryArray;
        public byte[] BinaryArray
        {
            get { return _BinaryArray; }
            set { _BinaryArray = value; }
        }

    }
}
