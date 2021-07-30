using GY2021001DAL;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using OW.Game.Expression;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GuangYuan.GY001.BLL
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
        private readonly CancellationTokenSource _CancellationTokenSource = new CancellationTokenSource();

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
        public GameItemTemplateManager ItemTemplateManager { get => _ItemTemplateManager ??= Services.GetRequiredService<GameItemTemplateManager>(); }

        private GameCharManager _GameCharManager;
        public GameCharManager CharManager { get => _GameCharManager ??= Services.GetRequiredService<GameCharManager>(); }

        private CombatManager _CombatManager;
        public CombatManager CombatManager { get => _CombatManager ??= Services.GetRequiredService<CombatManager>(); }

        private GameItemManager _GameItemManager;
        /// <summary>
        /// 虚拟事物管理器。
        /// </summary>
        public GameItemManager ItemManager { get => _GameItemManager ??= Services.GetRequiredService<GameItemManager>(); }

        private GameSocialManager _SocialManager;
        /// <summary>
        /// 社交管理器。
        /// </summary>
        public GameSocialManager SocialManager => _SocialManager ??= Services.GetRequiredService<GameSocialManager>();


        private BlueprintManager _BlueprintManager;
        /// <summary>
        /// 资源转换管理器。
        /// </summary>
        public BlueprintManager BlueprintManager { get => _BlueprintManager ??= Services.GetRequiredService<BlueprintManager>(); }

        private ObjectPool<List<GameItem>> _ObjectPoolListGameItem;

        public ObjectPool<List<GameItem>> ObjectPoolListGameItem
        {
            get
            {
                if (null == _ObjectPoolListGameItem)
                {
                    lock (ThisLocker)
                        _ObjectPoolListGameItem ??= Services.GetService<ObjectPool<List<GameItem>>>() ?? new DefaultObjectPool<List<GameItem>>(new ListGameItemPolicy(), Environment.ProcessorCount * 8);
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
        /// 获取两个数之间的一个随机数。支持并发调用。
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
            var logger = Services.GetRequiredService<ILogger<VWorld>>();
            logger.LogInformation("初始化完毕，开始服务。");
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

        /// <summary>
        /// 存储线程最后的错误信息。
        /// </summary>
        [ThreadStatic]
        static internal protected string _LastErrorMessage;

        /// <summary>
        /// 获取最后的错误信息。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public string GetLastErrorMessage() => _LastErrorMessage;

        /// <summary>
        /// 设置最后错误信息。
        /// </summary>
        /// <param name="msg"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void SetLastErrorMessage(string msg) => _LastErrorMessage = msg;
    }

    /// <summary>
    /// 
    /// </summary>
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

    /// <summary>
    /// 游戏的服务主机。
    /// </summary>
    public class GameHostedService : IHostedService
    {
        private readonly IServiceProvider _Services;

        public GameHostedService(IServiceProvider services)
        {
            _Services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var result = Task.Factory.StartNew(c =>
            {
                //CreateDb((IServiceProvider)c);
                //Task.Run(() => LoadCache());
            }, _Services, cancellationToken);
            return result;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 加载缓存。
        /// </summary>
        private void LoadCache()
        {
            var logger = _Services.GetService<ILogger<GameItemTemplateManager>>();
            try
            {
                var gitm = _Services.GetService<GameItemTemplateManager>();
                var templates = gitm.Id2Template;
                logger?.LogInformation($"{DateTime.UtcNow:s}服务已上线，初始化加载{templates.Count}个模板数据进行缓存。");
            }
            catch (Exception err)
            {
                logger?.LogError($"{DateTime.UtcNow:s}初始化发生异常{err.Message}", err);
            }
            var bptm = _Services.GetService<BlueprintManager>();
            var test = _Services.GetService<GamePropertyHelper>();
        }

        private void CreateDb(IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILogger<GameHostedService>>();
            try
            {
                var tContext = services.GetRequiredService<GameTemplateContext>();
                TemplateMigrateDbInitializer.Initialize(tContext);
                logger.LogInformation($"{DateTime.UtcNow}用户数据库已正常升级。ConnectionString={tContext.Database.GetDbConnection().ConnectionString}");

                var context = services.GetRequiredService<GY2021001DbContext>();
                MigrateDbInitializer.Initialize(context);
                logger.LogInformation($"{DateTime.UtcNow}用户数据库已正常升级。ConnectionString={context.Database.GetDbConnection().ConnectionString}");
            }
            catch (Exception err)
            {
                logger.LogError(err, $"An error occurred creating the DB.{err.Message}");
            }
        }

    }
}
