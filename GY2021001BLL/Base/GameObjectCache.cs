using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
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

    public class GameObjectCache : EfObjectCache
    {
        public class GameObjectCacheEntry : EfObjectCacheEntry
        {
            public GameObjectCacheEntry(object key, EfObjectCache cache) : base(key, cache)
            {
                AutoDispose = true;
                SaveWhenLeave = true;
                var options = (GameObjectCacheOptions)cache.Options;
                LoadCallback = (key, state) => Context.Find(ObjectType, options.CacheKey2DbKeyCallback?.Invoke(key) ?? key);
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
            innerOptions.CreateDbContextCallback ??= c => _World.CreateNewUserDbContext();

        }

        VWorld _World;

        protected override MemoryCacheBaseEntry CreateEntryCore(object key)
        {
            return new GameObjectCacheEntry(key, this);
        }
    }
}
