using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OW.Game.Store
{
    public class JsonDynamicPropertyBase : GuidKeyObjectBase, IDisposable, IBeforeSave
    {
        public JsonDynamicPropertyBase()
        {
        }

        public JsonDynamicPropertyBase(Guid id) : base(id)
        {
        }

        #region 数据库属性

        private string _JsonObjectString;
        /// <summary>
        /// 属性字符串。格式数Json字符串。
        /// </summary>
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

        #region JsonObject相关

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

        #endregion JsonObject相关

        #endregion 数据库属性

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
                JsonObjectType = null;
                JsonObject = null;
                _JsonObjectString = null;
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


        public virtual void PrepareSaving(DbContext db)
        {
            if (JsonObject != null)
                JsonObjectString = JsonSerializer.Serialize(JsonObject, JsonObjectType ?? JsonObject.GetType());
        }

    }
}