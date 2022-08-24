using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

namespace OW.Game
{

    public class DataObjectCacheOptions : MemoryCacheBaseOptions, IOptions<DataObjectCacheOptions>
    {
        /// <summary>
        /// 扫描间隔。
        /// </summary>
        /// <value>默认值:1分钟。</value>
        public TimeSpan ScanFrequency { get; internal set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 默认的缓存超时。
        /// </summary>
        /// <value>默认值:1分钟。</value>
        public TimeSpan DefaultCachingTimeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 创建数据库上下文的回调。
        /// </summary>
        public Func<object, DbContext> CreatDbContextCallback { get; set; }

        public new DataObjectCacheOptions Value => this;
    }

    /// <summary>
    /// 数据对象的缓存类。
    /// 数据对象的加载需要经过IO,且需要保存，并且其有唯一的键值。
    /// </summary>
    public class DataObjectCache : MemoryCacheBase, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public class DataObjectCacheEntry : MemoryCacheBaseEntry, IDisposable
        {
            #region 构造函数

            /// <summary>
            /// 构造函数。
            /// </summary>
            /// <param name="key"></param>
            public DataObjectCacheEntry(object key, DataObjectCache cache) : base(key, cache)
            {
            }

            #endregion 构造函数

            #region IDataObjectCacheEntry接口相关

            #region ICacheEntry接口相关

            #region IDisposable接口相关

            public override void Dispose()
            {
                base.Dispose();
            }

            #endregion IDisposable接口相关


            #endregion ICacheEntry接口相关

            #endregion IDataObjectCacheEntry接口相关

            /// <summary>
            /// 加载时调用。
            /// 在对键加锁的范围内调用。
            /// </summary>
            [AllowNull]
            public Func<object, object, object> LoadCallback { get; set; }

            /// <summary>
            /// <see cref="LoadCallback"/>的用户参数。
            /// </summary>
            [AllowNull]
            public object LoadCallbackState { get; set; }

            /// <summary>
            /// 创建对象时调用。
            /// 在对键加锁的范围内调用。
            /// </summary>
            [AllowNull]
            public Func<object, object, object> CreateCallback { get; set; }

            /// <summary>
            /// <see cref="CreateCallback"/>的用户参数
            /// </summary>
            [AllowNull]
            public object CreateCallbackState { get; set; }

            /// <summary>
            /// 需要保存时调用。
            /// 在对键加锁的范围内调用。
            /// 回调参数是要保存的对象，附加数据，返回true表示成功，否则是没有保存成功,若没有设置该回调，则说民无需保存，也就视同保存成功。
            /// </summary>
            [AllowNull]
            public Func<object, object, bool> SaveCallback { get; set; }

            /// <summary>
            /// <see cref="SaveCallback"/>的用户参数。
            /// </summary>
            [AllowNull]
            public object SaveCallbackState { get; set; }

            /// <summary>
            /// 是否已经初始化了<see cref="MemoryCacheBase.MemoryCacheBaseEntry.Value"/>的值。
            /// </summary>
            internal bool _IsInitialized;

            /// <summary>
            /// 是否已经初始化了<see cref="MemoryCacheBase.MemoryCacheBaseEntry.Value"/>的值。
            /// </summary>
            public bool IsInitialized => _IsInitialized;
        }

        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DataObjectCache(IOptions<DataObjectCacheOptions> options) : base(options)
        {
            Initialize();
        }

        /// <summary>
        /// 内部初始化函数。
        /// </summary>
        void Initialize()
        {
            _Timer = new Timer(TimerCallback, null, ((DataObjectCacheOptions)Options).ScanFrequency, ((DataObjectCacheOptions)Options).ScanFrequency);
        }

        #endregion 构造函数

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        public void TimerCallback(object state)
        {
            using var dw = DisposeHelper.Create(c => Monitor.TryEnter(c, 0), _Timer);   //防止重入
            if (dw.IsEmpty)  //若还在重入中
                return;
            Compact();
            Save();
        }

        /// <summary>
        /// 
        /// </summary>
        Timer _Timer;

        /// <summary>
        /// 对标记为脏的数据进行保存。
        /// </summary>
        protected void Save()
        {
            List<object> keys = new List<object>();
            lock (_Dirty)
            {
                OwHelper.Copy(_Dirty, keys);
                _Dirty.Clear();
            }
            for (int i = keys.Count - 1; i >= 0; i--)
            {
                var key = keys[i];
                using (var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, TimeSpan.Zero))
                {
                    if (dw.IsEmpty)
                        continue;
                    var entry = GetCacheEntry(key);
                    if (entry is null)  //若键下的数据已经销毁
                    {
                        keys.RemoveAt(i);
                        continue;
                    }
                    try
                    {
                        var option = (DataObjectOptions)entry.State;
                        if (null != option.SaveCallback && !(bool)option.SaveCallback?.Invoke(entry.Value, option.SaveCallbackState))
                            continue;
                        keys.RemoveAt(i);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            //放入下次再保存
            if (keys.Count > 0)
                lock (_Dirty)
                    OwHelper.Copy(keys, _Dirty);
        }

        /// <summary>
        /// 脏队列。
        /// </summary>
        HashSet<object> _Dirty = new HashSet<object>();

        #region IDataObjectCache接口相关

        public bool SetDirty(object key)
        {
            bool result;
            lock (_Dirty)
            {
                result = _Dirty.Add(key);
                Monitor.Pulse(_Dirty);
            }
            return result;
        }

        public void EnsureSaved(object key)
        {
            //TODO 
            throw new NotImplementedException();
        }

        /// <summary>
        /// 确保初始化了缓存项的加载。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <param name="timeout">锁定超时。省略或为null则使用<see cref="MemoryCacheBaseOptions.DefaultTimeout"/>。</param>
        /// <returns>0=成功，1=超时无法锁定键，2=没有找到指定键。</returns>
        public int EnsureInitialized(object key, out DataObjectCacheEntry result, TimeSpan? timeout = null)
        {
            ThrowIfDisposed();
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, timeout ?? Options.DefaultTimeout);
            if (dw.IsEmpty)
            {
                result = default;
                return 1;
            }
            var entry = (DataObjectCacheEntry)GetCacheEntry(key);
            if (entry is null)
            {
                result = default;
                return 2;
            }
            if (!entry._IsInitialized)   //若尚未初始化
            {
                bool hasError = false;
                if (entry.LoadCallback != null)    //若有加载器
                {
                    try
                    {
                        entry.Value = entry.LoadCallback(entry.Key, entry.LoadCallbackState);
                    }
                    catch (Exception)
                    {
                        hasError = true;
                    }
                }
                if ((hasError || entry.LoadCallback is null) && entry.CreateCallback != null)   //若加载器没有或未生效且有初始化器
                {
                    try
                    {
                        entry.Value = entry.CreateCallback(entry.Key, entry.CreateCallbackState);
                    }
                    catch (Exception err)
                    {
                        throw new InvalidOperationException($"无法初始化键：{entry.Key} 的缓存项。", err);
                    }
                }
                //若没有加载器也没有初始化器，则视同已经初始化
                entry._IsInitialized = true;
            }
            result = entry;
            return 0;
        }

        #region 重载基类函数

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="key"><inheritdoc/></param>
        /// <param name="value"><inheritdoc/></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">对象已处置。</exception>
        protected override bool TryGetValueCore(object key, out object value)
        {
            if (0 != EnsureInitialized(key, out _))
            {
                value = default;
                return false;
            }
            return base.TryGetValueCore(key, out value);
        }

        protected override MemoryCacheBaseEntry CreateEntryCore(object key)
        {
            return new DataObjectCacheEntry(key, this);
        }

        #region 重载基类函数

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    _Timer?.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _Dirty = null;
                base.Dispose(disposing);
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~DataObjectCache()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        #endregion IDisposable相关

        #endregion IMemoryCache接口相关

        #endregion IDataObjectCache接口相关


    }


}
