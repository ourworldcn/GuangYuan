using GY2021001DAL;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using OwGame;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace GY2021001BLL
{
    public class VWorldOptions
    {
        public DbContextOptions<GY2021001DbContext> UserDbOptions { get; set; }
        public DbContextOptions<GameTemplateContext> TemplateDbOptions { get; set; }
    }

    /// <summary>
    /// 非敏感性服务器信息。
    /// </summary>
    public class VWorldInfomation
    {
        /// <summary>
        /// 服务器的本次启动Utc时间。
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// 服务器的当前时间。
        /// </summary>
        public DateTime CurrentDateTime { get; set; }
    }

    /// <summary>
    /// 游戏世界的服务。
    /// </summary>
    public class VWorld : GameManagerBase<VWorldOptions>
    {
        public readonly DateTime StartDateTimeUtc = DateTime.UtcNow;

        CancellationTokenSource _CancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// 该游戏世界因为种种原因已经请求卸载。
        /// </summary>
        public CancellationToken RequestShutdown;

        #region 构造函数

        public VWorld()
        {
            Initialize();
        }

        public VWorld(IServiceProvider serviceProvider, VWorldOptions options) : base(serviceProvider, options)
        {
            Initialize();
        }

        #endregion 构造函数

        #region 属性及相关
        private GameItemTemplateManager _ItemTemplateManager;
        public GameItemTemplateManager ItemTemplateManager { get => _ItemTemplateManager ??= Service.GetRequiredService<GameItemTemplateManager>(); }

        private GameCharManager _GameCharManager;
        public GameCharManager CharManager { get => _GameCharManager ??= Service.GetRequiredService<GameCharManager>(); }

        private CombatManager _CombatManager;
        public CombatManager CombatManager { get => _CombatManager ??= Service.GetRequiredService<CombatManager>(); }

        private GameItemManager _GameItemManager;

        public GameItemManager ItemManager { get => _GameItemManager ??= Service.GetRequiredService<GameItemManager>(); }

        private BlueprintManager _BlueprintManager;

        public BlueprintManager BlueprintManager { get => _BlueprintManager ??= Service.GetRequiredService<BlueprintManager>(); }

        ObjectPool<List<GameItem>> _ObjectPoolListGameItem;

        public ObjectPool<List<GameItem>> ObjectPoolListGameItem
        {
            get
            {
                if (null == _ObjectPoolListGameItem)
                {
                    lock (ThisLocker)
                        _ObjectPoolListGameItem ??= Service.GetService<ObjectPool<List<GameItem>>>() ?? new DefaultObjectPool<List<GameItem>>(new ListGameItemPolicy(), Environment.ProcessorCount * 8);
                }
                return _ObjectPoolListGameItem;
            }
        }
        #region 随机数相关
        /// <summary>
        /// 公用随机数生成器。
        /// </summary>
        [ThreadStatic]
        private static Random _WorldRandom;

        public static Random WorldRandom => _WorldRandom ??= new Random();

        /// <summary>
        /// 获取两个数之间的一个随机数。
        /// </summary>
        /// <param name="from">返回值大于或等于此参数。</param>
        /// <param name="to">返回值小于此参数。</param>
        /// <returns>随机数属于区间 [ <paramref name="from"/>, <paramref name="to"/> )</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetRandomNumber(double from, double to)
        {
            return WorldRandom.NextDouble() * (to - from) + from;
        }

        #endregion 随机数相关


        #endregion 属性及相关

        private void Initialize()
        {
            RequestShutdown = _CancellationTokenSource.Token;
        }

        public TimeSpan GetServiceTime()
        {
            return DateTime.UtcNow - StartDateTimeUtc;
        }

        /// <summary>
        /// 通知游戏世界开始下线。
        /// </summary>
        public void NotifyShutdown()
        {
            _CancellationTokenSource.Cancel();
        }

        /// <summary>
        /// 新建一个用户数据库的上下文对象。
        /// 调用者需要自行负责清理对象。
        /// </summary>
        /// <returns></returns>
        public GY2021001DbContext CreateNewUserDbContext()
        {
            //DbContextOptionsBuilder.EnableSensitiveDataLogging
            return new GY2021001DbContext(Options.UserDbOptions);
        }

        /// <summary>
        /// 创建模板数据库上下文对象。
        /// 调用者需要自行负责清理对象。
        /// </summary>
        /// <returns></returns>
        public GameTemplateContext CreateNewTemplateDbContext()
        {
            return new GameTemplateContext(Options.TemplateDbOptions);
        }

        public VWorldInfomation GetInfomation()
        {
            return new VWorldInfomation()
            {
                StartDateTime = StartDateTimeUtc,
                CurrentDateTime = DateTime.UtcNow,
            };
        }

        /// <summary>
        /// 测试指定概率数值是否命中。
        /// </summary>
        /// <param name="val">概率，取值[0,1],大于1则视同1，小于0则视同0,1必定返回true,0必定返回false。</param>
        /// <param name="random"></param>
        /// <returns>true命中，false未命中。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsHit(double val, Random random = null)
        {
            return (random ?? WorldRandom).NextDouble() < val;
        }
    }

    public class ListGameItemPolicy : PooledObjectPolicy<List<GameItem>>
    {
        public ListGameItemPolicy()
        {
        }

        public override List<GameItem> Create()
        {
            return new List<GameItem>();
        }

        public override bool Return(List<GameItem> obj)
        {
            obj.Clear();
            return true;
        }

    }
}
