using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OW.Game.Store
{
    /// <summary>
    /// 使用Json字符串存储动态对象的接口。
    /// </summary>
    public interface IJsonDynamicProperty
    {
        /// <summary>
        /// Json字符串。
        /// </summary>
        string JsonObjectString { get; set; }

        /// <summary>
        /// 存储最后一次获取对象的类型。
        /// </summary>
        [NotMapped]
        Type JsonObjectType { get; set; }

        /// <summary>
        /// Json字符串代表的对象。请调用<see cref="GetJsonObject{T}"/>生成该属性值。
        /// </summary>
        [NotMapped]
        abstract object JsonObject { get; set; }

        /// <summary>
        /// 将<see cref="JsonObjectString"/>解释为指定类型的对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        abstract T GetJsonObject<T>() where T : new();
        //{
        //    if (typeof(T) != JsonObjectType || JsonObject is null)
        //    {
        //        if (string.IsNullOrWhiteSpace(JsonObjectString))
        //        {
        //            JsonObject = new T();
        //        }
        //        else
        //        {
        //            JsonObject = JsonSerializer.Deserialize(JsonObjectString, typeof(T));
        //        }
        //        JsonObjectType = typeof(T);
        //    }
        //    return (T)JsonObject;
        //}
    }

    /// <summary>
    /// <see cref="JsonDynamicPropertyBase.JsonObject"/>属性的基类。
    /// </summary>
    public class JsonDynamicPropertyViewBase : IDisposable
    {
        #region 构造函数

        public JsonDynamicPropertyViewBase()
        {
        }

        public JsonDynamicPropertyViewBase(Dictionary<string, string> stringDictionary)
        {
            _StringDictionary = stringDictionary;
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


    }

    public class JsonDynamicPropertyBase : GuidKeyObjectBase, IDisposable, IBeforeSave, IJsonDynamicProperty
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public JsonDynamicPropertyBase()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id"></param>
        public JsonDynamicPropertyBase(Guid id) : base(id)
        {
        }

        #endregion 构造函数

        #region 数据库属性

        #region JsonObject相关

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
                    _JsonObject = null;
                    JsonObjectType = null;
                }
            }
        }

        /// <summary>
        /// 获取或初始化<see cref="JsonObject"/>属性并返回。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T GetJsonObject<T>() where T : new()
        {
            if (typeof(T) != JsonObjectType || JsonObject is null)  //若需要初始化
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
                if (JsonObject is INotifyPropertyChanged changed)
                    changed.PropertyChanged += Changed_PropertyChanged; ;
            }
            return (T)JsonObject;
        }

        private void Changed_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        private object _JsonObject;
        /// <summary>
        /// 用<see cref="GetJsonObject{T}"/>获取。
        /// </summary>
        [JsonIgnore, NotMapped]
        public object JsonObject
        {
            get => _JsonObject;
            set
            {
                _JsonObject = value;
            }
        }

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
                _JsonObject = null;
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

        #region IBeforeSave接口及相关

        public virtual void PrepareSaving(DbContext db)
        {
            if (_JsonObject != null)
                _JsonObjectString = JsonSerializer.Serialize(_JsonObject, JsonObjectType ?? JsonObject.GetType());
        }

        #endregion IBeforeSave接口及相关
    }
}