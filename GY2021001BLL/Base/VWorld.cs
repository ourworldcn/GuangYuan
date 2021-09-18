using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

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

    public static class ErrorCodes
    {
        public const int NO_ERROR = 0;
        public const int WAIT_TIMEOUT = 258;
        public const int ERROR_INVALID_TOKEN = 315;
        public const int ERROR_NO_SUCH_USER = 1317;
        /// <summary>
        /// 并发或交错操作更改了对象的状态，使此操作无效。
        /// </summary>
        public const int E_CHANGED_STATE = unchecked((int)0x8000000C);
        public const int Unauthorized = unchecked((int)0x80190191);
        public const int RO_E_CLOSED = unchecked((int)0x80000013);
        public const int ObjectDisposed = RO_E_CLOSED;

        /// <summary>
        /// 参数错误。
        /// </summary>
        public const int ERROR_BAD_ARGUMENTS = 160;

        /// <summary>
        /// 没有足够资源完成操作。
        /// </summary>
        public const int RPC_S_OUT_OF_RESOURCES = 1721;

        /// <summary>
        /// 没有足够的配额来处理此命令。通常是超过某些次数的限制。
        /// </summary>
        public const int ERROR_NOT_ENOUGH_QUOTA = 1816;
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

        #endregion 构造函数

        #region 属性及相关

        #region 复用用户数据库上下文
#if NETCOREAPP5_0_OR_GREATER
//NET5_0_OR_GREATER

        GameUserContext _UserContext;

        /// <summary>
        /// 一个公用的数据库上下文。
        /// 特别地！使用之前必须锁定该对象，且一旦释放，应假设有后台线程会立即试图保存，然后清空所有跟踪对象！
        /// 对此对象调用<see cref="Monitor.PulseAll(object)"/>可以加速保存线程获取锁。
        /// </summary>
        public GameUserContext UserContext
        {
            get
            {
                if (_UserContext is null)
                    lock (this)
                        if (_UserContext is null)
                        {
                            _UserContext = CreateNewUserDbContext();
                            Thread thread = new Thread(DbSaveFunc)
                            {
                                IsBackground = false,
                                Priority = ThreadPriority.Lowest
                            };
                            thread.Start();
                        }
                return _UserContext;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        private void DbSaveFunc(object obj)
        {
            var ct = CancellationTokenSource.Token;
            var logger = Services.GetService<ILogger<VWorld>>();
            logger.LogDebug($"[{DateTime.UtcNow:s}]DbSaveFunc启动");
            lock (_UserContext)
            {
                while (!CancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        if (Monitor.Wait(_UserContext, 2000) /*|| IsHit(0.2)*/)
                        {
                            var xx = _UserContext.Set<GameItem>().FirstOrDefault();
                            _UserContext.SaveChanges();
                            ClearDb(_UserContext);
                        }
                    }
                    catch (Exception err)
                    {
                        logger.LogError($"公用用户数据库上下文保存数据时出错——{err.Message}");
                    }
                }
            }
        }

        void ClearDb(DbContext db)
        {
            var coll = db.ChangeTracker.Entries().ToArray();
            foreach (var item in coll)
            {
                var obj = db.Entry(item.Entity);
                obj.State = EntityState.Detached;
            }
            db.SaveChanges();
        }
#else
        private readonly object _TemporaryUserContextLocker = new object();
        private volatile GameUserContext _TemporaryUserContext;

        private GameUserContext TemporaryUserContext
        {
            get
            {
                if (_TemporaryUserContext is null)
                    lock (_TemporaryUserContextLocker)
                        if (_TemporaryUserContext is null)
                            _TemporaryUserContext = CreateNewUserDbContext();
                return _TemporaryUserContext;
            }
        }

        public void AddToUserContext(IEnumerable<object> collection)
        {
            lock (_TemporaryUserContextLocker)
            {
                TemporaryUserContext.AddRange(collection);
                Monitor.Pulse(_TemporaryUserContextLocker);
            }
        }

        public void RemoveToUserContext(IEnumerable<object> collection)
        {
            lock (_TemporaryUserContextLocker)
            {
                TemporaryUserContext.RemoveRange(collection);
                Monitor.Pulse(_TemporaryUserContextLocker);
            }
        }

        public void UpdateToUserContext(IEnumerable<object> collection)
        {
            lock (_TemporaryUserContextLocker)
            {
                TemporaryUserContext.UpdateRange(collection);
                Monitor.Pulse(_TemporaryUserContextLocker);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void SaveTemporaryUserContext()
        {
            ILogger<VWorld> logger = Service.GetService<ILogger<VWorld>>();
            DateTime dt = DateTime.UtcNow;
            lock (_TemporaryUserContextLocker)
                while (!CancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var waitSucc = Monitor.Wait(_TemporaryUserContextLocker, 1000);
                        if (_TemporaryUserContext is null)  //若没有数据
                        {
                            dt = DateTime.UtcNow;
                            continue;
                        }
                        if (DateTime.UtcNow - dt > TimeSpan.FromSeconds(1) || waitSucc && OwGameCommandInterceptor.ExecutingCount <= 0)    //若超过1s,避免过于频繁的保存
                        {
                            _TemporaryUserContext.SaveChanges();
                            if (_TemporaryUserContext.ChangeTracker.Entries().Count() > 200)    //若数据较多
                            {
                                _TemporaryUserContext.Dispose();
                                _TemporaryUserContext = null;
                                GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false, true);
                            }
                        }
                        dt = DateTime.UtcNow;
                    }
                    catch (Exception err)
                    {
                        logger.LogError(err.Message);
                        _TemporaryUserContext?.Dispose();
                        _TemporaryUserContext = null;
                    }
                }
            lock (_TemporaryUserContextLocker)
            {
                _TemporaryUserContext?.SaveChanges();
                _TemporaryUserContext.Dispose();
                _TemporaryUserContext = null;
            }
        }
#endif
        #endregion 复用用户数据库上下文

        private GameItemTemplateManager _ItemTemplateManager;
        public GameItemTemplateManager ItemTemplateManager { get => _ItemTemplateManager ??= Service.GetRequiredService<GameItemTemplateManager>(); }

        private GameCharManager _GameCharManager;
        public GameCharManager CharManager { get => _GameCharManager ??= Service.GetRequiredService<GameCharManager>(); }

        private CombatManager _CombatManager;
        public CombatManager CombatManager { get => _CombatManager ??= Service.GetRequiredService<CombatManager>(); }

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
            var logger = Service.GetRequiredService<ILogger<VWorld>>();
            logger.LogInformation("初始化完毕，开始服务。");

            Thread thread = new Thread(SaveTemporaryUserContext)
            {
                IsBackground = false,
                Priority = ThreadPriority.Lowest,
            };
            thread.Start();
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

        #endregion 锁定字符串
    }

    /// <summary>
    /// 
    /// </summary>
    public class ListGameItemPolicy : PooledObjectPolicy<List<GameItem>>
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
