using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Extensions.Caching.Memory
{
    public class LeafMemoryCacheOptions : MemoryCacheOptions, IOptions<LeafMemoryCacheOptions>
    {
        public LeafMemoryCacheOptions()
        {

        }

        public Func<object, TimeSpan, bool> LockCallback { get; set; }

        public Action<object> UnlockCallback { get; set; }

        public TimeSpan DefaultTimeout { get; set; }

        public LeafMemoryCacheOptions Value => this;
    }

    public class LeafMemoryCache : IMemoryCache, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        private class LeafCacheEntry : ICacheEntry
        {
            public LeafCacheEntry(LeafMemoryCache cache)
            {
                Cache = cache;
            }

            public LeafMemoryCache Cache { get; set; }

            #region ICacheEntry接口相关

            object _Key;

            public object Key { get => _Key; set => _Key = value; }

            public object Value { get; set; }

            public DateTimeOffset? AbsoluteExpiration { get; set; }

            public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

            public TimeSpan? SlidingExpiration { get; set; }

            public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();

            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();

            public CacheItemPriority Priority { get; set; }

            public long? Size { get; set; }

            public void Dispose()
            {
            }
            #endregion ICacheEntry接口相关

        }

        #region 构造函数相关

        public LeafMemoryCache(IOptions<LeafMemoryCacheOptions> options)
        {
            _Options = options.Value;
        }

        #endregion 构造函数相关

        LeafMemoryCacheOptions _Options;
        public LeafMemoryCacheOptions Options => _Options;

        ConcurrentDictionary<object, LeafCacheEntry> _Datas = new ConcurrentDictionary<object, LeafCacheEntry>();

        #region IMemoryCache接口相关

        #region IDisposable接口相关

        private bool _Disposed;

        protected bool Disposed { get => _Disposed; }

        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _Disposed = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~LeafMemoryCache()
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

        #endregion IDisposable接口相关

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="TimeoutException">锁定键超时 -或- 出现异常。</exception>
        public ICacheEntry CreateEntry(object key)
        {
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Options.DefaultTimeout);
            if (dw.IsEmpty)
                throw new TimeoutException();
            return new LeafCacheEntry(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="TimeoutException">锁定键超时 -或- 出现异常。</exception>
        public void Remove(object key)
        {
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Options.DefaultTimeout);
            if (dw.IsEmpty)
                throw new TimeoutException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="TimeoutException">锁定键超时 -或- 出现异常。</exception>
        public bool TryGetValue(object key, out object value)
        {
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Options.DefaultTimeout);
            if (dw.IsEmpty)
                throw new TimeoutException();
            if (!_Datas.TryGetValue(key, out var entity))
            {
                value = default;
                return false;
            }
            value = entity.Value;
            return true;
        }

        #endregion IMemoryCache接口相关
    }
}