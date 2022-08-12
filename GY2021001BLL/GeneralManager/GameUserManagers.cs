using GuangYuan.GY001.BLL.Specific;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using OW.Extensions.Game.Store;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace GuangYuan.GY001.BLL
{

    /// <summary>
    /// GameCharManager类的配置类。
    /// </summary>
    public class GameCharManagerOptions
    {
        public GameCharManagerOptions()
        {

        }

        /// <summary>
        /// 默认锁定超时。单位：秒
        /// 在指定时间内无法锁定对象就返回失败。
        /// 作为工程产品，这个可以避免死锁，务必不要是一个太大的值。
        /// </summary>
        /// <value>默认值:3秒。</value>
        public TimeSpan DefaultLockTimeout { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// 扫描超时不动用户强制注销的频率。
        /// </summary>
        /// <value>默认值：1分钟。</value>
        public TimeSpan ScanFrequencyOfLogout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 登录用户多长时间不动作，会被视同登出。
        /// </summary>
        /// <value>默认值:15分钟。</value>
        public TimeSpan LogoutTimeout { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// 系统因某种原因自动加载进缓存的用户对象，多长时间不使用，会自动驱逐。
        /// </summary>
        public TimeSpan LogoutTimeoutForAutoLoad { get; set; } = TimeSpan.FromMinutes(1);

    }

    public class GameCharManager : GameManagerBase<GameCharManagerOptions>
    {
        private class GameUserStore : IDisposable
        {
            public GameUserStore()
            {
            }

            //private readonly IServiceProvider _Service;
            //private VWorld World;

            //public VWorld World => World ??= _Service.GetService<VWorld>();

            internal ConcurrentDictionary<Guid, GameUser> _Token2Users = new ConcurrentDictionary<Guid, GameUser>();
            internal ConcurrentDictionary<string, GameUser> _LoginName2Users = new ConcurrentDictionary<string, GameUser>();
            /// <summary>
            /// 键是用户对象Id,值用户对象。
            /// </summary>
            internal ConcurrentDictionary<Guid, GameUser> _Id2Users = new ConcurrentDictionary<Guid, GameUser>();
            internal ConcurrentDictionary<Guid, GameChar> _Id2GChars = new ConcurrentDictionary<Guid, GameChar>();

            /// <summary>
            /// 在线角色。键是角色Id，值角色对象。
            /// </summary>
            internal ConcurrentDictionary<Guid, GameChar> _Id2OnlineChars = new ConcurrentDictionary<Guid, GameChar>();

            private bool _IsDisposed;

            /// <summary>
            /// 向内存缓存加入用户对象。
            /// </summary>
            /// <param name="user"></param>
            /// <returns></returns>
            public bool Add(GameUser user)
            {
                bool succ = _Token2Users.TryAdd(user.CurrentToken, user);
                succ = _LoginName2Users.TryAdd(user.LoginName, user) && succ;
                succ = _Id2Users.TryAdd(user.Id, user) && succ;
                if (null != user.CurrentChar)   //若有当前用户
                    succ = _Id2GChars.TryAdd(user.CurrentChar.Id, user.CurrentChar) && succ;
                if (!succ)
                {
                    _Token2Users.TryRemove(user.CurrentToken, out _);
                    _LoginName2Users.TryRemove(user.LoginName, out _);
                    _Id2Users.TryRemove(user.Id, out _);
                    _Id2GChars.TryRemove(user.CurrentChar.Id, out _);
                }
                return succ;
            }

            /// <summary>
            /// 清理数据，自动标记不在线。
            /// </summary>
            /// <param name="user"></param>
            /// <returns></returns>
            public bool Remove(GameUser user)
            {
                bool succ = _Token2Users.TryRemove(user.CurrentToken, out _);
                succ = _LoginName2Users.TryRemove(user.LoginName, out _) && succ;
                succ = _Id2Users.TryRemove(user.Id, out _) && succ;
                succ = _Id2GChars.TryRemove(user.CurrentChar.Id, out _) && succ;
                _Id2OnlineChars.TryRemove(user.CurrentChar.Id, out _);
                if (!succ)
                {
                    _Token2Users.TryAdd(user.CurrentToken, user);
                    _LoginName2Users.TryAdd(user.LoginName, user);
                    _Id2Users.TryAdd(user.Id, user);
                    _Id2GChars.TryAdd(user.CurrentChar.Id, user.CurrentChar);
                }
                return succ;
            }

            /// <summary>
            /// 变换令牌。
            /// </summary>
            /// <param name="user"></param>
            /// <param name="oldToken">旧令牌</param>
            /// <returns></returns>
            public GameUser ChangeToken(GameUser user, Guid oldToken)
            {
                _Token2Users.TryRemove(oldToken, out _);
                return _Token2Users.AddOrUpdate(user.CurrentToken, user, (p1, p2) => user);
            }

            #region IDisposable接口及相关

            protected virtual void Dispose(bool disposing)
            {
                if (!_IsDisposed)
                {
                    if (disposing)
                    {
                        // 释放托管状态(托管对象)
                    }

                    // 释放未托管的资源(未托管的对象)并重写终结器
                    // 将大型字段设置为 null
                    _Token2Users = null;
                    _LoginName2Users = null;
                    _Id2Users = null;
                    _Id2GChars = null;
                    _Id2OnlineChars = null;

                    _IsDisposed = true;
                }
            }

            // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
            // ~GameUserStore()
            // {
            //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            //     Dispose(disposing: false);
            // }

            public void Dispose()
            {
                // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
            #endregion IDisposable接口及相关
        }

        public const string FriendRobotLoginName = "A3B4B3F7-8CAD-4B95-9983-4D5DAFDAA5F0";

        /// <summary>
        /// 获取一个新的不重复的昵称。
        /// </summary>
        /// <param name="gameUser"></param>
        /// <returns></returns>
        public string GetNewDisplayName(GameUser gameUser)
        {
            return gameUser.LoginName;
        }

        /// <summary>
        /// 设置好友搜索优先度。
        /// </summary>
        /// <param name="gameChar">角色对象。</param>
        /// <param name="dateTime">时间。</param>
        public void SetSearchFriendPriority(GameChar gameChar, DateTime dateTime)
        {
            using var dw = World.CharManager.LockAndReturnDisposer(gameChar.GameUser);
            if (dw is null)
                throw new TimeoutException("无法锁定用户对象");
            var slot = gameChar.GetFriendSlot();
            var lv = (int)gameChar.Properties.GetDecimalOrDefault(World.PropertyManager.LevelPropertyName);
            var str = $"{lv:d4}{dateTime:s}";
            if (str != slot.ExtraString)
            {
                slot.ExtraString = str;
                World.CharManager.NotifyChange(gameChar.GameUser);
            }
        }

        #region 唯一名称相关

        //public void GetUniqueName(out string loginName, out string displayName)
        //{

        //}

        #endregion 唯一名称相关

        #region 字段

        /// <summary>
        /// 使用多个字典索引共同的存储对象。
        /// </summary>
        private readonly GameUserStore _Store = new GameUserStore();

        private bool _QuicklyRegisterSuffixSeqInit = false;
        private int _QuicklyRegisterSuffixSeq;
        private Timer _LogoutTimer;

        /// <summary>
        /// 提示一些对象已经处于"脏"状态，后台线程应尽快保存。
        /// </summary>
        private readonly ConcurrentDictionary<GameUser, GameUser> _DirtyUsers = new ConcurrentDictionary<GameUser, GameUser>();

        /// <summary>
        /// 保存数据的线程。
        /// </summary>
        private Thread _SaveThread;

        /// <summary>
        /// 取所有在线玩家的字典，键是Id，值是在线对象（瞬态）。
        /// </summary>
        public IReadOnlyDictionary<Guid, GameChar> Id2GameChar { get => _Store._Id2GChars; }

        #endregion 字段

        #region 构造函数

        /// <summary>
        /// 默认构造函数。
        /// </summary>
        public GameCharManager()
        {
            Initialize();
        }

        /// <summary>
        /// 依赖注入使用的构造函数。
        /// </summary>
        /// <param name="serviceProvider"></param>
        public GameCharManager(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Initialize();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="options"></param>
        public GameCharManager(IServiceProvider serviceProvider, GameCharManagerOptions options) : base(serviceProvider, options)
        {
            Initialize();
        }

        private void Initialize()
        {
            VWorld world = World;
            world.AddToUserContext("UPDATE [dbo].[GameUsers] SET [NodeNum] = null WHERE [NodeNum] in (0,1)");  //复位服务器节点号
            _LogoutTimer = new Timer(LogoutFunc, null, Options.ScanFrequencyOfLogout, Options.ScanFrequencyOfLogout);
            _SaveThread = new Thread(SaveFunc) { IsBackground = false, Priority = ThreadPriority.BelowNormal };
            _SaveThread.Start();
            world.RequestShutdown.Register(() =>
            {
                _LogoutTimer.Dispose();
            });
        }

        #endregion 构造函数

        #region 私有方法

        /// <summary>
        /// 创建一个新账号加入缓存，然后锁定后返回。
        /// 请使用<see cref="Unlock(GameUser, bool)"/>解锁。
        /// 创建者需要保证登录名不会重复，否则无法保存。
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public GameUser CreateNewUserAndLock(string loginName, string pwd)
        {
            var gu = new GameUser();
            World.EventsManager.GameUserCreated(gu, loginName, pwd, null, null);
            if (!Lock(gu))
                return null;
            _Store.Add(gu);
            if (gu.CurrentChar != null)
                SetSearchFriendPriority(gu.CurrentChar, DateTime.UtcNow);
            return gu;
        }

        /// <summary>
        /// 后台扫描超时不动该强制注销的用户并注销之。
        /// 周期性调用此函数。
        /// </summary>
        private void LogoutFunc(object state)
        {
            DateTime start = DateTime.UtcNow;
            VWorld world = World;
            if (Environment.HasShutdownStarted || world.RequestShutdown.IsCancellationRequested)
                return;
            var succCount = 0;
            foreach (var item in _Store._Token2Users.Values)
            {
                if (DateTime.UtcNow - start >= Options.ScanFrequencyOfLogout)    //若本次扫描已经超时
                    break;
                var loginName = item.LoginName;
                using var dwLoginName = world.LockStringAndReturnDisposer(ref loginName, TimeSpan.Zero);
                if (dwLoginName is null) //若锁定用户名不成功
                {
                    if (Environment.HasShutdownStarted || world.RequestShutdown.IsCancellationRequested)
                        break;
                    Thread.Yield();
                    continue;
                }
                try
                {
                    using var dwUser = this.LockAndReturnDisposer(item, TimeSpan.Zero);  //锁定用户
                    if (dwUser is null)    //若锁定用户失败
                    {
                        if (Environment.HasShutdownStarted || world.RequestShutdown.IsCancellationRequested)
                            break;
                        Thread.Yield();
                        continue;
                    }
                    if (DateTime.UtcNow - item.LastModifyDateTimeUtc >= item.Timeout)   //若超时
                    {
                        Logout(item, LogoutReason.Timeout); //注销
                        succCount++;
                    }
                    if (Environment.HasShutdownStarted || world.RequestShutdown.IsCancellationRequested)
                        break;
                }
                catch (Exception err)
                {
                    var logger = Service.GetRequiredService<ILogger<GameCharManager>>();
                    logger.LogError($"{err.Message}{Environment.NewLine}@{err.StackTrace}");
                }
                Thread.Yield();
            }
            if (!Environment.HasShutdownStarted && !world.RequestShutdown.IsCancellationRequested && World.IsHit((double)succCount / 5))
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            }
        }

        /// <summary>
        /// 后台保存数据的工作函数。当游戏服务终止时负责保存所有数据。
        /// </summary>
        /// <param name="state"></param>
        private void SaveFunc(object state)
        {
            var adminLoginName = "1D22F0CF-1704-412C-AD8D-32CE5FA5A7D5";
            var adminPwd = "37A87267-77DC-4AA3-8220-02F9538779AB";
            var adminCharId = new Guid("38D81816-E81E-4C18-93C9-2168DD77EED1");
            VWorld world = World;
            //复位超管账号
            using (var dwAdmin = world.CharManager.LockOrLoad(adminLoginName, out var adminUser))
                if (dwAdmin is null) //若未建立超管账号
                {
                    adminUser = world.CharManager.CreateNewUserAndLock(adminLoginName, adminPwd);
                    try
                    {
                        adminUser.CurrentChar.CharType |= CharType.SuperAdmin;
                        world.CharManager.NotifyChange(adminUser);
                    }
                    finally
                    {
                        world.CharManager.Unlock(adminUser);
                    }
                }
                else
                {
                    adminUser.CurrentChar.CharType |= CharType.SuperAdmin;
                    world.CharManager.NotifyChange(adminUser);
                }
            //复位加好友机器人
            var friendRobotPwd = "9D4C2381-F000-43F2-AB75-1A53718D44C0";
            var friendRobotCharId = new Guid("{1D860E58-74C9-4442-A3B4-A2406DA9566A}");
            using (var dwRobot = world.CharManager.LockOrLoad(FriendRobotLoginName, out var robotUser))
                if (dwRobot is null) //若未建立机器人账号
                {
                    robotUser = world.CharManager.CreateNewUserAndLock(FriendRobotLoginName, friendRobotPwd);
                    try
                    {
                        robotUser.CurrentChar.CharType |= CharType.Robot;
                        world.CharManager.NotifyChange(robotUser);
                    }
                    finally
                    {
                        world.CharManager.Unlock(robotUser);
                    }
                }
                else
                {
                    robotUser.CurrentChar.CharType |= CharType.Robot;
                    world.CharManager.NotifyChange(robotUser);
                }
            while (true)
            {
                foreach (var key in _DirtyUsers.Keys)
                {
                    try
                    {
                        if (!_DirtyUsers.TryRemove(key, out var item))
                            continue;
                        using var dwUser = this.LockAndReturnDisposer(item, TimeSpan.Zero);    //锁定用户
                        if (dwUser is null) //若锁定失败
                        {
                            if (!item.IsDisposed)   //若没有处置
                            {
                                _DirtyUsers[item] = item;   //加入准备下次再写入
                                Thread.Yield();
                            }
                            continue;
                        }
                        item.DbContext.SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException err)
                    {
                        Trace.WriteLine($"保存数据时出现未知错误——{err.Message}");
                    }
                    catch (Exception err)
                    {
                        Trace.WriteLine($"保存数据时出现未知错误——{err.Message}");
                        //var coll = OwHelper.GetAllSubItemsOfTree(item.CurrentChar.GameItems, c => c.Children);
                        //var coll2 = coll.Where(c => c.Number == Guid.Empty).ToArray();
                    }
                    Thread.Yield();
                }
                try
                {
                    if (world.RequestShutdown.IsCancellationRequested)
                        break;
                    if (world.RequestShutdown.WaitHandle.WaitOne(2000))
                        break;
                }
                catch (Exception)
                {
                    break;
                }
            }
            //服务终止
            Thread.CurrentThread.Priority = ThreadPriority.Normal;
            _DirtyUsers.Clear();
            while (!_Store._Token2Users.IsEmpty)   //把所有还在线的用户强制注销
                foreach (var item in _Store._Token2Users.Values)
                {
                    try
                    {
                        Logout(item, LogoutReason.SystemShutdown);
                    }
                    catch (Exception)
                    {
                    }
                }
        }

        #endregion 私有方法

        #region 公共属性

        /// <summary>
        /// 在线角色字典，键是角色Id,值是角色对象。
        /// </summary>
        public IReadOnlyDictionary<Guid, GameChar> Id2OnlineChar => _Store._Id2OnlineChars;

        private GameItemTemplateManager _ItemTemplateManager;

        /// <summary>
        /// 虚拟事物模板管理器。
        /// </summary>
        public GameItemTemplateManager ItemTemplateManager => _ItemTemplateManager ??= World.ItemTemplateManager;

        #endregion 公共属性

        #region 公共方法

        #region 通过索引获取对象

        /// <summary>
        /// 用指定的Id获取角色对象。仅是在内存中的对象。
        /// </summary>
        /// <param name="id"></param>
        /// <returns>角色对象。如果没有找到则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameChar GetCharFromId(Guid id)
        {
            return _Store._Id2GChars.GetValueOrDefault(id, null);
        }

        /// <summary>
        /// 获取指定令牌的用户对象。
        /// </summary>
        /// <param name="token">令牌。</param>
        /// <returns>用户对象，如果无效则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameUser GetUserFromToken(Guid token)
        {
            var result = _Store._Token2Users.GetValueOrDefault(token, null);
            if (result is null) //若令牌无效
                VWorld.SetLastError(ErrorCodes.ERROR_INVALID_TOKEN);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginName"></param>
        /// <returns>用户对象，如果无效则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameUser GetUserFromLoginName(string loginName) =>
            _Store._LoginName2Users.GetValueOrDefault(loginName, null);

        /// <summary>
        /// 获取已经在内存中的用户对象。
        /// </summary>
        /// <param name="id">用户对象的Id。</param>
        /// <returns>用户对象，如果无效则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameUser GetUserFromId(Guid id) =>
            _Store._Id2Users.GetValueOrDefault(id, null);

        /// <summary>
        /// 获取指定Id的角色是否在线。
        /// </summary>
        /// <param name="charId"></param>
        /// <returns>是否在线。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOnline(Guid charId) =>
            _Store._Id2OnlineChars.ContainsKey(charId);

        /// <summary>
        /// 获取指定的对象信息。
        /// </summary>
        /// <param name="datas"></param>
        public void GetItems(GetItemsContext datas)
        {
            IDisposable disposable;
            GameChar gc = null;
            if (datas.CharId is null)    //若取自身对象
            {
                disposable = datas.LockUser();
                if (null != disposable)
                    gc = datas.GameChar;
            }
            else //若取指定角色对象
            {
                disposable = LockOrLoad(datas.CharId.Value, out var gu);
                if (null != disposable && null != gu)
                    gc = gu.CurrentChar;
            }
            if (disposable is null)
            {
                datas.FillErrorFromWorld();
                return;
            }
            using var dw = disposable;
            if (datas.CharId is null)    //若取自身对象
            {
                var coll = gc.AllChildren.Join(datas.Ids, c => c.Id, c => c, (l, r) => l);  //自身对象
                datas.GameItems.AddRange(coll);
                if (datas.Ids.Contains(gc.Id))   //若取角色对象
                    datas.ResultGameChar = gc;
            }
            else //若取指定角色对象
            {
                var tid2gis = gc.AllChildren.ToLookup(c => c.ExtraGuid, c => c);
                var coll = tid2gis.Join(datas.Ids, c => c.Key, c => c, (l, r) => l).SelectMany(c => c);
                datas.GameItems.AddRange(coll);
                if (datas.Ids.Contains(ProjectConstant.CharTemplateId))   //若取角色对象
                    datas.ResultGameChar = gc;
            }
        }

        #endregion 通过索引获取对象

        #region 锁定对象

        /// <summary>
        /// 锁定登入登出进程。
        /// </summary>
        /// <param name="loginName">如果暂存了 loginName，则返回系统对其的引用；否则返回对值为 loginName 的字符串的新引用。</param>
        /// <param name="timeout">用于等待锁的时间。 值为 -1 毫秒表示指定无限期等待。省略或为null，则使用 Options.DefaultLockTimeout 给定的值作为超时时间。</param>
        /// <returns></returns>
        public bool LockLogin(ref string loginName, TimeSpan? timeout = null)
        {
            return StringLocker.TryEnter(ref loginName, timeout ?? Options.DefaultLockTimeout);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="isPulse">是否通知等待队列中的线程锁定对象状态的更改。false或默认值表示不通知。</param>
        public void UnlockLogin(string loginName, bool isPulse = false)
        {
            //TODO 
            //if(isPulse)
            //    Monitor.PulseAll()
            StringLocker.Exit(loginName);
        }

        /// <summary>
        /// 锁定用户。务必要用<seealso cref="Unlock(GameUser)"/>解锁。两者配对使用。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="timeout">超时时间,单位:毫秒。-1(默认值)表示一直等待。</param>
        /// <returns>true该用户已经锁定且有效。false锁定超时或用户已经无效。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Lock([NotNull] GameUser user, int timeout = Timeout.Infinite) =>
            Lock(user, TimeSpan.FromMilliseconds(timeout));

        /// <summary>
        /// 锁定用户。务必要用<seealso cref="Unlock(GameUser, bool)"/>解锁。两者配对使用。
        /// 派生类可以重载此方法。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="timeout">超时时间。-1毫秒是永久等待(<seealso cref="Timeout.InfiniteTimeSpan"/>),
        /// 省略或为null是使用配置中(Options.DefaultLockTimeout)，默认的超时值锁定。</param>
        /// <returns>true该用户已经锁定且有效。false锁定超时或用户已经无效。详细信息可通过<seealso cref="VWorld.GetLastError"/>获取。</returns>
        public virtual bool Lock([NotNull] GameUser user, [AllowNull] TimeSpan? timeout)
        {
            if (user.IsDisposed)    //若已经无效
            {
                VWorld.SetLastError(ErrorCodes.ObjectDisposed);
                return false;
            }
            timeout ??= Options.DefaultLockTimeout;
            if (!Monitor.TryEnter(user, timeout.Value))   //若锁定超时
            {
                VWorld.SetLastError(ErrorCodes.WAIT_TIMEOUT);
                return false;
            }
            if (user.IsDisposed)    //若已经无效
            {
                Monitor.Exit(user);
                VWorld.SetLastError(ErrorCodes.ObjectDisposed);
                return false;
            }
#if DEBUG
            //var st = new StackTrace(true);
            //var sb = new StringBuilder();
            //for (int i = 0; i < st.FrameCount; i++)
            //{
            //    // Note that high up the call stack, there is only
            //    // one stack frame.
            //    StackFrame sf = st.GetFrame(i);
            //    sb.AppendLine(sf.GetMethod().Tag);
            //    sb.AppendLine($"{sf.GetFileName()} , line {sf.GetFileLineNumber()}");
            //}
            //_LockerLog[user] = sb.ToString();
#endif
            return true;
        }

#if DEBUG
        //private readonly ConcurrentDictionary<GameUser, string> _LockerLog = new ConcurrentDictionary<GameUser, string>();
#endif

        /// <summary>
        /// 解锁用户。与<seealso cref="Lock(GameUser, int)"/>配对使用。
        /// </summary>
        /// <param name="user">用户对象。</param>
        /// <param name="pulse">是否通知等待队列中的线程锁定对象状态的更改。</param>
        public virtual void Unlock([NotNull] GameUser user, bool pulse = false)
        {
            if (pulse)
                Monitor.Pulse(user);
            Monitor.Exit(user);
#if DEBUG
            //if (!Monitor.IsEntered(user))
            //    _LockerLog.TryRemove(user, out _);
#endif
        }

        /// <summary>
        /// 将对象加载到缓存中，且锁定返回。若已经有同样对象，则返回已有对象。
        /// </summary>
        /// <param name="user">若成功返回时，可能是已有对象也可能是指定对象，如果指定对象不是已有对象，则自动处置指定对象。</param>
        /// <param name="timeout">超时时间。-1毫秒是永久等待(<seealso cref="Timeout.InfiniteTimeSpan"/>),
        /// 省略或为null是使用配置中(Options.DefaultLockTimeout)，默认的超时值锁定。</param>
        /// <returns></returns>
        protected internal bool GetOrAddAndLock([NotNull] ref GameUser user, [AllowNull] TimeSpan? timeout = null)
        {
            var lName = user.LoginName;
            using var dwLName = World.LockStringAndReturnDisposer(ref lName, timeout ?? Options.DefaultLockTimeout);    //锁定登录登出
            if (dwLName is null) //若锁定超时
                return false;
            if (_Store._LoginName2Users.TryGetValue(lName, out var gu))  //若已经存在同登录名对象
            {
                if (Lock(gu, timeout))    //成功锁定
                {
                    if (!ReferenceEquals(gu, user)) //若不是相同对象
                        user.Dispose();
                    user = gu;
                    return true;
                }
                else //若锁定失败
                    return false;
            }
            else //若没有加载到内存
            {
                if (Lock(user, timeout))  //若成功锁定
                {
                    _Store.Add(user);
                    return true;
                }
                else
                    return false;
            }
        }

        #endregion 锁定对象

        #region 锁定或加载后锁定对象

        /// <summary>
        /// 按角色Id锁定或加载后锁定用户对象。
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="user"></param>
        /// <param name="timeout">超时时间。-1毫秒是永久等待(<seealso cref="Timeout.InfiniteTimeSpan"/>),
        /// 省略或为null是使用配置中(Options.DefaultLockTimeout)，默认的超时值锁定。</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">试图锁定用户登录名在拘留池中实例时超过默认超时时间。通常这是潜在的死锁问题。</exception>
        public IDisposable LockOrLoad(Guid charId, out GameUser user, [AllowNull] TimeSpan? timeout = null)
        {
            var gc = GetCharFromId(charId);
            var gu = gc?.GameUser;
            IDisposable result;
            if (null != gu) //若得到用户
            {
                result = this.LockAndReturnDisposer(gu, timeout);
                if (null != result)
                {
                    user = gu;
                    return result;
                }
                else if (gc != null) //若无法锁定
                {
                    user = null;
                    return null;
                }
            }
            //试图加载用户
            var context = World.CreateNewUserDbContext();
            gc = context.Set<GameChar>().Include(c => c.GameUser).FirstOrDefault(c => c.Id == charId);
            if (gc is null)    //若找不到对象
            {
                VWorld.SetLastError(ErrorCodes.ERROR_NO_SUCH_USER);  //ERROR_NO_SUCH_USER
                context?.DisposeAsync();
                user = null;
                return null;
            }
            gu = gc.GameUser;
            World.EventsManager.GameUserLoaded(gu, context);
            result = this.GetOrAddAndLockAndReturnDisposer(ref gu, timeout);
            if (result is null) //若锁定失败
            {
                user = null;
                return null;
            }
            user = gu;
            return result;
        }

        /// <summary>
        /// 按登录名锁定用户或加载后锁定用户信息并返回清理接口。
        /// 这个是主要接口基本接口其他相关接口调用此函数才能加载数据并锁定。派生类可以重载此成员以控制加载锁定过程，但必须调用此成员。
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="user"></param>
        /// <param name="timeout">超时时间。-1毫秒是永久等待(<seealso cref="Timeout.InfiniteTimeSpan"/>),
        /// 省略或为null是使用配置中(Options.DefaultLockTimeout)，默认的超时值锁定。</param>
        /// <returns>成功锁定返回非空，调用者应负责释放！(<see cref="IDisposable.Dispose"/>)。
        /// null则表示出错，<see cref="VWorld.GetLastError"/>可以获取详细错误码。</returns>
        /// <exception cref="InvalidOperationException">试图锁定用户登录名在拘留池中实例时超过默认超时时间。通常这是潜在的死锁问题。</exception>
        public virtual IDisposable LockOrLoad([NotNull] string loginName, out GameUser user, [AllowNull] TimeSpan? timeout = null)
        {
            var ln = loginName;
            using var dwLn = World.LockStringAndReturnDisposer(ref ln, timeout ?? Options.DefaultLockTimeout/*务必使用较小超时，避免死锁*/);
            if (dwLn is null)  //若锁定此登录名用户的登入登出进程失败
            {
                user = null;
                return null;
            }
            IDisposable result; //返回值
            if (_Store._LoginName2Users.TryGetValue(ln, out var gu))   //若找到用户
            {
                user = gu;
                result = this.LockAndReturnDisposer(gu, timeout);
                if (null != result) //若锁定成功
                    return result;
            }
            if (null != gu && !gu.IsDisposed) //若超时
            {
                VWorld.SetLastError(ErrorCodes.WAIT_TIMEOUT);   //WAIT_TIMEOUT
                user = null;
                return null;
            }
            //此时内存中没有用户对象，试图加载对象
            var context = World.CreateNewUserDbContext();
            gu = context.GameUsers.Include(c => c.GameChars).FirstOrDefault(c => c.LoginName == ln);
            if (gu is null) //若指定的Id无效
            {
                VWorld.SetLastError(ErrorCodes.ERROR_NO_SUCH_USER);  //ERROR_NO_SUCH_USER
                user = null;
                return null;
            }
            //此时应不存在已加载的对象
            Trace.Assert(!_Store._LoginName2Users.ContainsKey(ln));
            World.EventsManager.GameUserLoaded(gu, context); //初始化

            if (!Monitor.TryEnter(gu) || gu.IsDisposed)
                throw new InvalidOperationException("异常的锁定失败。");
            result = DisposerWrapper.Create(c => World.CharManager.Unlock(c), gu);
            //加入全局数据结构
            _Store.Add(gu);
            user = gu;
            return result;
        }

        #endregion 锁定或加载后锁定对象

        /// <summary>
        /// 告知服务，指定的用户数据已经更改，且应重置下线计时。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>true已经处理，false指定用户已经无效。</returns>
        public bool NotifyChange(GameUser user)
        {
            using var dwUser = this.LockAndReturnDisposer(user);
            if (dwUser is null)
                return false;
            try
            {
                user.LastModifyDateTimeUtc = DateTime.UtcNow;
                _DirtyUsers[user] = user;
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 确定指定用户是不是指定的密码。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pwd"></param>
        /// <returns>true是，false密码错误。</returns>
        public bool IsPwd(GameUser user, string pwd)
        {
            using var hashAlgorithm = Service.GetService<HashAlgorithm>();
            var hash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(pwd));
            return Enumerable.SequenceEqual(hash, user.PwdHash);
        }

        /// <summary>
        /// 设置新密码。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public bool SetPwd(GameUser user, string pwd)
        {
            using var dwGu = this.LockAndReturnDisposer(user);
            if (dwGu is null) //若没有成功锁定
                return false;
            using var hash = Service.GetService<HashAlgorithm>();
            var pwdHash = hash.ComputeHash(Encoding.UTF8.GetBytes(pwd));
            user.PwdHash = pwdHash;
            return true;
        }

        /// <summary>
        /// 已经登录的用变更密码。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="newPwd"></param>
        /// <returns></returns>
        public bool ChangePwd(Guid token, string newPwd)
        {
            using var dwGu = this.LockAndReturnDisposer(token, out var gu);
            if (dwGu is null) //若没找到登录用户
                return false;
            using var ha = Service.GetService<HashAlgorithm>();
            gu.PwdHash = ha.ComputeHash(Encoding.UTF8.GetBytes(newPwd));
            NotifyChange(gu);
            return true;
        }

        #region 注册登入登出相关

        /// <summary>
        /// 登录用户。
        /// </summary>
        /// <param name="uid">登录名。</param>
        /// <param name="pwd"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        public GameUser Login(string uid, string pwd, string region)
        {
            var loginName = uid;
            using var dwLoginName = World.LockStringAndReturnDisposer(ref loginName, Timeout.InfiniteTimeSpan);   //锁定用户名
            Trace.Assert(null != dwLoginName);  //锁定该登录名不可失败
            List<GameActionRecord> actionRecords = new List<GameActionRecord>();
            bool logined;   //是否已经登录
            if (_Store._LoginName2Users.TryGetValue(loginName, out var gu))    //若已经登录
            {
                if (!Lock(gu))  //若锁定失败
                    return null;
                using var dw = DisposerWrapper.Create(c => Unlock(c, true), gu);    //锁定对象
                if (!IsPwd(gu, pwd))   //若密码错误
                    return null;
                logined = IsOnline(gu.CurrentChar.Id);
                var oldToken = gu.CurrentToken;
                gu.CurrentToken = Guid.NewGuid(); //换新令牌
                _Store.ChangeToken(gu, oldToken);
                var gc = gu.CurrentChar;
                _Store._Id2OnlineChars.AddOrUpdate(gc.Id, gc, (c1, c2) => gc);  //标记在线
                gu.Timeout = Options.LogoutTimeout; //置超时时间
                gu.NodeNum = World.NodeNumber;
                World.ItemManager.ResetSlot(gc);
                Nope(gu.CurrentToken);
            }
            else //未登录
            {
                using var dwUser = LockOrLoad(uid, out gu, Options.DefaultLockTimeout);
                if (dwUser is null)    //若未发现指定登录名
                    return null;
                if (!IsPwd(gu, pwd))   //若密码错误
                    return null;
                if (gu.BlockUtc != null && gu.BlockUtc > DateTime.UtcNow)  //若被封停
                    return null;
                logined = true;
                //初始化属性
                gu.LastModifyDateTimeUtc = DateTime.UtcNow;
                var gc = gu.CurrentChar;
                _Store._Id2OnlineChars.AddOrUpdate(gc.Id, gc, (c1, c2) => gc);  //标记在线

                SetSearchFriendPriority(gc, DateTime.MaxValue);
                actionRecords.Add(new GameActionRecord()    //写入登录日志
                {
                    ActionId = "Login",
                    ParentId = gc.Id,
                });
                gu.Timeout = Options.LogoutTimeout; //置超时时间
                gu.NodeNum = World.NodeNumber;
                World.ChatManager.JoinOrCreateChannel(gu.CurrentChar.Id.ToString(), "70EEA684-4E1F-4C1E-B987-765BE2845538", World.ChatManager.Options.LockTimeout, null);
                NotifyChange(gu);
            }
            if (null != actionRecords && actionRecords.Count > 0)
                World.AddToUserContext(actionRecords);
            //补偿操作
            gu.CurrentChar.Properties["DayCountOfLogin"] = (decimal)Math.Round((DateTime.UtcNow.Date - gu.CurrentChar.CreateUtc.Date).TotalDays); //最后一次登录距离创建账号的天数
            if (logined)    //若需要通知用户登录
                World.EventsManager.GameCharLogined(gu.CurrentChar);
            return gu;
        }

        /// <summary>
        /// T78发行商登录或创建用户。
        /// </summary>
        /// <param name="sid"></param>
        /// <returns>登录的用户对象，null出现错误,此时会在VWorld中设置详细错误信息。</returns>
        public GameUser LoginT78(string sid)
        {
            //验证sid有效性
            var t78 = Service.GetService<PublisherT78>();
            var dto = t78.Login(sid);
            if (dto.Ret != "0")
            {
                VWorld.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                VWorld.SetLastErrorMessage(dto.msg);
                return null;
            }
            using var db = World.CreateNewUserDbContext();
            var t78Uid = dto.Content?.Data?.UserId;
            var slot = db.Set<GameItem>().SingleOrDefault(c => c.ExtraGuid == ProjectConstant.T78PublisherSlotTId && c.ExtraString == t78Uid);
            GameUser result = null;
            if (slot is null)    //若没有注册用户
            {
                string pwd = null;
                result = QuicklyRegister(ref pwd);
                slot = new GameItem() { ExtraString = t78Uid };
                World.EventsManager.GameItemCreated(slot, ProjectConstant.T78PublisherSlotTId);
                World.ItemManager.ForcedAdd(slot, result.CurrentChar);
                result.DbContext.Add(slot);
                result.RuntimeProperties["T78IsCreated"] = true;
            }
            else //若已经注册用户
            {
                var charId = slot.OwnerId.Value;
                using var dw = LockOrLoad(charId, out result);  //加载用户
                if (dw is null)
                    return null;
                if (!IsOnline(charId))   //若没有登录
                    World.EventsManager.GameCharLogined(result.CurrentChar);
                result.RuntimeProperties["T78IsCreated"] = false;
            }
            result.RuntimeProperties["T78LoginResultString"] = dto.ResultString;
            World.CharManager.NotifyChange(result);
            return result;
        }

        public void LoginT89(T89LoginData datas)
        {
            var t89 = World.Service.GetRequiredService<PublisherT89>();
            t89.Login(datas);
            GameUser gu;
            if (!datas.HasError)
            {
                GameItem slot;
                using (var db = World.CreateNewUserDbContext())
                    slot = db.Set<GameItem>().AsNoTracking().SingleOrDefault(c => c.ExtraGuid == ProjectConstant.T89PublisherSlotTId && c.ExtraString == datas.Uid);
                if (slot is null)    //若没有注册用户
                {
                    string pwd = null;
                    gu = QuicklyRegister(ref pwd);
                    slot = new GameItem() { ExtraString = datas.Uid };
                    World.EventsManager.GameItemCreated(slot, ProjectConstant.T89PublisherSlotTId);
                    World.ItemManager.ForcedAdd(slot, gu.CurrentChar);
                    gu.DbContext.Add(slot);
                    datas.Pwd = pwd;
                    World.CharManager.NotifyChange(gu);
                }
                else //若已经注册用户
                {
                    var charId = slot.OwnerId.Value;
                    using var dw = LockOrLoad(charId, out gu);  //加载用户
                    if (dw is null)
                    {
                        datas.ErrorCode = ErrorCodes.WAIT_TIMEOUT;
                        return;
                    }
                    if (!IsOnline(charId))   //若没有登录
                        World.EventsManager.GameCharLogined(gu.CurrentChar);
                    World.CharManager.NotifyChange(gu);
                }
                datas.LoginName = gu.LoginName;
                datas.InnerToken = gu.CurrentToken;
                datas.GameChars.AddRange(gu.GameChars);
            }
        }

        /// <summary>
        /// 注销一个内存中的用户。
        /// </summary>
        /// <param name="gu"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public bool Logout(GameUser gu, LogoutReason reason)
        {
            var loginName = gu.LoginName;
            if (!_Store._LoginName2Users.TryGetValue(loginName, out gu)) //若未知情况
            {
                return false;   //TO DO
            }
            using var dwLoginName = World.LockStringAndReturnDisposer(ref loginName, Timeout.InfiniteTimeSpan);    //锁定用户名
            Trace.Assert(null != dwLoginName);
            using var dwUser = this.LockAndReturnDisposer(gu, Timeout.InfiniteTimeSpan); //锁定用户
            if (dwUser is null)  //若已被处置
                return false;
            List<GameActionRecord> actionRecords = new List<GameActionRecord>();
            if (null != gu.CurrentChar) //若存在选择的角色
            {
                var actionRecord = new GameActionRecord()   //操做记录对象。
                {
                    ActionId = "Logout",
                    ParentId = gu.CurrentChar.Id,
                };
                actionRecords.Add(actionRecord);
                if (IsOnline(gu.CurrentChar.Id))    //若用户登录了
                    SetSearchFriendPriority(gu.CurrentChar, DateTime.UtcNow);
                try
                {
                    gu.InvokeLogouting(reason);
                    gu.NodeNum = null;
                    actionRecord.DateTimeUtc = DateTime.UtcNow;
                }
                catch (Exception)
                {
                    //TO DO
                }
            }
            try
            {
                gu.DbContext.SaveChanges();
                if (actionRecords.Count > 0)
                    World.AddToUserContext(actionRecords);
            }
            catch (Exception err)
            {
                var logger = Service.GetService<ILogger<GameChar>>();
                logger?.LogError("保存用户(Number={Number})信息时发生错误。——{err}", gu.Id, err);
            }

            _Store.Remove(gu);
            gu.Dispose();
            return true;
        }


        /// <summary>
        /// 获取快速注册时，登录名的后缀序号。
        /// 每次自动递增1。
        /// </summary>
        /// <returns>登录名的后缀序号。</returns>
        public int GetQuicklyRegisterSuffixSeq()
        {
            lock (ThisLocker)
                if (!_QuicklyRegisterSuffixSeqInit)
                {
                    using var db = World.CreateNewUserDbContext();
                    var maxSeqStr = db.GameUsers.Where(c => c.LoginName.StartsWith("gy")).OrderByDescending(c => c.CreateUtc).FirstOrDefault()?.LoginName ?? "000000";
                    _QuicklyRegisterSuffixSeq = int.Parse(maxSeqStr.Substring(maxSeqStr.Length - 6, 6));
                    _QuicklyRegisterSuffixSeqInit = true;
                }
            return Interlocked.Increment(ref _QuicklyRegisterSuffixSeq);
        }

        /// <summary>
        /// 快速注册一个账号和角色。
        /// </summary>
        /// <param name="pwd">可以指定密码，如果是null，则自动生成密码且在这个参数返回明文。这是唯一获取明文的机会。数据库实际仅记录用户密码的Hash值。</param>
        /// <param name="loginName">可以指定登录名，如果为空字符串或null则自动生成登录名。</param>
        /// <returns>返回用户对象，当前版本，会默认生成唯一一个角色。指定了用户名且重名的情况将导致返回null。</returns>
        public GameUser QuicklyRegister(ref string pwd, string loginName = null)
        {
            GameUser result = new GameUser()
            {

            }; //gy210415123456 密码12位大小写
            using (var db = World.CreateNewUserDbContext())
            {
                //生成返回值
                var rnd = new Random();
                var dt = DateTime.Now;
                if (string.IsNullOrWhiteSpace(loginName)) //若需要生成登录名
                {
                    loginName = $"gy{dt.Year % 100:00}{dt.Month:00}{dt.Day:00}{GetQuicklyRegisterSuffixSeq() % 1000000:000000}";
                }
                else
                {
                    if (db.GameUsers.Any(c => c.LoginName == result.LoginName))
                        return null;
                }
                if (string.IsNullOrEmpty(pwd))   //若需要生成密码
                {
                    StringBuilder sb = StringBuilderPool.Shared.Get();
                    try
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            var rndNum = rnd.Next(52);
                            char c;
                            if (rndNum > 25)   //若是大写
                                c = Convert.ToChar(Convert.ToByte('A') + rndNum % 26);
                            else //小写
                                c = Convert.ToChar(Convert.ToByte('a') + rndNum);
                            sb.Append(c);
                        }
                        pwd = sb.ToString();
                    }
                    finally
                    {
                        StringBuilderPool.Shared.Return(sb);
                    }
                }
                using var dwLn = World.LockStringAndReturnDisposer(ref loginName, Options.DefaultLockTimeout);
                if (dwLn is null)   //若锁定失败
                    return null;
                if (_Store._LoginName2Users.ContainsKey(loginName))  //若已经存在登录名用户
                    return null;
                //存储角色信息
                World.EventsManager.GameUserCreated(result, loginName, pwd, null, null);
                lock (result)
                {
                    _Store.Add(result);
                    _Store._Id2OnlineChars.TryAdd(result.CurrentChar.Id, result.CurrentChar);
                    result.Timeout = TimeSpan.FromSeconds(15);
                    NotifyChange(result);
                }
                //db.SaveChanges();
            }
            return result;
        }

        /// <summary>
        /// 发送一个空操作以保证闲置下线重新开始计时。
        /// </summary>
        /// <param name="token"></param>
        /// <returns>true成功重置下线计时器，false未能找到有效对象。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Nope(Guid token)
        {
            var gu = GetUserFromToken(token);
            return gu != null && Nope(gu);
        }

        /// <summary>
        /// 发送一个空操作以保证闲置下线重新开始计时。
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool Nope(GameUser user)
        {
            using var dwUser = this.LockAndReturnDisposer(user, Timeout.InfiniteTimeSpan);
            if (dwUser is null)
                return false;
            try
            {
                user.LastModifyDateTimeUtc = DateTime.UtcNow;
            }
            catch
            {
            }
            return true;
        }

        /// <summary>
        /// 删除指定Id的用户和所有附属信息。
        /// </summary>
        /// <param name="loginNames"></param>
        /// <returns>true 成功删除所有用户信息，false至少有一个用户无法删除。</returns>
        public bool Delete(IEnumerable<string> loginNames)
        {
            var lns = loginNames.OrderBy(c => c);
            var list = new List<IDisposable>();

            foreach (var ln in lns) //注销所有用户并锁定其登录进程
            {
                string tmp = ln;
                var disper = World.LockStringAndReturnDisposer(ref tmp, Options.DefaultLockTimeout); //锁定登录等相关进程
                if (disper is null)
                {
                    using var dwsTmp = DisposerWrapper.Create(list);
                    return false;
                }
                list.Add(disper);
                var gu = GetUserFromLoginName(tmp);
                if (null != gu)    //若已经加载到内存中
                {
                    if (!Logout(gu, LogoutReason.SystemShutdown))
                        throw new InvalidOperationException();
                }
            }
            using var dws = DisposerWrapper.Create(list);   //最终清理
            using var db = World.CreateNewUserDbContext(); db.Database.SetCommandTimeout(TimeSpan.FromMinutes(1));
            DeleteCore(loginNames, db);
            return true;
        }

        /// <summary>
        /// 删除指定用户名的一组用户及相关所有数据。调用者要自己处理缓存和锁定问题。
        /// </summary>
        /// <param name="loginNames"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        protected bool DeleteCore(IEnumerable<string> loginNames, DbContext context)
        {
            List<Guid> ids = new List<Guid>();  //所有删除实体的Id集合。
            var userIds = context.Set<GameUser>().Where(c => loginNames.Contains(c.LoginName)).Select(c => c.Id).ToArray();    //删除的用户id集合
            if (userIds.Length <= 0)
                return true;
            ids.AddRange(userIds);
            var charIds = context.Set<GameChar>().Where(c => userIds.Contains(c.GameUserId)).Select(c => c.Id).ToArray();  //删除的角色Id集合
            ids.AddRange(charIds);
            //Remove<GameUser>(c => userIds.Contains(c.Id), context);
            var str = $"DELETE FROM [dbo].[GameUsers] WHERE [Id] in ('{string.Join("','", userIds)}')";
            context.Database.ExecuteSqlRaw(str); //删除用户和角色

            if (0 < charIds.Length)    //若有要删除的角色
            {
                var itemIds = Remove(charIds, context);  //角色直接拥有的对象Id集合
            }

            //删除扩展属性
            str = $"DELETE FROM [dbo].[ExtendProperties] WHERE [Id] in ('{string.Join("','", ids)}')";
            //context.Database.ExecuteSqlRaw(str);
            return true;
        }

        private List<Guid> Remove(IEnumerable<Guid> charIds, DbContext context)
        {
            var result = charIds.ToList();
            if (result.Count > 0)
            {
                var entityTyp = context.Model.FindEntityType(typeof(GameItem));
                string sqlStr;

                sqlStr = $"alter table {entityTyp.GetTableName()} nocheck constraint all";  //关闭外键约束
                context.Database.ExecuteSqlRaw(sqlStr);

                //sqlStr = $"DELETE FROM {entityTyp.GetTableName()} WHERE {nameof(GuidKeyObjectBase.Id)} in ('{string.Join("','", result)}')";    //删除顶层无效节点
                //context.Database.ExecuteSqlRaw(sqlStr);

                //sqlStr = $"delete from {entityTyp.GetTableName()} " +
                //    "where not exists (SELECT 1 FROM {entityTyp.GetTableName()} as t1 where t1.[Id]={entityTyp.GetTableName()}.[ParentId]) and ParentId is not null";
                //while (context.Database.ExecuteSqlRaw(sqlStr) > 0) ;    //删除所有无效节点

                try
                {
                    sqlStr = "DECLARE @MyTableVar TABLE( " +
                            "id uniqueidentifier NOT NULL" +
                            ");" +
                            "delete from [GameItems] " +
                                "output deleted.[Id] into @MyTableVar " +
                                "where [OwnerId] in ('{0}');" +
                            "while (select @@ROWCOUNT) > 0 " +
                            "begin " +
                                "delete from [GameItems] " +
                                "output deleted.[Id] into @MyTableVar " +
                                "where ParentId in (select id from @MyTableVar) " +
                            "end";
                    sqlStr = string.Format(sqlStr, string.Join("','", result));
                    context.Database.ExecuteSqlRaw(sqlStr);
                }
                finally
                {
                    sqlStr = $"alter table {entityTyp.GetTableName()} check constraint all";    //打开外键约束
                    context.Database.ExecuteSqlRaw(sqlStr);
                }
            }
            return result;
        }
        #endregion 注册登入登出相关

        /// <summary>
        /// 修改客户端字符串。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="thingId"></param>
        /// <param name="guts"></param>
        /// <returns></returns>
        public bool ModifyClientString(GameChar gameChar, Guid thingId, string guts)
        {
            if (!Lock(gameChar.GameUser))
                return false;
            try
            {
                var thing = OwHelper.GetAllSubItemsOfTree(gameChar.GameItems, c => c.Children).FirstOrDefault(c => c.Id == thingId);
                if (null == thing)
                    return false;
                thing.SetClientString(guts);
            }
            finally
            {
                Unlock(gameChar.GameUser);
            }
            return true;
        }

        /// <summary>
        /// 锁定一组指定Id的角色。
        /// 自动避免乱序死锁。
        /// </summary>
        /// <param name="charIds"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable LockOrLoadWithCharIds(IEnumerable<Guid> charIds, TimeSpan timeout)
        {
            var result = OwHelper.LockWithOrder(charIds.Distinct().OrderBy(c => c), (charId, ts) => LockOrLoad(charId, out _, timeout), timeout);
            return result;
        }

        /// <summary>
        /// 设置角色的新经验值。应引发相关事件。
        /// </summary>
        /// <remarks>可从<see cref="VWorld.GetLastError"/>中获取详细的信息。</remarks>
        /// <param name="gameChar"></param>
        /// <param name="newExp"></param>
        public void SetExp(GameChar gameChar, decimal newExp)
        {
            using var dwUser = World.CharManager.LockAndReturnDisposer(gameChar.GameUser);
            if (dwUser is null)
            {
                return;
            }
            var e = new DynamicPropertyChangedCollection();
            var oldExp = gameChar.Properties.GetDecimalOrDefault("exp");    //当前经验值
            if (newExp != oldExp)
                e.MarkAndSet(gameChar, "exp", newExp);
            var oldLv = gameChar.Properties.GetDecimalOrDefault(World.PropertyManager.LevelPropertyName); //当前等级
            var limit = gameChar.Properties.GetValueOrDefault("expLimit");
            int newLv;
            if (gameChar.GetTemplate().Properties.GetValueOrDefault("expLimit") is IEnumerable limitSeq && newExp >= oldExp)   //若经验已经变化
            {
                List<decimal> lst = new List<decimal>(limitSeq.OfType<decimal>());
                newLv = lst.FindIndex(c => c > newExp); //新等级
                newLv = newLv == -1 ? lst.Count : newLv;
                if (newLv != oldLv)    //若等级发生变化
                {
                    e.MarkAndSet(gameChar, World.PropertyManager.LevelPropertyName, newLv);
                }
            }
            World.EventsManager.OnDynamicPropertyChanged(e);
        }

        /// <summary>
        /// 给指定角色增加经验值。
        /// </summary>
        /// <remarks>可从<see cref="VWorld.GetLastError"/>中获取详细的信息。</remarks>
        /// <param name="gameChar"></param>
        /// <param name="incExp"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddExp(GameChar gameChar, decimal incExp)
        {
            using var dwUser = World.CharManager.LockAndReturnDisposer(gameChar.GameUser);
            if (dwUser is null)
            {
                return;
            }
            var oldExp = gameChar.Properties.GetDecimalOrDefault("exp");    //当前经验值
            SetExp(gameChar, oldExp + incExp);
        }

        /// <summary>
        /// 返回对变化通知集合的引用。更改该集合将直接导致记录到数据库中。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns>返回对变化通知集合的引用,需要锁定用户才能安全访问。</returns>
        public List<ChangeData> GetChangeData(GameChar gameChar)
        {
            return gameChar.GetOrCreateBinaryObject<CharBinaryExProperties>().ChangeDatas;
        }

        /// <summary>
        /// 返回风格和方案号。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="style"></param>
        /// <param name="sol"></param>
        /// <returns>若<paramref name="str"/>是空则立即返回false;</returns>
        bool GetStyleNumber(string str, [MaybeNullWhen(false)] out int style, [MaybeNullWhen(false)] out int sol)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                style = 0;
                sol = 0;
                return false;
            }
            var ary = str.Split(OwHelper.SemicolonArrayWithCN);
            switch (ary.Length)
            {
                case 1: //<风格号><2位方案号>
                    if (!int.TryParse(str[^2..], out sol)) //若无法获得方案号
                    {
                        style = 0;
                        return false;
                    }
                    if (!int.TryParse(str[0..^2], out style)) //若无法获得风格号
                    {
                        sol = 0;
                        return false;
                    }
                    break;
                case 2: //<风格号>;<方案号>
                    if (!int.TryParse(ary[1], out sol)) //若无法获得方案号
                    {
                        style = 0;
                        return false;
                    }
                    if (!int.TryParse(ary[0], out style)) //若无法获得风格号
                    {
                        sol = 0;
                        return false;
                    }
                    break;
                default:
                    style = sol = 0;
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 获取水晶的默认模板。
        /// </summary>
        /// <param name="gi"></param>
        /// <returns></returns>
        GameItemTemplate GetDefaultTemplate(GameItem gi)
        {
            var tt = gi.GetTemplate();
            var index = tt.GId % 10;    //尾号
            var cata = tt.GId / 1000;    //类型号
            var gid = cata * 1000 + index; //默认水晶类型号
            var result = World.ItemTemplateManager.Id2Template.Values.FirstOrDefault(c => c.GId == gid);
            return result;
        }

        /// <summary>
        /// 修改一系列对象的属性。只能修改客户端与服务器约定的数据，对其他未约定的数据修改将导致失败。
        /// </summary>
        /// <param name="datas"></param>
        public void ModifyProperties(ModifyPropertiesDatas datas)
        {
            using var dw = datas.LockUser();
            if (dw is null)
                return;
            var coll = datas.Modifies.Join(datas.GameChar.AllChildren, c => c.Item1, c => c.Id, (l, r) => (r, l.Item2, l.Item3)).ToLookup(c => c.r, c => (c.Item2, c.Item3));
            foreach (var tp in coll)
            {
                foreach (var item in tp)
                {
                    var tt = tp.Key.GetTemplate();
                    if (string.Compare(item.Item1, "tid", true) == 0 && tp.Key.GetTemplate().CatalogNumber == 42 && OwConvert.TryToGuid(item.Item2, out var id))   //若是水晶更改模板
                        World.ItemManager.ChangeTemplate(tp.Key, World.ItemTemplateManager.GetTemplateFromeId(id));
                    else if (tp.Key.ExtraGuid == ProjectConstant.HomelandSlotId && string.Compare(item.Item1, "activeStyle", true) == 0)   //若是家园对象的当前激活风格属性
                    {
                        var str = item.Item2 as string; Debug.Assert(str != null);
                        tp.Key.Properties["activeStyle"] = str;
                        var b = GetStyleNumber(str, out var sn, out var fn); Debug.Assert(b);
                        foreach (var gi in tp.Key.GetAllChildren()) //遍历变化水晶的模板id
                        {
                            if (gi.GetTemplate().CatalogNumber != 42)   //若不是水晶
                                continue;
                            var tidfor = gi.Properties.GetGuidOrDefault($"tidfor{sn}{fn:00}");
                            if (tidfor == Guid.Empty)  //若没有指定模板id
                                tidfor = gi.Properties.GetGuidOrDefault($"tidfor{sn};{fn}");
                            if (tidfor == Guid.Empty)  //若没有指定模板id
                            {
                                //设置默认模板号
                                var ntt = GetDefaultTemplate(gi);
                                if (ntt != gi.GetTemplate())
                                    World.ItemManager.ChangeTemplate(gi, ntt);
                            }
                            else
                            {
                                var ntt = World.ItemTemplateManager.GetTemplateFromeId(tidfor);
                                World.ItemManager.ChangeTemplate(gi, ntt);
                            }
                        }
                    }
                    else if (tt.CatalogNumber == 42 && item.Item1.StartsWith("tidfor"))   //若设置家园水晶阵容数据
                    {
                        if (!GetStyleNumber(item.Item1[6..], out var styleNumber, out var fanganNumber))  //若无法获得方案号或风格号
                            continue;
                        if (!OwConvert.TryToGuid(item.Item2, out var tidfor)) //若无法获取模板号
                            continue;
                        var hl = datas.GameChar.AllChildren.FirstOrDefault(c => c.ExtraGuid == ProjectConstant.HomelandSlotId);
                        tp.Key.Properties[item.Item1] = item.Item2;
                        var aciveStyle = hl.Properties.GetStringOrDefault("activeStyle", "1;1");    //激活号
                        if (GetStyleNumber(aciveStyle, out var sn, out var fn) && sn == styleNumber && fn == fanganNumber) //若设置了当前风格的模板号
                        {
                            var ntt = World.ItemTemplateManager.GetTemplateFromeId(tidfor);
                            World.ItemManager.ChangeTemplate(tp.Key, ntt);
                        }
                    }
                    else
                        tp.Key.Properties[item.Item1] = item.Item2;
                }
            }
            //修改角色自身信息
            var coll1 = datas.Modifies.Where(c => c.Item1 == datas.GameChar.Id);
            foreach (var item in coll1)
            {
                datas.GameChar.Properties[item.Item2] = item.Item3;
            }
            World.CharManager.NotifyChange(datas.GameChar.GameUser);
        }

        #endregion 公共方法

        #region 事件及相关

        #endregion 事件及相关
    }

    /// <summary>
    /// 获取指定的一组对象信息，调用者需要自行锁定角色，否则返回时可能出现争用改变。
    /// </summary>
    public class GetItemsContext : ComplexWorkGameContext
    {
        public GetItemsContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public GetItemsContext([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public GetItemsContext([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 若取自己的对象则是对象Id集合。若取其他角色对象这里应为模板集合。特别地，取他人对象应取槽及下属对象，否则很容易返回大量重复信息。
        /// </summary>
        public List<Guid> Ids { get; } = new List<Guid>();

        /// <summary>
        /// 是否返回每个对象完整的孩子集合。
        /// true 则返回所有孩子；false则Children属性返回空集合。
        /// </summary>
        public bool IncludeChildren { get; set; }

        /// <summary>
        /// 如果需要取其他人的对象信息，这里应置为他人的角色id。省略或为null则取自己的对象。
        /// </summary>
        public Guid? CharId { get; set; }

        /// <summary>
        /// 返回的物品信息。
        /// </summary>
        public List<GameItem> GameItems { get; } = new List<GameItem>();

        /// <summary>
        /// 如果获取了指定角色id，则这里返回角色信息。若没有指定可能返回null。
        /// </summary>
        public GameChar ResultGameChar { get; set; }

    }

    public class ModifyPropertiesDatas : ComplexWorkGameContext
    {
        public ModifyPropertiesDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public ModifyPropertiesDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public ModifyPropertiesDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 修改的项数据。只能修改客户端与服务器约定的数据，对其他未约定的数据修改将导致失败。
        /// </summary>
        public List<(Guid, string, object)> Modifies { get; } = new List<(Guid, string, object)>();
    }

    public class CharLoadedEventArgs : EventArgs
    {
        public CharLoadedEventArgs()
        {
        }


        public CharLoadedEventArgs(GameChar gameChar)
        {
            GameChar = gameChar;
        }

        public GameChar GameChar { get; set; }
    }

    public static class GameCharManagerExtensions
    {
        public static IDisposable LockLoginAndReturnDisposer(this GameCharManager manager, ref string loginName, TimeSpan? timeout = null)
        {
            if (!manager.LockLogin(ref loginName, timeout))
                return null;
            return DisposerWrapper.Create(c => manager.UnlockLogin(c, false), loginName);
        }

        /// <summary>
        /// 创建一个新账号并加入内存缓存。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="loginName"></param>
        /// <param name="pwd"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        //public static GameUser CreateNewGameUser(this GameCharManager manager, string loginName, string pwd, string displayName)
        //{
        //    var gu = new GameUser();
        //    var db = manager.World.CreateNewUserDbContext();
        //    gu.Initialize(manager.Service, loginName, pwd,db, displayName);
        //    manager._s
        //    return null;
        //}

        #region 锁定用户对象

        /// <summary>
        /// 锁定用户。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="user"></param>
        /// <param name="timeout">超时时间。-1毫秒是永久等待(<seealso cref="Timeout.InfiniteTimeSpan"/>),
        /// 省略或为null是使用配置中(Options.DefaultLockTimeout)，默认的超时值锁定。</param>
        /// <returns>返回解锁的处置接口，该接口处置时，自动解锁。如果锁定失败则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable LockAndReturnDisposer(this GameCharManager obj, GameUser user, TimeSpan? timeout = null)
        {
            if (!obj.Lock(user, timeout))
                return null;
            return DisposerWrapper.Create(() => obj.Unlock(user));
        }

        /// <summary>
        /// 用令牌锁定用户并返回。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="token"></param>
        /// <param name="gameUser">在返回成功时，这里个参数返回用户对象。</param>
        /// <param name="timeout">省略表示使用配置中默认</param>
        /// <returns>true成功锁定，false没有找到令牌代表的用户。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Lock([NotNull] this GameCharManager manager, Guid token, out GameUser gameUser, [AllowNull] TimeSpan? timeout = null)
        {
            gameUser = manager.GetUserFromToken(token);
            if (gameUser is null)
            {
                VWorld.SetLastError(ErrorCodes.ERROR_INVALID_TOKEN);
                return false;
            }
            return manager.Lock(gameUser, timeout);
        }

        /// <summary>
        /// 用令牌锁定用户并返回
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="token"></param>
        /// <param name="user"></param>
        /// <returns>null无效的令牌或锁定超时。返回处置接口用于解锁。</returns>
        /// <param name="timeout"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable LockAndReturnDisposer([NotNull] this GameCharManager obj, Guid token, out GameUser user, [AllowNull] TimeSpan? timeout = null)
        {
            if (!obj.Lock(token, out user, timeout))
                return null;
            var tmp = user;
            return DisposerWrapper.Create(() => obj.Unlock(tmp));
        }

        /// <summary>
        /// 将对象加载到缓存中，且锁定返回。若已经有同样对象，则返回已有对象。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user">若成功返回时，可能是已有对象也可能是指定对象，如果指定对象不是已有对象，则自动处置指定对象。</param>
        /// <param name="timeout">超时时间。-1毫秒是永久等待(<seealso cref="Timeout.InfiniteTimeSpan"/>),
        /// 省略或为null是使用配置中(Options.DefaultLockTimeout)，默认的超时值锁定。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable GetOrAddAndLockAndReturnDisposer([NotNull] this GameCharManager manager, [NotNull] ref GameUser user, [AllowNull] TimeSpan? timeout = null)
        {
            if (!manager.GetOrAddAndLock(ref user, timeout))
                return null;
            var gu = user;
            return DisposerWrapper.Create(() => manager.Unlock(gu));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="loginName"></param>
        /// <param name="user"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static IDisposable LockByLoginNameAndReturnDisposer([NotNull] this GameCharManager manager, string loginName, out GameUser user, [AllowNull] TimeSpan? timeout = null)
        {
            var gu = user = manager.GetUserFromLoginName(loginName);
            if (user is null)
            {
                VWorld.SetLastError(ErrorCodes.ERROR_NO_SUCH_USER);
                return null;
            }
            return manager.LockAndReturnDisposer(gu, timeout);
        }

        #endregion 锁定用户对象

        #region 项目特定

        /// <summary>
        /// 用BASE64编码的令牌获取用户。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameUser GetUserFromToken([NotNull] this GameCharManager manager, [NotNull] string token)
        {
            Guid t;
            try
            {
                t = OwConvert.ToGuid(token);
            }
            catch (Exception)
            {
                VWorld.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                return null;
            }
            return manager.GetUserFromToken(t);
        }

        /// <summary>
        /// 获取角色对象。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="token">Base64表现形式的令牌。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameChar GetGameCharFromToken([NotNull] this GameCharManager manager, [NotNull] string token) =>
            manager.GetUserFromToken(token)?.CurrentChar;

        /// <summary>
        /// 按票据锁定指定用户对象，返回一个用于解锁的的<see cref="IDisposable"/>接口。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="token"></param>
        /// <param name="gameUser"></param>
        /// <param name="timeout">超时时间。-1毫秒是永久等待(<seealso cref="Timeout.InfiniteTimeSpan"/>),
        /// 省略或为null是使用配置中(Options.DefaultLockTimeout)，默认的超时值锁定。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable LockAndReturnDisposer([NotNull] this GameCharManager manager, [NotNull] string token, out GameUser gameUser, [AllowNull] TimeSpan? timeout = null)
        {
            gameUser = manager.GetUserFromToken(token);
            if (gameUser is null)
                return null;
            return manager.LockAndReturnDisposer(gameUser, timeout);
        }

        #endregion 项目特定

    }

}
