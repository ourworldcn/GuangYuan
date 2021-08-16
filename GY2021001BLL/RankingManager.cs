using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace GuangYuan.GY001.BLL
{
    /// <summary>
    /// 排行榜管理器。
    /// </summary>
    public class RankingManager : GameManagerBase<RankingOptions>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public RankingManager()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        public RankingManager(IServiceProvider service) : base(service)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        public RankingManager(IServiceProvider service, RankingOptions options) : base(service, options)
        {
        }

        MemoryCache _Cache = new MemoryCache(new MemoryCacheOptions() { });

        protected IMemoryCache Cache => _Cache;

        GY001UserContext _UserDB;
        protected GY001UserContext UserDB
        {
            get
            {
                if (_UserDB is null)
                    lock (_Cache)
                        if (_UserDB is null)
                            _UserDB = World.CreateNewUserDbContext();
                return _UserDB;
            }
        }

        public GameRanking FindAndLock(Guid id)
        {
            Cache.GetOrCreate(id.ToString(), c =>
            {
                lock (UserDB)
                    return UserDB.Rankings.Find(id);
            });
            return null;
        }

        public void Unlock(GameRanking obj)
        {
            
        }
    }

    /// <summary>
    /// 排行榜管理器的配置类。
    /// </summary>
    public class RankingOptions
    {
        public RankingOptions()
        {

        }
    }
}
