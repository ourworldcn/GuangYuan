using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Extensions.Caching.Memory
{
    public class LeafMemoryCacheOptions : MemoryCacheOptions, IOptions<LeafMemoryCacheOptions>
    {
        public LeafMemoryCacheOptions()
        {
        }

        /// <summary>
        /// 设置或获取锁定键的回调。应支持递归与<see cref="UnlockCallback"/>配对使用。
        /// 默认值是<see cref="Monitor.TryEnter(object, TimeSpan)"/>。
        /// </summary>
        public Func<object, TimeSpan, bool> LockCallback { get; set; } = Monitor.TryEnter;

        /// <summary>
        /// 设置或获取释放键的回调。应支持递归与<see cref="LockCallback"/>配对使用。
        /// 默认值是<see cref="Monitor.Exit(object)"/>。
        /// </summary>
        public Action<object> UnlockCallback { get; set; } = Monitor.Exit;

        /// <summary>
        /// 确定当前线程是否保留指定键上的锁。
        /// 默认值是<see cref="Monitor.IsEntered(object)"/>
        /// </summary>
        public Func<object, bool> IsEnteredCallback { get; set; } = Monitor.IsEntered;

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// 
        /// </summary>
        public LeafMemoryCacheOptions Value => this;
    }

    /// <summary>
    /// 
    /// </summary>
    public class LeafMemoryCache : IMemoryCache, IDisposable
    {
        public class LeafCacheEntry : ICacheEntry
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

            bool _IsDisposed;

            public void Dispose()
            {
                using var dw = DisposeHelper.Create(Cache.Options.LockCallback, Cache.Options.UnlockCallback, Key, Cache.Options.DefaultTimeout);
                if (dw.IsEmpty)
                    throw new TimeoutException();
                if (!_IsDisposed)
                {
                    var factEntity = Cache._Datas.AddOrUpdate(Key, this, (key, ov) => this);
                    factEntity.LastUseUtc = DateTime.UtcNow;
                    _IsDisposed = true;
                }
            }
            #endregion ICacheEntry接口相关

            public DateTime LastUseUtc { get; internal set; } = DateTime.UtcNow;

            public bool CheckExpired(DateTime utcNow)
            {
                if (SlidingExpiration.HasValue && utcNow - LastUseUtc >= SlidingExpiration)
                    return true;
                if (AbsoluteExpiration.HasValue && utcNow >= AbsoluteExpiration)
                    return true;
                return false;
            }

            /// <summary>
            /// 获取或设置用户的附加配置数据。
            /// </summary>
            public object State { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (_Disposed)
                Throw();

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DoesNotReturn]
        static void Throw() => throw new ObjectDisposedException(typeof(LeafMemoryCache).FullName);

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
        public virtual ICacheEntry CreateEntry(object key)
        {
            ThrowIfDisposed();
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Options.DefaultTimeout);
            if (dw.IsEmpty)
                throw new TimeoutException();
            return new LeafCacheEntry(this);
        }

        public LeafCacheEntry CreateLeafCacheEntry(object key)
        {
            return (LeafCacheEntry)CreateEntry(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="TimeoutException">锁定键超时 -或- 出现异常。</exception>
        public virtual void Remove(object key)
        {
            ThrowIfDisposed();
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Options.DefaultTimeout);
            if (dw.IsEmpty)
                throw new TimeoutException();
            if (!_Datas.TryRemove(key, out var entity))
                return;
            entity.PostEvictionCallbacks.SafeForEach(c => c.EvictionCallback?.Invoke(key, entity.Value, EvictionReason.Removed, c.State));
            _Datas.Remove(key, out _);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="TimeoutException">锁定键超时 -或- 出现异常。</exception>
        public virtual bool TryGetValue(object key, out object value)
        {
            ThrowIfDisposed();
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Options.DefaultTimeout);
            if (dw.IsEmpty)
                throw new TimeoutException();
            if (!_Datas.TryGetValue(key, out var entity))
            {
                value = default;
                return false;
            }
            value = entity.Value;
            entity.LastUseUtc = DateTime.UtcNow;
            return true;
        }

        #endregion IMemoryCache接口相关

        [Conditional("DEBUG")]
        private void ThrowIfNotEntered(object key)
        {
            if (!_Options.IsEnteredCallback(key))
                throw new LockRecursionException();
        }

        /// <summary>
        /// 获取指定键的设置数据。没有找到则返回null。必须锁定键以后再调用此函数，否则可能出现争用。
        /// 可以更改返回值内部的内容，在解锁键之前不会生效。
        /// 这个函数不触发计时。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LeafCacheEntry GetCacheEntry(object key)
        {
            ThrowIfDisposed();
            ThrowIfNotEntered(key);
            return _Datas.TryGetValue(key, out var result) ? result : default;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="percentage"></param>
        public void Compact()
        {
            Compact((long)(_Datas.Count * _Options.CompactionPercentage));
        }

        protected virtual void Compact(long removalSizeTarget)
        {
            ThrowIfDisposed();
            var nowUtc = DateTime.UtcNow;
            foreach (var item in _Datas)
            {
                using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, item.Key, TimeSpan.Zero);
                if (dw.IsEmpty)
                    continue;
                if (!item.Value.CheckExpired(nowUtc))
                    continue;
                _Datas.TryRemove(item.Key, out var entity);
                try
                {
                    entity.PostEvictionCallbacks.SafeForEach(c => c.EvictionCallback?.Invoke(entity.Key, entity.Value, EvictionReason.Expired, c.State));
                }
                catch (Exception)
                {
                }
            }
        }

    }
}