﻿using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OW.Game.Store
{
    /// <summary>
    /// 玩家数据对象的基类。
    /// </summary>
    public abstract class GameObjectBase : SimpleDynamicPropertyBase, IDisposable, INotifyDynamicPropertyChanged
    {

        #region 构造函数

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public GameObjectBase()
        {

        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public GameObjectBase(Guid id) : base(id)
        {

        }

        #endregion 构造函数

        #region Json通用扩展属性相关

        private string _JsonObjectString;
        /// <summary>
        /// 属性字符串。格式数Json字符串。
        /// </summary>
        [NotMapped,JsonIgnore]
        public string JsonObjectString
        {
            get => _JsonObjectString;
            set
            {
                if (!ReferenceEquals(_JsonObjectString, value))
                {
                    _JsonObjectString = value;
                }
            }
        }

        /// <summary>
        /// 获取或初始化<see cref="JsonObject"/>属性并返回。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetJsonObject<T>() where T : new()
        {
            if (typeof(T) != JsonObjectType || JsonObject is null)
            {
                if (string.IsNullOrWhiteSpace(JsonObjectString))
                {
                    JsonObject = new T();
                }
                else
                {
                    JsonObject = JsonSerializer.Deserialize(JsonObjectString, typeof(T));
                }
                JsonObjectType = typeof(T);
            }
            return (T)JsonObject;
        }

        /// <summary>
        /// 用<see cref="GetJsonObject{T}"/>获取。
        /// </summary>
        [JsonIgnore, NotMapped]
        public object JsonObject { get; set; }

        [JsonIgnore, NotMapped]
        public Type JsonObjectType { get; set; }

        #endregion Json通用扩展属性相关

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

        #region IDisposable接口相关

        /// <summary>
        /// <inheritdoc/>
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
                _Base64IdString = null;
                _IdString = null;
                _RuntimeProperties = null;
                base.Dispose(disposing);
            }
        }

        #endregion IDisposable接口相关

        #region 事件及相关

        #endregion 事件及相关
    }
}
