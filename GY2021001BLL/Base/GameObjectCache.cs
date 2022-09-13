using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;

namespace OW.Game.Caching
{
    public class GameObjectCacheOptions : DataObjectCacheOptions, IOptions<GameObjectCacheOptions>
    {
        public GameObjectCacheOptions() : base()
        {
            LockCallback = (key, timeout) => StringLocker.TryEnter((string)key, timeout);
            UnlockCallback = key => StringLocker.Exit((string)key);
            IsEnteredCallback = key => StringLocker.IsEntered((string)key);
        }

        GameObjectCacheOptions IOptions<GameObjectCacheOptions>.Value => this;

        /// <summary>
        /// 创建数据库上下文的回调。
        /// (数据库实体的类型,数据库内的键)=>访问用的数据库上下文。
        /// </summary>
        public Func<Type, object, DbContext> CreateDbCallback { get; set; }
    }

    /// <summary>
    /// 特定适用于游戏世界内对象的缓存服务对象。
    /// </summary>
    public class GameObjectCache : DataObjectCache
    {
        /// <summary>
        /// 
        /// </summary>
        public class GameObjectCacheEntry : DataObjectCacheEntry
        {
            /// <summary>
            /// 构造函数。
            /// 会自动设置加载，保存回调，并在驱逐前强制保存，驱逐后处置上下文及对象本身（如果支持IDisposable接口）
            /// </summary>
            /// <param name="key"></param>
            /// <param name="cache"></param>
            public GameObjectCacheEntry(object key, GameObjectCache cache) : base(key, cache)
            {
            }

            public override void Dispose()
            {
                base.Dispose();
            }

        }

        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// 若<see cref="EfObjectCacheOptions.CreateDbContextCallback"/>未设置，则自动设置为<see cref="VWorld.CreateNewUserDbContext"/>。
        /// </summary>
        /// <param name="world"></param>
        /// <param name="options"></param>
        public GameObjectCache(VWorld world, IOptions<GameObjectCacheOptions> options) : base(options)
        {
            _World = world;
            if (options.Value.CreateDbCallback is null)
            {
                options.Value.CreateDbCallback = (type, dbKey) => _World.CreateNewUserDbContext();
            }
        }

        #endregion 构造函数

        VWorld _World;

        protected override OwMemoryCacheBaseEntry CreateEntryCore(object key)
        {
            return new GameObjectCacheEntry(key, this);
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {

                }
                _World = null;
            }
            base.Dispose(disposing);
        }
    }

    public static class GameObjectCacheExtensions
    {
        public static GameObjectCache.GameObjectCacheEntry SetSingleObject<TEntity>(this GameObjectCache.GameObjectCacheEntry entry,
            Expression<Func<TEntity, bool>> predicate,
            Func<Type, DbContext> createDbCallback) where TEntity : class
        {
            var db = createDbCallback(typeof(TEntity));
            entry.SetLoadCallback((key, state) =>
            {
                var result = ((DbContext)state).Set<TEntity>().FirstOrDefault(predicate);
                return result;
            }, db)
            .SetSaveCallback((obj, state) =>
            {
                try
                {
                    ((DbContext)state).SaveChanges();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }, db)
            .RegisterBeforeEvictionCallback((key, value, reason, state) =>
            {
                db.SaveChanges();
            }, db)
            .RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                (value as IDisposable)?.Dispose();
            }, db);
            return entry;
        }

        /// <summary>
        /// 设置加载，驱逐ef对象。
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entry"></param>
        /// <param name="dbKey"></param>
        /// <param name="createDbCallback">创建数据库上下文的回调。</param>
        /// <returns></returns>
        public static GameObjectCache.GameObjectCacheEntry SetSingleObject<TEntity>(this GameObjectCache.GameObjectCacheEntry entry, object dbKey,
            Func<object, Type, DbContext> createDbCallback) where TEntity : class
        {
            var db = createDbCallback(dbKey, typeof(TEntity));
            entry.SetLoadCallback((key, state) => ((DbContext)state).Set<TEntity>().Find(dbKey), db);
            entry.SetSaveAndEviction(db);
            return entry;
        }

        public static GameObjectCache.GameObjectCacheEntry SetCreateNew<TEntity>(this GameObjectCache.GameObjectCacheEntry entry, object dbKey, Func<object, object, object> createCallback,
            Func<object, Type, DbContext> createDbCallback) where TEntity : class
        {
            var db = createDbCallback(dbKey, typeof(TEntity));
            entry.SetCreateCallback(createCallback, db);
            entry.SetSaveAndEviction(db);
            return entry;
        }

        /// <summary>
        /// 设置默认的保存和驱逐前后的回调。
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static GameObjectCache.GameObjectCacheEntry SetSaveAndEviction(this GameObjectCache.GameObjectCacheEntry entry, DbContext db)
        {
            entry.SetSaveCallback((obj, state) =>
            {
                try
                {
                    ((DbContext)state).SaveChanges();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }, db)
            .RegisterBeforeEvictionCallback((key, value, reason, state) =>
            {
                ((DbContext)state).SaveChanges();
            }, db)
            .RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                (value as IDisposable)?.Dispose();
            }, db);
            return entry;
        }

        public static GameObjectCache.GameObjectCacheEntry SetCollection<TElement>(this GameObjectCache.GameObjectCacheEntry entry, string key,
            Expression<Func<TElement, bool>> predicate,
            Func<string, Type, DbContext> createDbCallback = null) where TElement : class
        {
            //TODO
            throw new NotImplementedException();

            //DbContext db = createDbCallback(key, typeof(TElement));
            //ObservableCollection<TElement> oc = new ObservableCollection<TElement>();
            //db.Set<TElement>().SingleOrDefault(predicate);
            //return entry;
        }

        #region GameObjectCache扩展

        /// <summary>
        /// 获取或加载缓存对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cache"></param>
        /// <param name="key"></param>
        /// <param name="predicate"></param>
        /// <param name="createDbCallback">创建数据库上下文的回调，省略或为null则使用配置中的回调<see cref="GameObjectCacheOptions.CreateDbCallback"/>。</param>
        /// <returns>获取或加载的缓存对象，如果为null则说明后被存储和缓存中均无此对象，此时没有缓存项被加入缓存。
        /// 或锁定超时也可能导致返回null(<see cref="OwHelper.GetLastError"/>返回258)</returns>
        public static T GetOrLoad<T>(this GameObjectCache cache, string key, Expression<Func<T, bool>> predicate, Action<GameObjectCache.GameObjectCacheEntry> setCallback = null,
            Func<Type, object, DbContext> createDbCallback = null) where T : class
        {
            if (cache.TryGetValue(key, out T result))   //若已经在缓存中
                return result;
            using var dwKey = cache.Lock(key);
            if (dwKey.IsEmpty)   //若锁定超时
                return null;
            if (cache.TryGetValue(key, out result))   //若已经在缓存中
                return result;    //二相获取
            //此时已经确定不在缓存中
            createDbCallback ??= ((GameObjectCacheOptions)cache.Options).CreateDbCallback;
            var db = createDbCallback(typeof(T), key);
            result = db.Set<T>().FirstOrDefault(predicate);
            if (result is null)  //若后背存储也没有
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到Id={key}的对象");
                return null;
            }
            return cache.GetOrCreate(key, c =>
            {
                var entry = (GameObjectCache.GameObjectCacheEntry)c;
                entry.SetSaveAndEviction(db);
                return result;
            });
        }

        #endregion GameObjectCache扩展
    }
}
