using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Extensions.Caching.Memory
{
    public class MemoryCacheBaseOptions : MemoryCacheOptions, IOptions<MemoryCacheBaseOptions>
    {
        /// <summary>
        /// 构造函数。
        /// 设置<see cref="MemoryCacheOptions.ExpirationScanFrequency"/>为1分钟。
        /// </summary>
        public MemoryCacheBaseOptions() : base()
        {
            ExpirationScanFrequency = TimeSpan.FromMinutes(1);
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
        /// 默认的锁定超时时间。
        /// </summary>
        /// <value>默认值:3秒。</value>
        public TimeSpan DefaultLockTimeout { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// 
        /// </summary>
        public MemoryCacheBaseOptions Value => this;
    }

    /// <summary>
    /// 内存缓存的基础类。
    /// </summary>
    public abstract class MemoryCacheBase : IMemoryCache, IDisposable
    {
        public class MemoryCacheBaseEntry : ICacheEntry
        {
            /// <summary>
            /// 构造函数。
            /// </summary>
            /// <param name="cache">指定所属缓存对象，在调用<see cref="Dispose"/>时可以加入该对象。</param>
            public MemoryCacheBaseEntry(object key, MemoryCacheBase cache)
            {
                Key = key;
                Cache = cache;
            }

            /// <summary>
            /// 所属的缓存对象。
            /// </summary>
            public MemoryCacheBase Cache { get; set; }

            #region ICacheEntry接口相关

            object _Key;

            public object Key { get => _Key; set => _Key = value; }

            public object Value { get; set; }

            public DateTimeOffset? AbsoluteExpiration { get; set; }

            public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

            public TimeSpan? SlidingExpiration { get; set; }

            public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();

            /// <summary>
            /// 所有的函数调用完毕才会解锁键对象。
            /// </summary>
            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();

            public CacheItemPriority Priority { get; set; }

            public long? Size { get; set; }

            #region IDisposable接口相关

            bool _IsDisposed;
            /// <summary>
            /// 对象是否已经被处置，此类型特殊，被处置意味着已经加入到缓存配置表中，而非真的被处置。
            /// </summary>
            protected bool IsDisposed => _IsDisposed;

            /// <summary>
            /// 使此配置项加入或替换缓存对象。内部会试图锁定键。
            /// </summary>
            /// <exception cref="TimeoutException">试图锁定键超时。</exception>
            public virtual void Dispose()
            {
                using var dw = DisposeHelper.Create(Cache.Options.LockCallback, Cache.Options.UnlockCallback, Key, Cache.Options.DefaultLockTimeout);
                if (dw.IsEmpty)
                    throw new TimeoutException();
                if (!_IsDisposed)
                {
                    var factEntity = Cache._Datas.AddOrUpdate(Key, this, (key, ov) => this);
                    factEntity.LastUseUtc = DateTime.UtcNow;
                    _IsDisposed = true;
                }
            }
            #endregion IDisposable接口相关

            #endregion ICacheEntry接口相关

            /// <summary>
            /// 最后一次使用的Utc时间。
            /// </summary>
            public DateTime LastUseUtc { get; internal set; } = DateTime.UtcNow;

            /// <summary>
            /// 获取此配置项是否超期。
            /// </summary>
            /// <param name="utcNow"></param>
            /// <returns></returns>
            public virtual bool IsExpired(DateTime utcNow)
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

            /// <summary>
            /// 是否在驱逐之后自动调用<see cref="IDisposable.Dispose"/>(如果支持该接口)。
            /// </summary>
            /// <value>true=如果可以则调用，false=不调用，这是默认值。</value>
            public bool AutoDispose { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        #region 构造函数相关

        public MemoryCacheBase(IOptions<MemoryCacheBaseOptions> options)
        {
            _Options = options.Value;
        }

        #endregion 构造函数相关

        MemoryCacheBaseOptions _Options;
        public MemoryCacheBaseOptions Options => _Options;

        ConcurrentDictionary<object, MemoryCacheBaseEntry> _Datas = new ConcurrentDictionary<object, MemoryCacheBaseEntry>();

        #region IMemoryCache接口相关

        /// <summary>
        /// 创建一个键。此函数不考虑锁定键的问题，若需要，调用者自行锁定。
        /// </summary>
        /// <param name="key"></param>
        /// <returns>返回的是<see cref="MemoryCacheBaseEntry"/>对象。</returns>
        /// <exception cref="ObjectDisposedException">对象已处置。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ICacheEntry CreateEntry(object key)
        {
            ThrowIfDisposed();
            return CreateEntryCore(key);
        }

        /// <summary>
        /// <see cref="IMemoryCache.CreateEntry(object)"/>实际调用此函数实现，派生类可需要实现此函数。
        /// 此函数不考虑锁定键的问题，若需要，实现者或调用者自行负责。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected abstract MemoryCacheBaseEntry CreateEntryCore(object key);

        public MemoryCacheBaseEntry CreateLeafCacheEntry(object key)
        {
            return (MemoryCacheBaseEntry)CreateEntry(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="TimeoutException">锁定键超时 -或- 出现异常。</exception>
        /// <exception cref="ObjectDisposedException">对象已处置。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(object key)
        {
            ThrowIfDisposed();
            if (_Datas.TryGetValue(key, out var entry)) //TODO 找不到的情况未处理
                RemoveCore(entry, EvictionReason.Removed);
        }

        /// <summary>
        /// 以指定原因移除缓存项。此函数会对键进行加锁，然后调用所有超期回调，最后移除配置项。
        /// 最后如果要求自动调用Dispose,且支持<see cref="IDisposable"/>接口则调用<see cref="IDisposable.Dispose"/>。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="reason"></param>
        /// <returns>0=成功移除，1=锁定键超时，2=没有找到指定键。</returns>
        protected virtual bool RemoveCore(MemoryCacheBaseEntry entry, EvictionReason reason)
        {
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, entry.Key, Options.DefaultLockTimeout);
            if (dw.IsEmpty)
            {
                OwHelper.SetLastError(258);
                return false;
            }
            try
            {
                entry.PostEvictionCallbacks.SafeForEach(c => c.EvictionCallback?.Invoke(entry.Key, entry.Value, EvictionReason.Removed, c.State));
                _Datas.TryRemove(entry.Key, out _);
            }
            catch (Exception)
            {
                return false;
            }
            if (entry.AutoDispose)
                (entry.Value as IDisposable)?.Dispose();
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="TimeoutException">锁定键超时 -或- 出现异常。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(object key, out object value)
        {
            return TryGetValueCore(key, out value);
        }

        /// <summary>
        /// <see cref="IMemoryCache.TryGetValue(object, out object)"/>实际调用此函数实现，派生类可重载此函数。
        /// 此函数会重置缓存项的最后使用时间。内部也会对键加锁。
        /// </summary>
        /// <param name="key">缓存项的键。</param>
        /// <param name="value">返回缓存键指定的值。</param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">对象已处置。</exception>
        protected virtual bool TryGetValueCore(object key, out object value)
        {
            ThrowIfDisposed();
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Options.DefaultLockTimeout);
            if (dw.IsEmpty)
            {
                value = default;
                OwHelper.SetLastError(258);
                return false;
            }
            if (!_Datas.TryGetValue(key, out var entity))
            {
                value = default;
                OwHelper.SetLastError(87);
                return false;
            }
            value = entity.Value;
            entity.LastUseUtc = DateTime.UtcNow;
            return true;
        }

        #region IDisposable接口相关

        /// <summary>
        /// 如果对象已经被处置则抛出<see cref="ObjectDisposedException"/>异常。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposed()
        {
            if (_IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //[DoesNotReturn]
        //static void Throw() => throw new ObjectDisposedException(typeof(LeafMemoryCache).FullName);

        private bool _IsDisposed;

        protected bool IsDisposed { get => _IsDisposed; }

        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _Datas = null;
                _IsDisposed = true;
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

        #endregion IMemoryCache接口相关

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfNotEntered(object key)
        {
            if (!_Options.IsEnteredCallback(key))
                throw new InvalidOperationException($"需要对键{key}加锁，但检测到没有锁定。");
        }

        /// <summary>
        /// 获取指定键的设置数据。没有找到则返回null。必须锁定键以后再调用此函数，否则可能出现争用。
        /// 可以更改返回值内部的内容，在解锁键之前不会生效。
        /// 这个函数不触发计时。
        /// </summary>
        /// <param name="key"></param>
        /// <returns>返回设置数据对象，没有找到键则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MemoryCacheBaseEntry GetCacheEntry(object key)
        {
            ThrowIfDisposed();
            ThrowIfNotEntered(key);
            return _Datas.TryGetValue(key, out var result) ? result : default;
        }

        /// <summary>
        /// 压缩缓存数据。
        /// </summary>
        /// <param name="percentage">回收比例。</param>
        public void Compact()
        {
            ThrowIfDisposed();
            Compact(Math.Max((long)(_Datas.Count * _Options.CompactionPercentage), 1));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="removalSizeTarget"></param>
        protected virtual void Compact(long removalSizeTarget)
        {
            var nowUtc = DateTime.UtcNow;
            long removalCount = 0;
            foreach (var item in _Datas)
            {
                using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, item.Key, TimeSpan.Zero);
                if (dw.IsEmpty) //忽略无法锁定的项
                    continue;
                if (!item.Value.IsExpired(nowUtc))  //若未超期
                    continue;
                if (RemoveCore(item.Value, EvictionReason.Expired))
                    if (++removalCount >= removalSizeTarget)    //若已经达成回收目标
                        break;
            }
        }

    }


}