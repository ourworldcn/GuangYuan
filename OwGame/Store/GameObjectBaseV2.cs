using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OW.Game.Store
{
    public abstract class GameObjectBaseV2 : JsonDynamicPropertyBase
    {
        protected GameObjectBaseV2()
        {
        }

        protected GameObjectBaseV2(Guid id) : base(id)
        {
        }

        #region RuntimeProperties属性相关

        private ConcurrentDictionary<string, object> _RuntimeProperties;

        /// <summary>
        /// 存储一些运行时需要用的到的属性，使用者自己定义。
        /// 这些存储的属性不会被持久化。
        /// </summary>
        [NotMapped, JsonIgnore]
        public ConcurrentDictionary<string, object> RuntimeProperties
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.Synchronized)]
            get => _RuntimeProperties ??= new ConcurrentDictionary<string, object>();
        }

        /// <summary>
        /// 存储RuntimeProperties属性的后备字段是否已经初始化。
        /// </summary>
        [NotMapped, JsonIgnore]
        public bool IsCreatedOfRuntimeProperties => _RuntimeProperties != null;

        #endregion RuntimeProperties属性相关

        #region 扩展对象相关

        /// <summary>
        /// 二进制数据。
        /// </summary>
        public byte[] BinaryArray { get; set; }

        object _BinaryObject;
        /// <summary>
        /// <see cref="BinaryArray"/>中存储的对象视图。
        /// </summary>
        /// <remarks>在保存时会调用<see cref="BinaryObject"/>的GetType获取类型。</remarks>
        [NotMapped, JsonIgnore]
        public object BinaryObject
        {
            get
            {
                if (_BinaryObject is null && BinaryArray != null && BinaryArray.Length > 0)
                {
                    using MemoryStream ms = new MemoryStream(BinaryArray);
                    //using BrotliStream bs = new BrotliStream(ms, CompressionLevel.Fastest);
                    string fullName;
                    using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                        fullName = br.ReadString();
                    var type = Type.GetType(fullName);
                    _BinaryObject = JsonSerializer.DeserializeAsync(ms, type).Result;
                }
                return _BinaryObject;
            }
            set => _BinaryObject = value;
        }

        /// <summary>
        /// 获取BinaryObject中的对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetOrCreateBinaryObject<T>() where T : class, new()
        {
            if (BinaryObject is null)
                _BinaryObject = new T();
            return (T)_BinaryObject;
        }

        #endregion 扩展对象相关

        public override void PrepareSaving(DbContext db)
        {
            if (_BinaryObject != null)
            {
                var fullName = _BinaryObject.GetType().AssemblyQualifiedName;
                MemoryStream ms;
                using (ms = new MemoryStream())
                {
                    using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
                        bw.Write(fullName);
                    JsonSerializer.SerializeAsync(ms, _BinaryObject, _BinaryObject.GetType()).Wait();
                }
                BinaryArray = ms.ToArray();
            }
            base.PrepareSaving(db);
        }

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
                BinaryArray = null;
                _BinaryObject = null;
                base.Dispose(disposing);
            }
        }
    }
}
