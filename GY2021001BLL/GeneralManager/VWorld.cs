using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.GeneralManager;
using GuangYuan.GY001.BLL.Script;
using GuangYuan.GY001.BLL.Specific;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using OW.Game.Item;
using OW.Game.Managers;
using OW.Game.Mission;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OW.Game
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

        /// <summary>
        /// 后台保存数据上下文中最大实体数量。超过这个数量，将重新生成一个新的上下文对象。
        /// 过多的追踪对象不仅占用内存且也耗费cpu扫描更改。
        /// </summary>
        /// <value>默认值:200</value>
        public int ContextMaxEntityCount { get; set; } = 200;
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

        /// <summary>
        /// 产品版本，格式为 主版本号.次要版本号.修正号，修正号仅仅是修复bug等，只要主要和次要版本号一致就是兼容版本。
        /// </summary>
        public string Version { get; internal set; }
    }

    /// <summary>
    /// 游戏世界的服务。目前一个虚拟世界，对应唯一一个本类对象，且一个应用程序域（AppDomain）最多支持一个虚拟世界。
    /// 本质上游戏相关类群使用AOC机制，但不依赖DI，所以，在其所处的应用程序域内，本类的唯一对象（单例）部分的代替了容器。
    /// 这种设计确实去耦不彻底，但是使用起来很方便。
    /// 使用者可以根据情况将本类<see cref="Default"/>加入服务容器，以供其他代码通过<see cref="IServiceProvider"/>使用。
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
        public static VWorld Default => _Default ??= new VWorld(_RootServices, null);

        public readonly DateTime StartDateTimeUtc = DateTime.UtcNow;
        private readonly CancellationTokenSource _CancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// 此标志发出信号表示虚拟世界已经因为某种原因请求退出。
        /// </summary>
        public CancellationTokenSource CancellationTokenSource => _CancellationTokenSource;

        private static readonly Barrier _Barrier = new Barrier(0);

        /// <summary>
        /// 用于多个参与者协调不同阶段的屏障。
        /// <list type="bullet">
        /// <item>0 构建过程中。</item>
        /// <item>1 正常服务。</item>
        /// <item>2 开始结束清理。</item>
        /// <item>3 全部清理结束。</item>
        /// </list>
        /// </summary>
        public static Barrier Barrier => _Barrier;

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

        /// <summary>
        /// 初始化函数。
        /// </summary>
        private void Initialize()
        {
            RequestShutdown = _CancellationTokenSource.Token;
            var logger = Service.GetRequiredService<ILogger<VWorld>>();

            Thread thread = new Thread(SaveTemporaryUserContext)
            {
                IsBackground = false,
                Priority = ThreadPriority.BelowNormal,
            };
            thread.Start();
        }
        #endregion 构造函数

        #region 属性及相关

        #region 复用用户数据库上下文
#if NETCOREAPP5_0_OR_GREATER
//NET5_0_OR_GREATER
#else
        private volatile GameUserContext _TemporaryUserContext;

        private enum DbAction
        {
            Add,
            Update,
            Remove,
            Execute,
        }

        /// <summary>
        /// 数据库工作项队列。
        /// </summary>
        private readonly BlockingCollection<(DbAction, object)> _DbWorkQueue = new BlockingCollection<(DbAction, object)>();

        private GameUserContext TemporaryUserContext
        {
            get
            {
                if (_TemporaryUserContext is null)
                {
                    var newVal = CreateNewUserDbContext();
                    var oldVal = Interlocked.CompareExchange(ref _TemporaryUserContext, CreateNewUserDbContext(), null);
                    if (null != oldVal) //若没有替换
                        newVal.DisposeAsync();
                }
                return _TemporaryUserContext;
            }
        }

        public void AddToUserContext(string sql)
        {
            _DbWorkQueue.Add((DbAction.Execute, sql));
        }

        public void AddToUserContext(IEnumerable<object> collection)
        {
            _DbWorkQueue.Add((DbAction.Add, collection));
        }

        public void RemoveToUserContext(IEnumerable<object> collection)
        {
            _DbWorkQueue.Add((DbAction.Remove, collection));
        }

        public void UpdateToUserContext(IEnumerable<object> collection)
        {
            _DbWorkQueue.Add((DbAction.Update, collection));
        }

        /// <summary>
        /// 后台保存数据库的工作函数。
        /// </summary>
        private void SaveTemporaryUserContext()
        {
            ILogger<VWorld> logger = Service.GetService<ILogger<VWorld>>();
            DateTime dt = DateTime.UtcNow;
            List<(DbAction, object)> innerWorkdItems = new List<(DbAction, object)>();
            (DbAction, object) workItem;
            while (!CancellationTokenSource.IsCancellationRequested)
            {
                innerWorkdItems.Clear();
                try
                {
                    if (_DbWorkQueue.IsCompleted || !_DbWorkQueue.TryTake(out workItem, -1))   //若没能等待获取工作项
                        break;  //容错
                }
                catch (Exception)
                {
                    break;
                }
                innerWorkdItems.Add(workItem);
                if (workItem.Item1 != DbAction.Execute)    //若不是立即执行的命令
                    while (_DbWorkQueue.Count > 0 && _DbWorkQueue.TryTake(out workItem, 0))   //获取所有工作项
                    {
                        innerWorkdItems.Add(workItem);
                        if (workItem.Item1 == DbAction.Execute)   //若遇到一个需要立即执行的命令
                            break;
                    }
                foreach (var item in innerWorkdItems)
                {
                    switch (item.Item1)
                    {
                        case DbAction.Add:
                            TemporaryUserContext.AddRange(item.Item2 as IEnumerable<object>);
                            break;
                        case DbAction.Update:
                            TemporaryUserContext.UpdateRange(item.Item2 as IEnumerable<object>);
                            break;
                        case DbAction.Remove:
                            TemporaryUserContext.RemoveRange(item.Item2 as IEnumerable<object>);
                            break;
                        case DbAction.Execute:
                        default:
                            break;
                    }
                }
                if (OwGameCommandInterceptor.ExecutingCount > 0)   //避免IO过于频繁
                    Thread.Sleep(1000);
                try
                {
                    TemporaryUserContext.SaveChanges();
                }
                catch (Exception err)
                {
                    logger.LogError(err.Message);
                    _TemporaryUserContext?.Dispose();
                    _TemporaryUserContext = null;
                }
                if (innerWorkdItems.Count > 0) //容错
                {
                    workItem = innerWorkdItems[^1];
                    try
                    {
                        if (workItem.Item2 is string sql)   //若是一个需要立即执行的命令
                            TemporaryUserContext.Database.ExecuteSqlRaw(sql);
                    }
                    catch (Exception err)
                    {
                        logger.LogError(err.Message);
                        _TemporaryUserContext?.Dispose();
                        _TemporaryUserContext = null;
                    }
                }
                if (null != _TemporaryUserContext)
                    if (_TemporaryUserContext.ChangeTracker.Entries().Count() > Options.ContextMaxEntityCount)    //若数据较多
                    {
                        _TemporaryUserContext.Dispose();
                        _TemporaryUserContext = null;
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false, true);
                    }
                dt = DateTime.UtcNow;
            }
            //TO DO
            _TemporaryUserContext?.SaveChanges();
            _TemporaryUserContext.Dispose();
            _TemporaryUserContext = null;
        }
#endif
        #endregion 复用用户数据库上下文

        #region 子管理器

        ChatManager _ChatManager;
        /// <summary>
        /// 获取聊天服务器。
        /// </summary>
        public ChatManager ChatManager { get => _ChatManager ??= Service.GetService<ChatManager>(); }

        private GameItemTemplateManager _ItemTemplateManager;
        /// <summary>
        /// 模板管理器。
        /// </summary>
        public GameItemTemplateManager ItemTemplateManager { get => _ItemTemplateManager ??= Service.GetRequiredService<GameItemTemplateManager>(); }

        private GameCharManager _GameCharManager;
        /// <summary>
        /// 角色管理器。
        /// </summary>
        public GameCharManager CharManager { get => _GameCharManager ??= Service.GetRequiredService<GameCharManager>(); }

        private GameCombatManager _CombatManager;
        /// <summary>
        /// 战斗管理器。
        /// </summary>
        public GameCombatManager CombatManager { get => _CombatManager ??= Service.GetRequiredService<GameCombatManager>(); }

        private GameItemManager _GameItemManager;
        /// <summary>
        /// 虚拟事物管理器。
        /// </summary>
        public GameItemManager ItemManager { get => _GameItemManager ??= Service.GetRequiredService<GameItemManager>(); }

        private GameSocialManager _SocialManager;
        /// <summary>
        /// 社交管理器。
        /// </summary>
        public GameSocialManager SocialManager => _SocialManager ??= Service.GetRequiredService<GameSocialManager>();


        private BlueprintManager _BlueprintManager;
        /// <summary>
        /// 资源转换管理器。
        /// </summary>
        public BlueprintManager BlueprintManager { get => _BlueprintManager ??= Service.GetRequiredService<BlueprintManager>(); }

        private GameMissionManager _GameMissionManager;
        /// <summary>
        /// 任务/成就管理器。
        /// </summary>
        public GameMissionManager MissionManager { get => _GameMissionManager ??= Service.GetRequiredService<GameMissionManager>(); }

        private GamePropertyManager _PropertyManager;

        /// <summary>
        /// 属性管理器。
        /// </summary>
        public GamePropertyManager PropertyManager { get => _PropertyManager ??= Service.GetRequiredService<IGamePropertyManager>() as GamePropertyManager; }

        private GameEventsManager _EventsManager;
        /// <summary>
        /// 事件管理器。
        /// </summary>
        public GameEventsManager EventsManager => _EventsManager ??= Service.GetService<GameEventsManager>();

        private GameSchedulerManager _SchedulerManager;
        /// <summary>
        /// 获取任务计划管理器。
        /// </summary>
        public GameSchedulerManager SchedulerManager => _SchedulerManager ??= Service.GetService<GameSchedulerManager>();

        private GameAdminManager _AdminManager;
        /// <summary>
        /// 获取管理员服务。
        /// </summary>
        public GameAdminManager AdminManager => _AdminManager ??= Service.GetService<GameAdminManager>();

        private GameShoppingManager _ShoppingManager;
        /// <summary>
        /// 获取商城服务。
        /// </summary>
        public GameShoppingManager ShoppingManager => _ShoppingManager ??= Service.GetService<GameShoppingManager>();

        GameScriptManager _ScriptManager;
        /// <summary>
        /// 脚本服务。
        /// </summary>
        public GameScriptManager ScriptManager => _ScriptManager ??= Service.GetService<GameScriptManager>();

        GamePropertyChangeManager _PropertyChangeManager;

        /// <summary>
        /// 属性变化管理器。
        /// </summary>
        public GamePropertyChangeManager PropertyChangeManager => _PropertyChangeManager ??= Service.GetService<GamePropertyChangeManager>();

        GameAllianceManager _AllianceManager;
        /// <summary>
        /// 联盟/工会管理器。
        /// </summary>
        public GameAllianceManager AllianceManager { get => _AllianceManager ??= Service.GetService<GameAllianceManager>(); }

        private VirtualThingManager _VirtualThingManager;

        /// <summary>
        /// 虚拟事物管理器。
        /// </summary>
        public VirtualThingManager VirtualThingManager
        {
            get => _VirtualThingManager ??= Service.GetService<VirtualThingManager>();
        }

        #endregion 子管理器

        #region 对象池

        /// <summary>
        /// 
        /// </summary>
        private class ListGameItemPolicy : PooledObjectPolicy<List<GameItem>>
        {
            public ListGameItemPolicy()
            {
            }

            public override List<GameItem> Create() =>
                new List<GameItem>();

            public override bool Return(List<GameItem> obj)
            {
                obj.Clear();
                return true;
            }
        }

        private ObjectPool<List<GameItem>> _ObjectPoolListGameItem;

        public ObjectPool<List<GameItem>> ObjectPoolListGameItem
        {
            get
            {
                if (null == _ObjectPoolListGameItem)
                {
                    lock (ThisLocker)
                        _ObjectPoolListGameItem ??= (Service.GetService<ObjectPool<List<GameItem>>>() ??
                            new DefaultObjectPool<List<GameItem>>(new ListGameItemPolicy(), Environment.ProcessorCount * 8));
                }
                return _ObjectPoolListGameItem;
            }
        }

        /// <summary>
        /// </summary>
        private class StringObjectDictionaryPolicy : PooledObjectPolicy<Dictionary<string, object>>
        {
            public override Dictionary<string, object> Create()
            {
                return new Dictionary<string, object>();
            }

            public override bool Return(Dictionary<string, object> obj)
            {
                obj.Clear();
                return true;
            }
        }

        private ObjectPool<Dictionary<string, object>> _StringObjectDictionaryPool;

        /// <summary>
        /// 使用该属性可以避免不必要的内存分配和释放，使用此对象池的前提是满足以下所有条件：
        /// 该字典会在一定周期内返回池，最好是一个数据包处理过程中临时用到的字典。
        /// 使用非常频繁。
        /// </summary>
        public ObjectPool<Dictionary<string, object>> StringObjectDictionaryPool
        {
            get
            {
                if (_StringObjectDictionaryPool is null)
                    lock (ThisLocker)
                    {
                        _StringObjectDictionaryPool ??= (Service.GetService<ObjectPool<Dictionary<string, object>>>()   //获取服务提供的对象池
                            ?? new DefaultObjectPool<Dictionary<string, object>>(new StringObjectDictionaryPolicy(), Environment.ProcessorCount * 8));    //若服务没有提供能自己初始化
                    }
                return _StringObjectDictionaryPool;
            }
        }

        #endregion 对象池

        #region 随机数相关

        /// <summary>
        /// 公用随机数生成器。
        /// </summary>
        [ThreadStatic]
        private static Random _WorldRandom;

        /// <summary>
        /// 取当前线程的随机种子。
        /// 可以并发调用。
        /// </summary>
        public static Random WorldRandom => _WorldRandom ??= new Random();

        /// <summary>
        /// 该节点号。
        /// </summary>
        public int? NodeNumber { get; internal set; } = 1;

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
                Version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion,
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

        #region 错误处理

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
        public static string GetLastErrorMessage()
        {
            if (_LastErrorMessage is null)
            {
                try
                {
                    _LastErrorMessage = new Win32Exception(_LastError).Message;
                }
                catch (Exception)
                {
                }
            }
            return _LastErrorMessage;
        }

        /// <summary>
        /// 设置最后错误信息。
        /// </summary>
        /// <param name="msg"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetLastErrorMessage(string msg) => _LastErrorMessage = msg;

        [ThreadStatic]
        private static int _LastError;

        /// <summary>
        /// 获取最后发生错误的错误码。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLastError() => _LastError;

        /// <summary>
        /// 设置最后一次错误的错误码。
        /// </summary>
        /// <param name="errorCode"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetLastError(int errorCode)
        {
            _LastError = errorCode;
            _LastErrorMessage = null;
        }

        #endregion 错误处理

        #region 锁定字符串

        private static readonly ConcurrentDictionary<string, WeakReference<string>> _StringPool = new ConcurrentDictionary<string, WeakReference<string>>();

        /// <summary>
        /// 从字符串拘留池中取出实力并试图锁定。
        /// 按每个字符串平均占用64字节计算，10万个字符串实质占用6.4MB内存,可以接受。
        /// </summary>
        /// <param name="str">如果暂存了 str，则返回系统对其的引用；否则返回对值为 str 的字符串的新引用。</param>
        /// <param name="timeout">用于等待锁的时间。 值为 -1 毫秒表示指定无限期等待。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool LockString([NotNull] ref string str, TimeSpan timeout) =>
            LockString(ref str, (int)timeout.TotalMilliseconds);

        /// <summary>
        /// 从字符串拘留池中取出实力并试图锁定。
        /// 按每个字符串平均占用64字节计算，10万个字符串实质占用6.4MB内存,可以接受。
        /// </summary>
        /// <param name="str">如果暂存了 str，则返回系统对其的引用；否则返回对值为 str 的字符串的新引用。</param>
        /// <param name="timeout">等待锁所需的毫秒数。</param>
        /// <returns>如果当前线程获取该锁，则为 true；否则为 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool LockString([NotNull] ref string str, int timeout = -1)
        {
            var tmp = string.Intern(str);
            str = tmp;
            var result = Monitor.TryEnter(tmp, timeout);
            if (!result)
                SetLastError(ErrorCodes.WAIT_TIMEOUT);
            return result;
        }

        /// <summary>
        /// 释放指定对象上的排他锁。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="isPulse">是否通知等待队列中的线程锁定对象状态的更改。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnlockString(string str, bool isPulse = false)
        {
            if (isPulse)
                Monitor.Pulse(str);
#if DEBUG
            if (string.IsInterned(str) is null)
                throw new SynchronizationLockException();
#endif
            Monitor.Exit(str);
        }

        /// <summary>
        /// 锁定字符串使用的结构。键是(比较方法,区域),值字符串值。
        /// </summary>
        ConcurrentDictionary<(StringComparer, string), HashSet<string>> _StringLocker = new ConcurrentDictionary<(StringComparer, string), HashSet<string>>();

        /// <summary>
        /// 锁定指定的字符串，并返回实际锁定的实例。
        /// </summary>
        /// <param name="str">锁定的字符串，返回时是一个值等价的字符串，但锁加在该唯一实例(在指定的区域范围内)上。</param>
        /// <param name="region">区域范围，每个区域范围内值相等的字符串是唯一的。区分大小写。</param>
        /// <param name="timeout">不可以是空null,但可以是空字符串<see cref="string.Empty"/></param>
        /// <returns></returns>
        public virtual bool LockString(ref string str, string region, StringComparer comparer, TimeSpan timeout)
        {
            var hs = _StringLocker.GetOrAdd((comparer, region), c => new HashSet<string>(comparer));
            lock (hs)
                if (hs.TryGetValue(str, out str))
                    return true;
                else
                {
                    return hs.Add(str);
                }
        }

        ConcurrentDictionary<(StringComparer, string, string), string> _StringDic = new ConcurrentDictionary<(StringComparer, string, string), string>();

        public string GetUniString(string str, string region, StringComparer comparer)
        {
            return _StringDic.GetOrAdd((comparer, region, str), c => c.Item3);
        }

        public bool UnregUniString(string str, string region, StringComparer comparer)
        {
            return _StringDic.TryRemove((comparer, region, str), out _);
        }
        #endregion 锁定字符串

        #region 功能

        /// <summary>
        /// 获取全服推关战力排名前n位成员。
        /// </summary>
        /// <param name="topN">前多少位的排名。过大的值将导致缓慢，设计时考虑100左右。</param>
        /// <returns></returns>
        public IList<(Guid, decimal, string)> GetRankOfTuiguanQuery(int topN)
        {
            using var db = CreateNewUserDbContext();
            var coll = from slot in db.Set<GameItem>()
                       where slot.ExtraGuid == ProjectConstant.TuiGuanTId
                       join parent in db.Set<GameItem>()
                       on slot.ParentId equals parent.Id
                       join gc in db.Set<GameChar>()
                       on parent.OwnerId equals gc.Id
                       select new { gc.Id, gc.DisplayName, slot.ExtraDecimal.Value };
            var result = coll.AsNoTracking().OrderByDescending(c => c.Value).Take(topN).AsEnumerable().Select(c => (c.Id, c.Value, c.DisplayName));
            return result.ToList();
        }
        #endregion 功能
    }

    public static class VWorldExtensions
    {
        /// <summary>
        /// 锁定指定字符串在字符串拘留池中的实例。
        /// </summary>
        /// <param name="world"></param>
        /// <param name="str">返回时指向字符串拘留池中的实例。</param>
        /// <param name="timeout"></param>
        /// <param name="isPulse">在解锁是是否发出脉冲信号。</param>
        /// <returns>解锁的包装,通过<seealso cref="DisposerWrapper.Create(Action)"/>创建，如果没有成功锁定则为null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable LockStringAndReturnDisposer(this VWorld world, ref string str, TimeSpan timeout, bool isPulse = false)
        {
            if (!world.LockString(ref str, timeout))
                return null;
            var tmp = str;
            return DisposerWrapper.Create(() => world.UnlockString(tmp, isPulse));
        }

    }
}
