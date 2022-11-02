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
    public class OwMemoryCacheBaseOptions : MemoryCacheOptions, IOptions<OwMemoryCacheBaseOptions>
    {
        /// <summary>
        /// 构造函数。
        /// 设置<see cref="MemoryCacheOptions.ExpirationScanFrequency"/>为1分钟。
        /// </summary>
        public OwMemoryCacheBaseOptions() : base()
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
        public OwMemoryCacheBaseOptions Value => this;

    }

    public class BeforeEvictionCallbackRegistration
    {
        public BeforeEvictionCallbackRegistration()
        {

        }

        /// <summary>
        /// 在即将被驱逐时调用的回调集合。
        /// 键，值，驱逐原因，用户对象。
        /// </summary>
        public Action<object, object, EvictionReason, object> BeforeEvictionCallback { get; set; }

        public object State { get; set; }

    }

    /// <summary>
    /// 内存缓存的基础类。
    /// </summary>
    public abstract class OwMemoryCacheBase : IMemoryCache, IDisposable
    {
        public abstract class OwMemoryCacheBaseEntry : ICacheEntry
        {
            /// <summary>
            /// 构造函数。
            /// </summary>
            /// <param name="cache">指定所属缓存对象，在调用<see cref="Dispose"/>时可以加入该对象。</param>
            protected OwMemoryCacheBaseEntry(object key, OwMemoryCacheBase cache)
            {
                Key = key;
                Cache = cache;
            }

            /// <summary>
            /// 所属的缓存对象。
            /// </summary>
            public OwMemoryCacheBase Cache { get; set; }

            #region ICacheEntry接口相关

            object _Key;

            public object Key { get => _Key; set => _Key = value; }

            public virtual object Value { get; set; }

            public DateTimeOffset? AbsoluteExpiration { get; set; }

            public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

            public TimeSpan? SlidingExpiration { get; set; }

            public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();

            internal Lazy<List<PostEvictionCallbackRegistration>> _PostEvictionCallbacksLazyer = new Lazy<List<PostEvictionCallbackRegistration>>(true);
            /// <summary>
            /// 所有的函数调用完毕才会解锁键对象。
            /// </summary>
            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => _PostEvictionCallbacksLazyer.Value;

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
            /// 在完成时自动调用<see cref="AddItemCore(ICacheEntry)"/>(在锁内)。
            /// </summary>
            /// <exception cref="TimeoutException">试图锁定键超时。</exception>
            public virtual void Dispose()
            {
                using var dw = DisposeHelper.Create(Cache.Options.LockCallback, Cache.Options.UnlockCallback, Key, Cache.Options.DefaultLockTimeout);
                if (dw.IsEmpty)
                    throw new TimeoutException();
                if (!_IsDisposed)
                {
                    var factEntity = Cache._Items.AddOrUpdate(Key, this, (key, ov) => this);
                    factEntity.LastUseUtc = DateTime.UtcNow;
                    _IsDisposed = true;
                }
                Cache.AddItemCore(this);
            }
            #endregion IDisposable接口相关

            #endregion ICacheEntry接口相关

            internal Lazy<List<BeforeEvictionCallbackRegistration>> _BeforeEvictionCallbacksLazyer = new Lazy<List<BeforeEvictionCallbackRegistration>>(true);
            /// <summary>
            /// 获取或设置从缓存中即将逐出缓存项时将触发的回叫。
            /// 所有的函数调用完毕才会解锁键对象。
            /// 支持并发初始化，但返回集合本身不能支持并发。
            /// </summary>
            public IList<BeforeEvictionCallbackRegistration> BeforeEvictionCallbacks => _BeforeEvictionCallbacksLazyer.Value;

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

        }

        #region 构造函数相关

        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwMemoryCacheBase(IOptions<OwMemoryCacheBaseOptions> options)
        {
            _Options = options.Value;
        }

        #endregion 构造函数相关

        #region 属性及相关

        OwMemoryCacheBaseOptions _Options;
        /// <summary>
        /// 获取设置对象。
        /// </summary>
        public OwMemoryCacheBaseOptions Options => _Options;

        ConcurrentDictionary<object, OwMemoryCacheBaseEntry> _Items = new ConcurrentDictionary<object, OwMemoryCacheBaseEntry>();
        /// <summary>
        /// 所有缓存项的字典，键是缓存项的键，值缓存项的包装数据。该接口可以并发枚举。
        /// </summary>
        protected IReadOnlyDictionary<object, OwMemoryCacheBaseEntry> Items => _Items;

        #endregion 属性及相关

        #region IMemoryCache接口相关

        /// <summary>
        /// 创建一个键。
        /// 该公有函数会首先锁定键。
        /// </summary>
        /// <param name="key"></param>
        /// <returns>返回的是<see cref="CreateEntryCore"/>创建的对象。
        /// null表示锁定键超时 -或- 指定键已经存在。调用<see cref="OwHelper.GetLastError"/>可获取详细信息。258=锁定超时，698=键已存在，1168=键不存在。
        /// </returns>
        /// <exception cref="ObjectDisposedException">对象已处置。</exception>
        public ICacheEntry CreateEntry(object key)
        {
            ThrowIfDisposed();
            using var dw = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, Options.DefaultLockTimeout);
            if (dw.IsEmpty) //若超时
            {
                OwHelper.SetLastError(258);
                return null;
            }
            if (_Items.ContainsKey(key))    //若键已经存在
            {
                OwHelper.SetLastError(698); //ERROR_OBJECT_NAME_EXISTS ,1168L Element not found.
                return null;
            }
            return CreateEntryCore(key);
        }

        /// <summary>
        /// <see cref="IMemoryCache.CreateEntry(object)"/>实际调用此函数实现，派生类可需要实现此函数。
        /// 不要考虑是否已经存在指定键的问题。
        /// 派生类可以重载此函数。非公有函数不会自动对键加锁，若需要调用者需要负责加/解锁。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected abstract OwMemoryCacheBaseEntry CreateEntryCore(object key);

        /// <summary>
        /// 某一项加入缓存时被调用。该实现立即返回。
        /// 派生类可以重载此函数。非公有函数不会自动对键加锁，若需要调用者需要负责加/解锁。
        /// </summary>
        /// <param name="entry"></param>
        protected virtual void AddItemCore(OwMemoryCacheBaseEntry entry) { }

        /// <summary>
        /// 获取指定键的设置数据。没有找到则返回null。
        /// 可以更改返回值内部的内容，在解锁键之前不会生效。
        /// 这个函数不触发计时。
        /// 该函数不会自动对键加锁，若需要调用者需要负责加/解锁。
        /// </summary>
        /// <param name="key"></param>
        /// <returns>返回设置数据对象，没有找到键则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OwMemoryCacheBaseEntry GetCacheEntry(object key)
        {
            ThrowIfDisposed();
            return _Items.TryGetValue(key, out var result) ? result : default;
        }

        /// <summary>
        /// 试图移除指定键的缓存项。
        /// 该公有函数会首先锁定键。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="timeout">锁定键的最长超时，省略或为null则使用<see cref="OwMemoryCacheBaseOptions.DefaultLockTimeout"/>。</param>
        /// <returns>
        /// true成功移除，false锁定键超时或指定键不存在。
        /// 调用<see cref="OwHelper.GetLastError"/>可获取详细信息。0=成功，258=锁定超时，698=键已存在，1168=键不存在。
        /// </returns>
        /// <exception cref="ObjectDisposedException">对象已处置。</exception>
        public bool TryRemove(object key, TimeSpan? timeout = null)
        {
            ThrowIfDisposed();
            using var dw = Lock(key, timeout);
            if (!dw.IsEmpty) //若未超时
            {
                var entry = GetCacheEntry(key);
                if (entry != null)
                    if (RemoveCore(entry, EvictionReason.Removed))
                        OwHelper.SetLastError(0);
                    else
                        OwHelper.SetLastError(1168);
                else
                    OwHelper.SetLastError(1168);
            }
            return OwHelper.GetLastError() == 0;
        }

        /// <summary>
        /// null表示锁定键超时 -或- 指定键已经存在。
        /// 调用<see cref="OwHelper.GetLastError"/>可获取详细信息。0=成功，258=锁定超时，698=键已存在，1168=键不存在。
        /// 该公有函数会首先锁定键。
        /// </summary>
        /// <remarks>若没有找到指定键，则立即返回。
        /// </remarks>
        /// <param name="key"></param>
        /// <exception cref="TimeoutException">锁定键超时 -或- 出现异常。</exception>
        /// <exception cref="ObjectDisposedException">对象已处置。</exception>
        public void Remove(object key)
        {
            if (!TryRemove(key))
                ThrowIfLockKeyTimeout(key);
        }

        /// <summary>
        /// 以指定原因移除缓存项。
        /// 此函数会调用<see cref="OwMemoryCacheBaseEntry.BeforeEvictionCallbacks"/>所有回调，然后移除配置项,最后调用所有<see cref="OwMemoryCacheBaseEntry.PostEvictionCallbacks"/>回调。
        /// 回调的异常均被忽略。
        /// 派生类可以重载此函数。非公有函数不会自动对键加锁，若需要调用者需要负责加/解锁。
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="reason"></param>
        /// <returns>true=成功移除，false=没有找到指定键。</returns>
        protected virtual bool RemoveCore(OwMemoryCacheBaseEntry entry, EvictionReason reason)
        {
            try
            {
                if (entry._BeforeEvictionCallbacksLazyer.IsValueCreated)
                    entry.BeforeEvictionCallbacks.SafeForEach(c => c.BeforeEvictionCallback?.Invoke(entry.Key, entry.Value, reason, c.State));
            }
            catch (Exception)
            {
            }
            var result = _Items.TryRemove(entry.Key, out _);
            try
            {
                if (entry._PostEvictionCallbacksLazyer.IsValueCreated)
                    entry.PostEvictionCallbacks.SafeForEach(c => c.EvictionCallback?.Invoke(entry.Key, entry.Value, reason, c.State));
            }
            catch (Exception)
            {
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>true成功缓存值，false锁定键超时或指定键不存在。
        /// 调用<see cref="OwHelper.GetLastError"/>可获取详细信息。0=成功，258=锁定超时，698=键已存在，1168=键不存在。</returns>
        /// <exception cref="ObjectDisposedException">对象已处置。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(object key, out object value)
        {
            ThrowIfDisposed();
            using var dw = Lock(key);
            if (dw.IsEmpty)
            {
                value = default;
                return false;
            }
            else
            {
                var entry = GetCacheEntry(key);
                var b = !(entry is null) && TryGetValueCore(entry);
                value = entry?.Value;
                if (b)
                    OwHelper.SetLastError(0);
                else
                    OwHelper.SetLastError(1168);
                return b;
            }
        }

        /// <summary>
        /// <see cref="IMemoryCache.TryGetValue(object, out object)"/>实际调用此函数实现，派生类可重载此函数。
        /// 派生类可以重载此函数。非公有函数不会自动对键加锁，若需要调用者需要负责加/解锁。
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>该实现会重置缓存项的最后使用时间，并立即返回true。</returns>
        /// <exception cref="ObjectDisposedException">对象已处置。</exception>
        protected virtual bool TryGetValueCore(OwMemoryCacheBaseEntry entry)
        {
            entry.LastUseUtc = Options.Clock?.UtcNow.UtcDateTime ?? DateTime.UtcNow;
            return true;
        }

        /// <summary>
        /// 用<see cref="OwMemoryCacheBaseOptions.LockCallback"/>锁定指定的键,以进行临界操作。
        /// 在未来用<see cref="OwMemoryCacheBaseOptions.UnlockCallback"/>解锁。
        /// 特别地，会用<see cref="OwHelper.SetLastError(int)"/>设置错误码，如果超时则设置258，成功锁定设置0。
        /// </summary>
        /// <param name="key">不会校验该key是否在本缓存内有无映射对象。</param>
        /// <param name="timeout">锁定键的最长超时，省略或为null则使用<see cref="OwMemoryCacheBaseOptions.DefaultLockTimeout"/>。</param>
        /// <returns>返回的结构可以用using 语句保证释放。判断<see cref="DisposeHelper{T}.IsEmpty"/>可以知道是否锁定成功。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DisposeHelper<object> Lock(object key, TimeSpan? timeout = null)
        {
            var result = DisposeHelper.Create(Options.LockCallback, Options.UnlockCallback, key, timeout ?? Options.DefaultLockTimeout);
            if (result.IsEmpty)
                OwHelper.SetLastError(258);
            else
                OwHelper.SetLastError(0);
            return result;
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

        /// <summary>
        /// 通过检测<see cref="OwHelper.GetLastError"/>返回值是否为258(WAIT_TIMEOUT)决定是否抛出异常<seealso cref="TimeoutException"/>。
        /// </summary>
        /// <param name="msg"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfTimeout(string msg)
        {
            if (OwHelper.GetLastError() == 258)
                throw new TimeoutException(msg);
        }

        /// <summary>
        /// 根据<see cref="OwHelper.GetLastError"/>返回值判断是否抛出锁定键超时的异常。
        /// </summary>
        /// <param name="key"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfLockKeyTimeout(object key)
        {
            if (OwHelper.GetLastError() == 258)
                throw new TimeoutException($"锁定键时超时，键:{key}");
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
                    //释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Options = null;
                _Items = null;
                _IsDisposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
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
        /// 压缩缓存数据。
        /// </summary>
        /// <param name="percentage">回收比例。</param>
        public void Compact()
        {
            ThrowIfDisposed();
            Compact(Math.Max((long)(_Items.Count * _Options.CompactionPercentage), 1));
        }

        /// <summary>
        /// 实际压缩的函数。
        /// </summary>
        /// <remarks>在调用<see cref="RemoveCore(OwMemoryCacheBaseEntry, EvictionReason)"/>之前会锁定键。</remarks>
        /// <param name="removalSizeTarget"></param>
        protected virtual void Compact(long removalSizeTarget)
        {
            var nowUtc = DateTime.UtcNow;
            long removalCount = 0;
            foreach (var item in _Items)
            {
                using var dw = Lock(item.Key, TimeSpan.Zero);
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

    /// <summary>
    /// 
    /// </summary>
    public static class OwMemoryCacheBaseExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="keys"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static IDisposable LockKeys(this OwMemoryCacheBase cache, IEnumerable<object> keys, TimeSpan timeout)
        {
            return OwHelper.LockWithOrder(keys.OrderBy(c => c), (obj, timeout) =>
            {
                if (!cache.Options.LockCallback(obj, timeout))
                    return null;
                return DisposerWrapper.Create(c => cache.Options.UnlockCallback(c), obj);
            }, timeout);
        }

    }
}