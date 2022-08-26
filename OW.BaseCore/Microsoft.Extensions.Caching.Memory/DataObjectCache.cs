﻿using Microsoft.Extensions.Logging;
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
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Memory
{

    public class DataObjectCacheOptions : MemoryCacheBaseOptions, IOptions<DataObjectCacheOptions>
    {
        public DataObjectCacheOptions() : base()
        {
        }

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
                SlidingExpiration = TimeSpan.FromMinutes(1);
            }

            #endregion 构造函数

            #region IDataObjectCacheEntry接口相关

            /// <summary>
            /// 加载时调用。
            /// 在对键加锁的范围内调用。
            /// key,state,返回值。
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
            /// key,state,返回值。
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
            /// object,state,返回值。
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
            #region ICacheEntry接口相关

            #region IDisposable接口相关

            public override void Dispose()
            {
                base.Dispose();
                Task.Run(() => ((DataObjectCache)Cache).EnsureInitialized(Key, out _)); //异步初始化
            }

            #endregion IDisposable接口相关


            #endregion ICacheEntry接口相关

            #endregion IDataObjectCacheEntry接口相关

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
            _Timer = new Timer(TimerCallback, null, ((DataObjectCacheOptions)Options).ExpirationScanFrequency, ((DataObjectCacheOptions)Options).ExpirationScanFrequency);
        }

        #endregion 构造函数

        #region 定时任务相关

        /// <summary>
        /// 
        /// </summary>
        Timer _Timer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private void TimerCallback(object state)
        {
            using var dw = DisposeHelper.Create(c => Monitor.TryEnter(c, 0), _Timer);   //防止重入
            if (dw.IsEmpty)  //若还在重入中
                return;
            Save();
            Compact();
        }

        /// <summary>
        /// 对标记为脏的数据进行保存。
        /// 未能锁定或保存的数据都会再次放到队列中，等待下次保存。
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
                if (EnsureSaved(key, TimeSpan.Zero) || OwHelper.GetLastError() == 1168)    //若保存成功或已无需保存
                    keys.RemoveAt(i);
            }
            //放入下次再保存
            if (keys.Count > 0)
                lock (_Dirty)
                    OwHelper.Copy(keys, _Dirty);
        }

        #endregion 定时任务相关

        #region IDataObjectCache接口相关

        /// <summary>
        /// 脏队列。
        /// </summary>
        HashSet<object> _Dirty = new HashSet<object>();

        /// <summary>
        /// 设置一个键关联的数据对象需要保存。
        /// 该函数仅在一个集合中标记需要保存的对象的键，所以无需考虑锁定问题。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">对象已处置。</exception>
        public bool SetDirty(object key)
        {
            ThrowIfDisposed();
            bool result;
            lock (_Dirty)
            {
                result = _Dirty.Add(key);
                Monitor.Pulse(_Dirty);
            }
            return result;
        }

        /// <summary>
        /// 无论缓存是否需要都强制同步的确保指定键的关联对象被保存。
        /// 此函数会首先试图对键加锁，成功后才会进行实质工作，并解锁。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="timeout">锁定超时。省略或为null则使用<see cref="MemoryCacheBaseOptions.DefaultLockTimeout"/>。</param>
        /// <returns>true成功保存，false保存时出错。
        /// 调用<see cref="OwHelper.GetLastError"/>可获取详细信息。258=锁定超时，698=键已存在，1168=键不存在。
        /// </returns>
        /// <exception cref="ObjectDisposedException">对象已处置。</exception>
        public bool EnsureSaved(object key, TimeSpan? timeout = null)
        {
            ThrowIfDisposed();
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Options.DefaultLockTimeout);
            if (dw.IsEmpty)
            {
                OwHelper.SetLastError(258);
                return false;
            }
            var entry = GetCacheEntry(key) as DataObjectCacheEntry;
            if (entry is null)
            {
                OwHelper.SetLastError(1168);
                return false;
            }
            var result = EnsureSavedCore(entry);
            if (result)
            {
                OwHelper.SetLastError(0);
                lock (_Dirty)
                    _Dirty.Remove(entry.Key);
            }
            return result;
        }

        /// <summary>
        /// 实际确保缓存项保存的函数。
        /// 派生类可以重载此函数。非公有函数不会自动对键加锁，若需要调用者需要负责加/解锁。
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>true成功保存，false保存时出错。</returns>
        protected virtual bool EnsureSavedCore(DataObjectCacheEntry entry)
        {
            try
            {
                entry.SaveCallback?.Invoke(entry.Value, entry.SaveCallbackState);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 确保初始化了缓存项的加载。
        /// 此函数会首先试图对键加锁，成功后才会进行实质工作，并解锁。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <param name="timeout">锁定超时。省略或为null则使用<see cref="MemoryCacheBaseOptions.DefaultLockTimeout"/>。</param>
        /// <returns>true=成功，false=超时无法锁定键 - 或 - 键不存在。
        /// 调用<see cref="OwHelper.GetLastError"/>可获取详细信息。258=锁定超时，698=键已存在，1168=键不存在。
        /// </returns>
        public bool EnsureInitialized(object key, out DataObjectCacheEntry result, TimeSpan? timeout = null)
        {
            ThrowIfDisposed();
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, timeout ?? Options.DefaultLockTimeout);
            if (dw.IsEmpty)
            {
                result = default;
                OwHelper.SetLastError(258);
                return false;
            }
            var entry = (DataObjectCacheEntry)GetCacheEntry(key);
            if (entry is null)
            {
                result = default;
                OwHelper.SetLastError(1168);
                return false;
            }
            if (!entry.IsInitialized)
                EnsureInitializedCore(entry, timeout ?? Options.DefaultLockTimeout);
            result = entry;
            OwHelper.SetLastError(0);
            return true;
        }

        /// <summary>
        /// 确保初始化了缓存项的加载。
        /// 派生类可以重载此函数。非公有函数不会自动对键加锁，若需要调用者需要负责加/解锁。
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="timeout">工作超时。实施者自行定义超时后的行为，此实现忽略该参数。</param>
        /// <returns>true=成功初始化，false=已经初始化。
        /// 调用<see cref="OwHelper.GetLastError"/>可获取详细信息。
        /// </returns>
        /// <exception cref="InvalidOperationException">所有初始化手段均失败。</exception>
        protected virtual bool EnsureInitializedCore(DataObjectCacheEntry entry, TimeSpan timeout)
        {
            bool result;
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
                if ((hasError || entry.LoadCallback is null || entry.Value is null) && entry.CreateCallback != null)   //若加载器没有或未生效且有初始化器
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
                result = true;
            }
            else
                result = false;
            return result;
        }

        #region 重载基类函数

        protected override MemoryCacheBaseEntry CreateEntryCore(object key)
        {
            return new DataObjectCacheEntry(key, this);
        }

        /// <summary>
        /// 派生类可以重载此函数。非公有函数不会自动对键加锁，若需要调用者需要负责加/解锁。
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>该实现会确保初始化成功完成<seealso cref="EnsureInitializedCore(DataObjectCacheEntry, TimeSpan)"/>，然后调用基类实现--<inheritdoc/>。</returns>
        /// <exception cref="ObjectDisposedException">对象已处置。</exception>
        protected override bool TryGetValueCore(MemoryCacheBaseEntry entry)
        {
            EnsureInitializedCore((DataObjectCacheEntry)entry, Options.DefaultLockTimeout);
            return base.TryGetValueCore(entry);
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

    public static class DataObjectCacheExtensions
    {

    }
}
