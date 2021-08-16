using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using OW.Game.Expression;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GuangYuan.GY001.BLL
{
    /// <summary>
    /// 虚拟世界主控服务的配置类。
    /// </summary>
    public class VWorldOptions
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public VWorldOptions()
        {

        }

        public DbContextOptions<GY001UserContext> UserDbOptions { get; set; }
        public DbContextOptions<GY001TemplateContext> TemplateDbOptions { get; set; }
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
    /// 游戏世界的服务。目前一个虚拟世界，对应唯一一个本类对象，且一个应用程序域（AppDomain）最多支持一个虚拟世界。
    /// 本质上游戏相关类群使用AOC机制，但不依赖DI，所以，在其所处的应用程序域内，本类的唯一对象（单例）部分的代替了容器。
    /// 这种设计确实去耦不彻底，但是使用起来很方便。
    /// </summary>
    public class VWorld : GameManagerBase<VWorldOptions>
    {
        private static IServiceProvider _RootServices;

        public static IServiceProvider RootServices => _RootServices;

        public static void Initialize(IServiceProvider service)
        {
            _RootServices = service;
        }

        private static VWorld _Default;
        public VWorld Default => _Default ??= new VWorld(_RootServices, null);

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
                        _ObjectPoolListGameItem ??= (Services.GetService<ObjectPool<List<GameItem>>>() ??
                            new DefaultObjectPool<List<GameItem>>(new ListGameItemPolicy(), Environment.ProcessorCount * 8));
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

        /// <summary>
        /// 获取是否命中。支持并发调用。
        /// </summary>
        /// <param name="val">命中概率，小于0视同0，永远返回 false,大于或等于1都会永远返回true.</param>
        /// <returns>是否命中</returns>
        public static bool IsHit(double val) =>
            WorldRandom.NextDouble() < val;

        #endregion 随机数相关


        #endregion 属性及相关

        private void Initialize()
        {
            RequestShutdown = _CancellationTokenSource.Token;
            var logger = Services.GetRequiredService<ILogger<VWorld>>();
            logger.LogInformation("初始化完毕，开始服务。");
        }

        public TimeSpan GetServiceTime() =>
            DateTime.UtcNow - StartDateTimeUtc;

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GY001UserContext CreateNewUserDbContext() =>
            new GY001UserContext(Options.UserDbOptions);

        /// <summary>
        /// 创建模板数据库上下文对象。
        /// 调用者需要自行负责清理对象。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GY001TemplateContext CreateNewTemplateDbContext() =>
            new GY001TemplateContext(Options.TemplateDbOptions);

        /// <summary>
        /// 获取服务器的信息。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VWorldInfomation GetInfomation() =>
            new VWorldInfomation()
            {
                StartDateTime = StartDateTimeUtc,
                CurrentDateTime = DateTime.UtcNow,
            };

        /// <summary>
        /// 测试指定概率数值是否命中。
        /// </summary>
        /// <param name="val">概率，取值[0,1],大于1则视同1，小于0则视同0,1必定返回true,0必定返回false。</param>
        /// <param name="random"></param>
        /// <returns>true命中，false未命中。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsHit(double val, Random random = null) =>
            (random ?? WorldRandom).NextDouble() < val;

        /// <summary>
        /// 存储当前线程最后的错误信息。
        /// </summary>
        [ThreadStatic]
        private static string _LastErrorMessage;

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

        /// <summary>
        /// 按既定顺序锁定一组对象。
        /// </summary>
        /// <param name="objectes"></param>
        /// <returns></returns>
        static public bool Lock<T>(IList<T> objectes, TimeSpan timeout, out int index) where T : class
        {
            index = -1;
            try
            {
                DateTime now = DateTime.UtcNow; //起始时间
                DateTime end = now + timeout;   //结束时间
                for (int i = 0; i < objectes.Count; i++)
                {
                    T obj = objectes[i];
                    var ts = end - DateTime.UtcNow;
                    if (!Monitor.TryEnter(obj))
                    {
                        index = i;
                        return false;
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {

            }
            return true;
        }

        static public void Unlock<T>(IList<T> objectes, bool pulse = false) where T : class
        {
            for (int i = 0; i < objectes.Count; i++)
            {
                if (pulse)
                    Monitor.Pulse(objectes[i]);
                Monitor.Exit(objectes[i]);
            }
        }
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

            _Services.GetService<BlueprintManager>();
            _Services.GetService<GamePropertyHelper>();
        }

        private void CreateDb(IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILogger<GameHostedService>>();
            try
            {
                var tContext = services.GetRequiredService<GY001TemplateContext>();
                TemplateMigrateDbInitializer.Initialize(tContext);
                logger.LogInformation($"{DateTime.UtcNow}用户数据库已正常升级。");

                var context = services.GetRequiredService<GY001UserContext>();
                MigrateDbInitializer.Initialize(context);
                logger.LogInformation($"{DateTime.UtcNow}用户数据库已正常升级。");
            }
            catch (Exception err)
            {
                logger.LogError(err, $"An error occurred creating the DB.{err.Message}");
            }
        }

    }
}
