using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Microsoft.Extensions.Caching.Memory
{
    public class EfObjectCacheOptions : DataObjectCacheOptions, IOptions<EfObjectCacheOptions>
    {
        public EfObjectCacheOptions() : base()
        {
        }

        /// <summary>
        /// 创建数据库上下文的回调，参数1是键，参数2是类型。
        /// key,type,返回的数据库上下文对象。
        /// </summary>
        public Func<object, Type, DbContext> CreateDbContextCallback { get; set; }

        /// <summary>
        /// 将缓存中的键可转换为数据库中的键的回调。
        /// </summary>
        public Func<object, object> CacheKey2DbKeyCallback { get; set; }

        /// <summary>
        /// 将数据库中的键，转换为缓存键的回调。
        /// </summary>
        public Func<object, object> DbKey2CacheKeyCallback { get; set; }

        EfObjectCacheOptions IOptions<EfObjectCacheOptions>.Value => this;
    }

    /// <summary>
    /// 特定于使用EntityFrame框架管理的数据库对象的缓存类。
    /// </summary>
    public class EfObjectCache : DataObjectCache
    {
        public class EfObjectCacheEntry : DataObjectCacheEntry, IDisposable
        {
            #region 构造函数

            public EfObjectCacheEntry(object key, EfObjectCache cache) : base(key, cache)
            {
            }

            #endregion 构造函数

            /// <summary>
            /// 对象的类型。
            /// </summary>
            public Type ObjectType { get; set; }

            /// <summary>
            /// 管理该对象的上下文。如果没有设置则调用<see cref="EfObjectCacheOptions.CreateDbContextCallback"/>创建一个。
            /// </summary>
            public DbContext Context { get; set; }

            #region IDisposable接口相关

            /// <summary>
            /// 若没有设置则自动设置加载，创建，和保存等回调。
            /// <inheritdoc/>
            /// </summary>
            public override void Dispose()
            {
                if (ObjectType is null)
                    throw new InvalidCastException($"需要设置{nameof(ObjectType)}属性。");
                var options = (EfObjectCacheOptions)Cache.Options;
                if (Context is null)
                    Context = options.CreateDbContextCallback(Key, ObjectType);
                if (CreateCallback is null)
                    CreateCallback = (key, state) =>
                    {
                        return TypeDescriptor.CreateInstance(null, ObjectType, null, null);
                    };
                if (LoadCallback is null)
                    LoadCallback = (key, state) =>
                    {
                        return Context.Find(ObjectType, options.CacheKey2DbKeyCallback(key));
                    };
                if (SaveCallback is null)
                    SaveCallback = (obj, state) =>
                    {
                        Context.SaveChanges();
                        return true;
                    };
                base.Dispose();
            }

            #endregion IDisposable接口相关
        }

        public EfObjectCache(IOptions<EfObjectCacheOptions> options) : base(options)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected override OwMemoryCacheBaseEntry CreateEntryCore(object key)
        {
            return new EfObjectCacheEntry(key, this);
        }
    }

    public static class EfObjectExtensions
    {
        public static EfObjectCache.EfObjectCacheEntry Set(this EfObjectCache.EfObjectCacheEntry entry)
        {
            return entry;
        }

        public static object GetOrCreateSingleObject(this EfObjectCache cache, object key, Type type, Func<object, Type, DbContext> createDbContextCallback = null)
        {
            OwHelper.SetLastError(0);
            if (cache.TryGetValue(key, out var result)) //首先试图取一次
                return result;
            var options = (EfObjectCacheOptions)cache.Options;
            using var dwKey = DisposeHelper.Create(options.LockCallback, options.UnlockCallback, key, options.DefaultLockTimeout);
            if (dwKey.IsEmpty)   //若超时
            {
                OwHelper.SetLastError(258);
                return null;
            }
            if (cache.TryGetValue(key, out result)) //锁定后第二次获取
                return result;
            using (var entry = (EfObjectCache.EfObjectCacheEntry)cache.CreateEntry(key))
            {
                var db = createDbContextCallback?.Invoke(key, type) ?? options.CreateDbContextCallback?.Invoke(key, type);
                entry.LoadCallbackState = db;
                entry.LoadCallback = (key, state) =>
                {
                    return ((DbContext)state).Find(type, key);
                };
                entry.SaveCallbackState = db;
                entry.SaveCallback = (value, state) =>
                {
                    var db = ((DbContext)state);
                    db.SaveChanges();
                    return true;
                };
                entry.CreateCallbackState = db;
                entry.CreateCallback = (key, state) =>
                  {
                      var db = ((DbContext)state);
                      var value = TypeDescriptor.CreateInstance(null, type, null, null);
                      db.Add(value);
                      return value;
                  };
            }
            if (cache.TryGetValue(key, out result))
            {
                OwHelper.SetLastError(0);
                return result;
            }
            return default;
        }

        public static IEnumerable<object> GetOrCreateCollection(this EfObjectCache cache, object key)
        {
            return Array.Empty<string>();
        }
    }
}
