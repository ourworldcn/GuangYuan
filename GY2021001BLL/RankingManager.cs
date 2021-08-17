using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

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

        public void PostEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
        }

        public GameRanking FindAndLock(Guid id)
        {
            var result = Cache.GetOrCreate(id.ToString(), c =>
            {
                GameRanking result;
                lock (UserDB)
                    result = UserDB.Rankings.Find(id);
                c.SetSlidingExpiration(TimeSpan.FromMinutes(1));
                c.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration() { EvictionCallback = PostEvictionCallback, State = _Cache });
                return result;
            });
            Monitor.Enter(result);
            return result;
        }

        public void Unlock(GameRanking obj)
        {
            UserDB.SaveChanges();
            Monitor.Exit(obj);
        }

        /// <summary>
        /// 获取该用户的指定指定日期的可pvp对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="now"></param>
        public void GtePvpChars(GameChar gameChar,ref DateTime now)
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
