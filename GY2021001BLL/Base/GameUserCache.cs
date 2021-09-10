/*
 * 文件放置游戏专用的一些基础类
 */

using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace OW.Game
{
    public class GameUserCacheEntry : ICacheEntry
    {

        public object Key => throw new NotImplementedException();

        public object Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTimeOffset? AbsoluteExpiration { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public TimeSpan? AbsoluteExpirationRelativeToNow { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public TimeSpan? SlidingExpiration { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IList<IChangeToken> ExpirationTokens => throw new NotImplementedException();

        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => throw new NotImplementedException();

        public CacheItemPriority Priority { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public long? Size { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #region IDisposable接口及相关

        private bool _Disposed;

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
        // ~GameUserCacheEntry()
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
        #endregion IDisposable接口及相关

    }

    /// <summary>
    /// 
    /// </summary>
    public class GameUserCache : MemoryCache, IMemoryCache
    {

        public GameUserCache(IServiceProvider service, IOptions<MemoryCacheOptions> optionsAccessor) : base(optionsAccessor)
        {
            _Service = service;
        }

        public GameUserCache(IServiceProvider service, IOptions<MemoryCacheOptions> optionsAccessor, ILoggerFactory loggerFactory) : base(optionsAccessor, loggerFactory)
        {
            _Service = service;
        }

        public new ICacheEntry CreateEntry(object key)
        {
            return base.CreateEntry(key);
        }

        public new void Remove(object key)
        {
            base.Remove(key);
        }

        public new bool TryGetValue(object key, out object value)
        {
            return base.TryGetValue(key, out value);
        }

        IServiceProvider _Service;

        VWorld _World;

        public VWorld World => _World ??= _Service.GetService<VWorld>();

        ConcurrentDictionary<Guid, GameUser> _Token2Users = new ConcurrentDictionary<Guid, GameUser>();
        ConcurrentDictionary<string, GameUser> _LoginName2Users = new ConcurrentDictionary<string, GameUser>();

        #region IDisposable接口及相关

        private bool _Disposed;

        protected override void Dispose(bool disposing)
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
        // ~GameUserCache()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        //public void Dispose()
        //{
        //    // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //    Dispose(disposing: true);
        //    GC.SuppressFinalize(this);
        //}
        #endregion IDisposable接口及相关
    }

}