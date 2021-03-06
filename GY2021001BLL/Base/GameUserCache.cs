/*
 * 文件放置游戏专用的一些基础类
 */
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;

namespace OW.Game.Cache
{
    public class HugeObjectCache : IMemoryCache
    {

        #region 缓存项

        public class HugeObjectCacheEntry : ICacheEntry
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
                        // 释放托管状态(托管对象)
                    }

                    // 释放未托管的资源(未托管的对象)并重写终结器
                    // 将大型字段设置为 null
                    _Disposed = true;
                }
            }

            // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
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

        #endregion 缓存项

        public HugeObjectCache()
        {

        }

        #region IMemoryCache接口及相关

        public ICacheEntry CreateEntry(object key)
        {
            throw new NotImplementedException();
        }

        public void Remove(object key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(object key, out object value)
        {
            throw new NotImplementedException();
        }

        #region IDisposable接口及相关
        public bool IsDisposed { get => _IsDisposed; }

        private bool _IsDisposed;


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
                _IsDisposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameUserCache()
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

        #endregion  IDisposable接口及相关

        #endregion IMemoryCache接口及相关

    }
}