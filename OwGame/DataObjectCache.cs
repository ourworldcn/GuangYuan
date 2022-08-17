using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace OW.Game
{

    public interface IDataObjectCacheEntry : ICacheEntry
    {
        /// <summary>
        /// 保存数据对象的回调。
        /// </summary>
        public Action<object> SaveCallback { get; set; }

    }

    public interface IDataObjectCache : IMemoryCache
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        ICacheEntry IMemoryCache.CreateEntry(object key)
        {
            return CreateEntry(key);
        }

        new IDataObjectCacheEntry CreateEntry(object key);

        bool SetDirty(object key);

        void EnsureSaved(object key);

    }

    public class DataObjectCacheOptions : MemoryCacheOptions
    {
        public TimeSpan PeriodOfSave { get; set; }
    }

    /// <summary>
    /// 数据对象的缓存类。
    /// 数据对象的加载需要经过IO,且需要回存，并且其有唯一的键值。
    /// </summary>
    public class DataObjectCache : IDataObjectCache, IDisposable
    {

        public class DataObjectCacheEntry : IDataObjectCacheEntry
        {
            #region 构造函数

            /// <summary>
            /// 构造函数。
            /// </summary>
            /// <param name="key"></param>
            public DataObjectCacheEntry(object key, DataObjectCache cache)
            {
                Key = key;
                Cache = cache;
            }

            #endregion 构造函数

            #region IDataObjectCacheEntry接口相关

            #region ICacheEntry接口相关

            #region IDisposable接口相关

            public void Dispose()
            {
                lock (Key)
                {
                    //加入缓存条目
                    var entity = Cache._Datas.AddOrUpdate(Key, this, (key, val) => val);
                    entity.LastDateTimeUtc = DateTime.UtcNow;
                }
            }
            #endregion IDisposable接口相关

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            public object Key { get; private set; }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            public object Value { get; set; }

            public DateTimeOffset? AbsoluteExpiration { get; set; }

            public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

            public TimeSpan? SlidingExpiration { get; set; }

            public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();

            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();

            public CacheItemPriority Priority { get; set; }

            public long? Size { get; set; }

            #endregion ICacheEntry接口相关

            public Action<object> SaveCallback { get; set; }

            #endregion IDataObjectCacheEntry接口相关

            public DataObjectCache Cache { get; }

            /// <summary>
            /// 最后一次访问的时间点。
            /// </summary>
            public DateTime LastDateTimeUtc { get; internal set; } = DateTime.UtcNow;

            /// <summary>
            /// 驱逐的原因。
            /// </summary>
            public EvictionReason Reason { get; internal set; }

            /// <summary>
            /// 该项是否超期需要逐出。
            /// </summary>
            /// <param name="now"></param>
            /// <returns></returns>
            internal bool IsExpiration(DateTime now)
            {
                if (SlidingExpiration.HasValue && now - LastDateTimeUtc > SlidingExpiration)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// 逐出所有超期的项。
        /// </summary>
        /// <param name="percentage"></param>
        /// <returns></returns>
        protected virtual int Compact(double percentage)
        {
            int result = 0;
            DateTime now = DateTime.UtcNow;
            foreach (var item in _Datas)
            {
                var key = item.Key;
                if (!Monitor.TryEnter(key))    //若已无效
                    continue;
                try
                {
                    var entity = item.Value;
                    if (!entity.IsExpiration(now))  //若未到期
                        continue;
                    entity.SaveCallback?.Invoke(entity.Value);
                    entity.Reason = EvictionReason.Expired;
                    if (_Datas.Remove(key, out entity))
                    {
                        RemoveCore(entity);
                        result++;
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    Monitor.Exit(key);
                }
                if (result > 100)
                    break;
            }
            return result;
        }

        /// <summary>
        /// 保存挂起的更改。
        /// </summary>
        protected virtual void Save()
        {
            List<object> list = new List<object>();
            lock (_Dirty)
            {
                if (_Dirty.Count <= 0)
                    return;
                OwHelper.Copy(_Dirty, list);
                _Dirty.Clear();
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var key = list[i];
                if (!Monitor.TryEnter(key)) //若无法锁定键值
                    continue;
                try
                {
                    if (!_Datas.TryGetValue(key, out var entity))
                        continue;
                    entity.SaveCallback?.Invoke(entity.Value);
                }
                catch (Exception)
                { }
                finally
                {
                    Monitor.Exit(key);
                }
            }
            lock (_Dirty)
                list.ForEach(c => _Dirty.Add(c));   //将未保存对象键值放在队列中以待下次保存
        }
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DataObjectCache()
        {
            Initialize();
        }

        /// <summary>
        /// 内部初始化函数。
        /// </summary>
        void Initialize()
        {

        }
        #endregion 构造函数

        ConcurrentDictionary<object, DataObjectCacheEntry> _Datas = new ConcurrentDictionary<object, DataObjectCacheEntry>();

        /// <summary>
        /// 脏队列。
        /// </summary>
        HashSet<object> _Dirty = new HashSet<object>();

        #region IDataObjectCache接口相关

        public bool SetDirty(object key)
        {
            lock (_Dirty)
            {
                var result = _Dirty.Add(key);
                Monitor.Pulse(_Dirty);
                return result;
            }
        }

        public void EnsureSaved(object key)
        {
            //TODO 
            throw new NotImplementedException();
        }

        #region IMemoryCache接口相关

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IDataObjectCacheEntry CreateEntry(object key)
        {
            return new DataObjectCacheEntry(key, this);
        }

        /// <summary>
        /// <inheritdoc/>
        /// 不会保存数据。立即移除。
        /// </summary>
        /// <param name="key"></param>
        public void Remove(object key)
        {
            lock (key)
            {
                if (!_Datas.TryRemove(key, out var entity))
                    return;
                entity.Reason = EvictionReason.Removed;
                RemoveCore(entity);
            }
        }

        /// <summary>
        /// 仅调用所有回调，然后对值调用Dispose如果实现了IDisposable接口。
        /// </summary>
        /// <param name="key"></param>
        private void RemoveCore(DataObjectCacheEntry entity)
        {
            foreach (var item in entity.PostEvictionCallbacks.ToArray())
            {
                try
                {
                    item.EvictionCallback(entity.Key, entity.Value, entity.Reason, item.State);
                }
                catch (Exception)
                {
                }
            }
            (entity.Value as IDisposable)?.Dispose();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(object key, out object value)
        {
            if (!_Datas.TryGetValue(key, out var entity))
            {
                value = default;
                return false;
            }
            value = entity.Value;
            return true;
        }

        #region IDisposable相关

        private bool _DisposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!_DisposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _Datas = null;
                _Dirty = null;
                _DisposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~DataObjectCache()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable相关

        #endregion IMemoryCache接口相关

        #endregion IDataObjectCache接口相关


    }

    public class DataObjectManagerOptions : IOptions<DataObjectManagerOptions>
    {
        public DataObjectManagerOptions Value => this;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);
        public TimeSpan Scan { get; internal set; } = TimeSpan.FromMinutes(1);
    }

    public class DataObjectManager
    {
        public class DataObjectCacheEntry : IDataObjectCacheEntry
        {
            #region 构造函数

            /// <summary>
            /// 构造函数。
            /// </summary>
            /// <param name="key"></param>
            public DataObjectCacheEntry(object key, DataObjectCache cache)
            {
                Key = key;
                Cache = cache;
            }

            #endregion 构造函数

            #region IDataObjectCacheEntry接口相关

            #region ICacheEntry接口相关

            #region IDisposable接口相关

            public void Dispose()
            {
                lock (Key)
                {
                    //加入缓存条目
                    //var entity = Cache._Datas.AddOrUpdate(Key, this, (key, val) => val);
                    //entity.LastDateTimeUtc = DateTime.UtcNow;
                }
            }
            #endregion IDisposable接口相关

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            public object Key { get; internal set; }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            public object Value { get; set; }

            public DateTimeOffset? AbsoluteExpiration { get; set; }

            public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

            public TimeSpan? SlidingExpiration { get; set; }

            public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();

            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();

            public CacheItemPriority Priority { get; set; }

            public long? Size { get; set; }

            #endregion ICacheEntry接口相关

            public Action<object> SaveCallback { get; set; }

            #endregion IDataObjectCacheEntry接口相关

            public DataObjectCache Cache { get; }

            /// <summary>
            /// 最后一次访问的时间点。
            /// </summary>
            public DateTime LastDateTimeUtc { get; internal set; } = DateTime.UtcNow;

            /// <summary>
            /// 驱逐的原因。
            /// </summary>
            public EvictionReason Reason { get; internal set; }

            /// <summary>
            /// 默认的超时时间。
            /// </summary>
            public TimeSpan Timeout { get; internal set; } = TimeSpan.FromMinutes(1);

            /// <summary>
            /// 该项是否超期需要逐出。
            /// </summary>
            /// <param name="now"></param>
            /// <returns></returns>
            internal bool IsExpiration(DateTime now)
            {
                if (SlidingExpiration.HasValue && now - LastDateTimeUtc > SlidingExpiration)
                    return true;
                return false;
            }
        }

        public DataObjectManager()
        {
            Options = new DataObjectManagerOptions();
            Initialize();
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        void Initialize()
        {
            _Timer = new Timer(TimerCallback, null, Options.Scan, Options.Scan);
        }

        public void TimerCallback(object state)
        {
            Save();
        }

        Timer _Timer;

        public DataObjectManagerOptions Options { get; set; }

        ConcurrentDictionary<string, DataObjectCacheEntry> _Datas = new ConcurrentDictionary<string, DataObjectCacheEntry>();

        HashSet<string> _Dirty = new HashSet<string>();

        protected void Save()
        {
            List<string> keys;
            lock (_Dirty)
                keys = _Dirty.ToList();
            for (int i = keys.Count - 1; i >= 0; i--)
            {
                var key = keys[i];
                using (var dw = DisposeHelper.Create(StringLocker.TryEnter, StringLocker.Exit, key, TimeSpan.Zero))
                {
                    if (dw.IsEmpty)
                        continue;
                    if (!_Datas.TryGetValue(key, out var entity))
                        continue;
                    try
                    {
                        entity.SaveCallback?.Invoke(entity.Value);
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
        /// 获取或加载对象。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="loader"></param>
        /// <returns></returns>
        public object GetOrLoad(string key, Func<DataObjectCacheEntry, object> loader)
        {
            using var dw = DisposeHelper.Create(StringLocker.TryEnter, StringLocker.Exit, key, Options.Timeout);
            if (dw.IsEmpty)
                return null;
            if (_Datas.TryGetValue(key, out var entity))
            {
                entity.LastDateTimeUtc = DateTime.UtcNow;
                return entity.Value;
            }

            entity = new DataObjectCacheEntry(key, null) { Key = key };
            entity.Value = loader(entity);
            entity = _Datas.GetOrAdd(key, entity);
            entity.LastDateTimeUtc = DateTime.UtcNow;
            return entity.Value;
        }

        /// <summary>
        /// 在本地中获取对象，如果没有则返回null。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object Find(string key)
        {
            using var dw = DisposeHelper.Create(StringLocker.TryEnter, StringLocker.Exit, key, Options.Timeout);
            if (dw.IsEmpty)
                return null;
            if (_Datas.TryGetValue(key, out var entity))
                return entity.Value;
            return null;
        }

        /// <summary>
        /// 指出对象已经更改，需要保存。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool SetDirty(string key)
        {
            lock (_Dirty)
                return _Dirty.Add(key);
        }

        public bool SetTimout(string key, TimeSpan timeout)
        {
            using var dw = DisposeHelper.Create(StringLocker.TryEnter, StringLocker.Exit, key, Options.Timeout);
            if (dw.IsEmpty)
            {
                return false;
            }
            if (!_Datas.TryGetValue(key, out var entity))
            {
                return false;
            }
            entity.Timeout = timeout;
            return true;
        }

        /// <summary>
        /// 仅调用所有回调，然后对值调用Dispose如果实现了IDisposable接口。
        /// </summary>
        /// <param name="key"></param>
        private void RemoveCore(DataObjectCacheEntry entity)
        {
            foreach (var item in entity.PostEvictionCallbacks.ToArray())
            {
                try
                {
                    item.EvictionCallback(entity.Key, entity.Value, entity.Reason, item.State);
                }
                catch (Exception)
                {
                }
            }
            (entity.Value as IDisposable)?.Dispose();
        }

    }


}
