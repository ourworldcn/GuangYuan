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
    public class GameObjectCacheOptions : EfObjectCacheOptions, IOptions<GameObjectCacheOptions>
    {
        public GameObjectCacheOptions() : base()
        {
            LockCallback = (key, timeout) => StringLocker.TryEnter((string)key, timeout);
            UnlockCallback = key => StringLocker.Exit((string)key);
            IsEnteredCallback = key => StringLocker.IsEntered((string)key);

            CacheKey2DbKeyCallback = key => Guid.Parse((string)key);

        }

        GameObjectCacheOptions IOptions<GameObjectCacheOptions>.Value => this;

    }

    /// <summary>
    /// 特定适用于游戏世界内对象的缓存服务对象。
    /// </summary>
    public class GameObjectCache : EfObjectCache
    {
        /// <summary>
        /// 
        /// </summary>
        public class GameObjectCacheEntry : EfObjectCacheEntry
        {
            /// <summary>
            /// 构造函数。
            /// 会自动设置加载，保存回调，并在驱逐前强制保存，驱逐后处置上下文及对象本身（如果支持IDisposable接口）
            /// </summary>
            /// <param name="key"></param>
            /// <param name="cache"></param>
            public GameObjectCacheEntry(object key, EfObjectCache cache) : base(key, cache)
            {
                var options = (GameObjectCacheOptions)cache.Options;
                LoadCallback = (key, state) => Context.Find(ObjectType, options.CacheKey2DbKeyCallback?.Invoke(key) ?? key);
                BeforeEvictionCallbacks.Add(new BeforeEvictionCallbackRegistration()    //驱逐前自动保存
                {
                    BeforeEvictionCallback = (key, val, res, state) =>
                    {
                        var entry = (GameObjectCacheEntry)state;
                        ((GameObjectCache)entry.Cache).EnsureSavedCore(entry);
                    },
                    State = this,
                });
                this.RegisterPostEvictionCallback((key, val, res, state) =>
                {
                    (val as IDisposable)?.Dispose();
                    ((GameObjectCacheEntry)state).Context?.Dispose();
                }, this); //驱逐后自动处置相关对象
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
            var innerOptions = (GameObjectCacheOptions)Options;
            innerOptions.CreateDbContextCallback ??= (c, type) => _World.CreateNewUserDbContext();

        }

        VWorld _World;

        protected override MemoryCacheBaseEntry CreateEntryCore(object key)
        {
            return new GameObjectCacheEntry(key, this);
        }
    }

    public static class GameObjectCacheExtensions
    {
        public static GameObjectCache.GameObjectCacheEntry SetSingleObject<TEntity>(this GameObjectCache.GameObjectCacheEntry entry, string key, object dbKey,
            Func<string, Type, DbContext> createDbCallback = null)
        {
            return entry;
        }

        public static GameObjectCache.GameObjectCacheEntry SetCollection<TElement>(this GameObjectCache.GameObjectCacheEntry entry, string key,
            Expression<Func<TElement, bool>> predicate,
            Func<string, Type, DbContext> createDbCallback = null) where TElement : class
        {
            DbContext db = createDbCallback(key, typeof(TElement));
            ObservableCollection<TElement> oc = new ObservableCollection<TElement>();
            db.Set<TElement>().SingleOrDefault(predicate);
            return entry;
        }
    }
}
