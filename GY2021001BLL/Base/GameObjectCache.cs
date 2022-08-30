using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

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

        /// <summary>
        /// 构造函数。
        /// 若<see cref="EfObjectCacheOptions.CreateDbContextCallback"/>未设置，则自动设置为<see cref="VWorld.CreateNewUserDbContext"/>。
        /// </summary>
        /// <param name="world"></param>
        /// <param name="options"></param>
        public GameObjectCache(VWorld world, IOptions<GameObjectCacheOptions> options) : base(options)
        {
            _World = world;
        }

        VWorld _World;

        protected override OwMemoryCacheBaseEntry CreateEntryCore(object key)
        {
            return new GameObjectCacheEntry(key, this);
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
            entry.SetLoadCallback((key, state) => ((DbContext)state).Set<TEntity>().Find(dbKey), db)
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
        /// <param name="createDbCallback"></param>
        /// <returns></returns>
        public static T GetOrLoad<T>(this GameObjectCache cache, string key, Expression<Func<T, bool>> predicate, Func<Type, DbContext> createDbCallback,
            Action<GameObjectCache.GameObjectCacheEntry> setCallback = null) where T : class
        {
            if (cache.TryGetValue(key, out object result))
                return (T)result;
            using (var entry = (GameObjectCache.GameObjectCacheEntry)cache.CreateEntry(key))
            {
                entry.SetSingleObject(predicate, createDbCallback);
                setCallback?.Invoke(entry);
            }
            if (cache.TryGetValue(key, out result))
                return (T)result;
            return default;
        }

        #endregion GameObjectCache扩展
    }
}
