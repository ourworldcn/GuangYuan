using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;

namespace OW.Game.Store
{
    public class VirtualThingEntityBase
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public VirtualThingEntityBase()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="thing"></param>
        public VirtualThingEntityBase(VirtualThing thing)
        {
            _Thing = thing;
        }

        #endregion 构造函数

        #region IDisposable接口及相关

        private volatile bool _IsDisposed;

        /// <summary>
        /// 对象是否已经被处置。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public bool IsDisposed
        {
            get => _IsDisposed;
            protected set => _IsDisposed = value;
        }

        /// <summary>
        /// 实际处置当前对象的方法。
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Thing = null;
                _StringDictionary = null;
                _IsDisposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~SimpleDynamicPropertyBase()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// 处置对象。
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable接口及相关

        private Dictionary<string, string> _StringDictionary;

        /// <summary>
        /// 记录一些扩展属性的字典。
        /// </summary>
        public Dictionary<string, string> StringDictionary { get => _StringDictionary ??= new Dictionary<string, string>(); set => _StringDictionary = value; }

        private VirtualThing _Thing;
        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public VirtualThing Thing { get => _Thing; set => _Thing = value; }

    }

    /// <summary>
    /// 标识通用的虚拟事物类所实现的接口。
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    public interface IVirtualThing<T> : IJsonDynamicProperty, IDbQuickFind, IDbTreeNode<T>, IEntityWithSingleKey<Guid> where T : IEntityWithSingleKey<Guid>
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class VirtualThingBase<T> : DbTreeNodeBase<T>, IVirtualThing<T> where T : GuidKeyObjectBase
    {
        #region 构造函数

        public VirtualThingBase()
        {
        }

        public VirtualThingBase(Guid id) : base(id)
        {
        }
        #endregion 构造函数

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
                _RuntimeProperties = null;
                _Base64IdString = null;
                _IdString = null;
                base.Dispose(disposing);
            }
        }
        #endregion 析构及处置对象相关

        #region RuntimeProperties属性相关

        private ConcurrentDictionary<string, object> _RuntimeProperties;

        /// <summary>
        /// 存储一些运行时需要用的到的属性，使用者自己定义。
        /// 这些存储的属性不会被持久化。
        /// </summary>
        [NotMapped, JsonIgnore]
        public ConcurrentDictionary<string, object> RuntimeProperties
        {
            get
            {
                if (_RuntimeProperties is null)
                    Interlocked.CompareExchange(ref _RuntimeProperties, new ConcurrentDictionary<string, object>(), null);
                return _RuntimeProperties;
            }
        }

        /// <summary>
        /// 存储RuntimeProperties属性的后备字段是否已经初始化。
        /// </summary>
        [NotMapped, JsonIgnore]
        public bool IsCreatedOfRuntimeProperties => _RuntimeProperties != null;

        #endregion RuntimeProperties属性相关

        #region 减低内存分配速率

        private string _IdString;

        /// <summary>
        /// 获取或设置Id的字符串表现形式。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public string IdString
        {
            get
            {
                return _IdString ??= Id.ToString();
            }
            set
            {
                Id = Guid.Parse(value);
                _IdString = null;
            }
        }

        private string _Base64IdString;

        /// <summary>
        /// 获取或设置Id的Base64字符串表现形式。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public string Base64IdString
        {
            get { return _Base64IdString ??= Id.ToBase64String(); }
            set
            {
                Id = OwConvert.ToGuid(value);
                _Base64IdString = value;
            }
        }

        #endregion 减低内存分配速率
    }

    /// <summary>
    /// 存储游戏世界事物的基本类。一般认为他们具有树状结构。
    /// </summary>
    [Table("VirtualThings")]
    public class VirtualThing : VirtualThingBase<VirtualThing>
    {
        #region 构造函数

        public VirtualThing()
        {
        }

        public VirtualThing(Guid id) : base(id)
        {
        }

        #endregion 构造函数

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
                _BinaryArray = null;
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

        private byte[] _BinaryArray;
        /// <summary>
        /// 扩展的二进制大对象。
        /// </summary>
        public byte[] BinaryArray
        {
            get { return _BinaryArray; }
            set { _BinaryArray = value; }

        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override T GetJsonObject<T>()
        {
            var result = base.GetJsonObject<T>();
            if (result is VirtualThingEntityBase viewBase)
                viewBase.Thing = this;
            return result;
        }
    }
}
