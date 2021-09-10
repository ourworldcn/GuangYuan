using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Game;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
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
        public double DefaultLockTimeout { get; set; } = 3;
    }

    public class GameCharManager : GameManagerBase<GameCharManagerOptions>
    {
        private class GameUserStore : IDisposable
        {
            public GameUserStore()
            {
            }

            private readonly IServiceProvider _Service;
            private VWorld _World;

            public VWorld World => _World ??= _Service.GetService<VWorld>();

            internal ConcurrentDictionary<Guid, GameUser> _Token2Users = new ConcurrentDictionary<Guid, GameUser>();
            internal ConcurrentDictionary<string, GameUser> _LoginName2Users = new ConcurrentDictionary<string, GameUser>();
            internal ConcurrentDictionary<Guid, GameUser> _Id2Users = new ConcurrentDictionary<Guid, GameUser>();
            internal ConcurrentDictionary<Guid, GameChar> _Id2GChars = new ConcurrentDictionary<Guid, GameChar>();

            private bool disposedValue;

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

            public bool Remove(GameUser user)
            {
                bool succ = _Token2Users.TryRemove(user.CurrentToken, out _);
                succ = _LoginName2Users.TryRemove(user.LoginName, out _) && succ;
                succ = _Id2Users.TryRemove(user.Id, out _) && succ;
                succ = _Id2GChars.TryRemove(user.CurrentChar.Id, out _) && succ;
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

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: 释放托管状态(托管对象)
                    }

                    // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                    // TODO: 将大型字段设置为 null
                    _Token2Users = null;
                    _LoginName2Users = null;
                    _Id2Users = null;
                    _Id2GChars = null;

                    disposedValue = true;
                }
            }

            // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
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
        }

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
        private readonly ConcurrentQueue<GameUser> _DirtyUsers = new ConcurrentQueue<GameUser>();

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

        #endregion 构造函数

        #region 私有方法

        private void Initialize()
        {
            VWorld world = World;
            _LogoutTimer = new Timer(LogoutFunc, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            _SaveThread = new Thread(SaveFunc) { IsBackground = false, Priority = ThreadPriority.BelowNormal };
            _SaveThread.Start();
            world.RequestShutdown.Register(() =>
            {
                _LogoutTimer.Dispose();
            });
        }

        /// <summary>
        /// 后台扫描超时不动该强制注销的用户并注销之。
        /// </summary>
        private void LogoutFunc(object state)
        {
            VWorld world = World;

            foreach (var item in _Store._Token2Users.Values)
            {
                var loginName = item.LoginName;
                if (world.LockString(ref loginName, TimeSpan.Zero)) //若锁定用户名成功
                {
                    try
                    {
                        if (!Lock(item, TimeSpan.Zero))    //若锁定用户失败
                            continue;
                        try
                        {
                            if (DateTime.UtcNow - item.LastModifyDateTimeUtc > TimeSpan.FromMinutes(15))
                                Logout(item, LogoutReason.Timeout);
                        }
                        finally
                        {
                            Unlock(item);
                        }
                    }
                    catch (Exception err)
                    {
                        var logger = Service.GetRequiredService<ILogger<GameCharManager>>();
                        logger.LogError($"{err.Message}{Environment.NewLine}@{err.StackTrace}");
                    }
                    finally
                    {
                        world.UnlockString(loginName);
                    }
                }
                if (Environment.HasShutdownStarted && world.RequestShutdown.IsCancellationRequested)
                    return;
                Thread.Yield();
            }
        }

        /// <summary>
        /// 后台保存数据的工作函数。当游戏服务终止时负责保存所有数据。
        /// </summary>
        /// <param name="state"></param>
        private void SaveFunc(object state)
        {
            VWorld world = World;
            List<GameUser> lst = new List<GameUser>();
            while (true)
            {
                lock (_DirtyUsers)
                {
                    lst.AddRange(_DirtyUsers.Distinct());
                    _DirtyUsers.Clear();
                }
                GameUser item;
                for (int i = 0; i < lst.Count; i++)
                {
                    try
                    {
                        item = lst[i];
                        if (!Monitor.TryEnter(item)) //若锁定失败
                        {
                            _DirtyUsers.Enqueue(item);  //放入队列下次再保存
                            continue;
                        }
                        try
                        {
                            if (item.IsDisposed)    //若已经无效
                                continue;
                            item.DbContext.SaveChanges();
                        }
                        finally
                        {
                            Monitor.PulseAll(item);
                            Monitor.Exit(item);
                        }
                        Thread.Yield();
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
                }
                try
                {
                    lst.Clear();
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
            while (_Store._Token2Users.Count > 0)   //把所有还在线的用户强制注销
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
        /// 获取在线人数。
        /// </summary>
        public int OnlineCount => _Store._Token2Users.Count;

        private GameItemTemplateManager _ItemTemplateManager;

        /// <summary>
        /// 虚拟事物模板管理器。
        /// </summary>
        public GameItemTemplateManager ItemTemplateManager => _ItemTemplateManager ??= World.ItemTemplateManager;

        #endregion 公共属性

        #region 公共方法

        /// <summary>
        /// 用指定的Id获取角色对象。
        /// </summary>
        /// <param name="id"></param>
        /// <returns>如果没有找到则返回null。</returns>
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
            return _Store._Token2Users.TryGetValue(token, out GameUser result) ? result : null;
        }

        /// <summary>
        /// 锁定用户。务必要用<seealso cref="Unlock(GameUser)"/>解锁。两者配对使用。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="timeout">超时时间,单位:毫秒。-1(默认值)表示一直等待。</param>
        /// <returns>true该用户已经锁定且有效。false锁定超时或用户已经无效。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Lock(GameUser user, int timeout = Timeout.Infinite)
        {
            return Lock(user, TimeSpan.FromMilliseconds(timeout));
        }

        /// <summary>
        /// 锁定用户。务必要用<seealso cref="Unlock(GameUser)"/>解锁。两者配对使用。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="timeout">超时时间。</param>
        /// <returns>true该用户已经锁定且有效。false锁定超时或用户已经无效。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool Lock(GameUser user, TimeSpan timeout)
        {
            if (user.IsDisposed)    //若已经无效
                return false;
            if (!Monitor.TryEnter(user, timeout))   //若锁定超时
                return false;
            if (user.IsDisposed)    //若已经无效
            {
                Monitor.Exit(user);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 用令牌锁定用户并返回。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="gameUser">在返回成功时，这里个参数返回用户对象。</param>
        /// <returns>true成功锁定，false没有找到令牌代表的用户。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Lock(Guid token, out GameUser gameUser)
        {
            gameUser = GetUserFromToken(token);
            return null != gameUser && Lock(gameUser, TimeSpan.FromSeconds(Options.DefaultLockTimeout));
        }

        /// <summary>
        /// 按票据锁定指定用户对象，返回一个用于解锁的的<see cref="IDisposable"/>接口。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="gameUser"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable Lock(string token, out GameUser gameUser)
        {
            var gu = gameUser = GetUserFromToken(GameHelper.FromBase64String(token));
            if (gameUser is null || !Lock(gameUser, TimeSpan.FromSeconds(Options.DefaultLockTimeout)))
                return null;
            return DisposerWrapper.Create(() => Unlock(gu));
        }

        /// <summary>
        /// 解锁用户。与<seealso cref="Lock(GameUser, int)"/>配对使用。
        /// </summary>
        /// <param name="user">用户对象。</param>
        /// <param name="pulse">是否通知等待队列中的线程锁定对象状态的更改。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unlock(GameUser user, bool pulse = false)
        {
            if (pulse)
                Monitor.Pulse(user);
            Monitor.Exit(user);
        }

        /// <summary>
        /// 告知服务，指定的用户数据已经更改，且应重置下线计时。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>true已经处理，false指定用户已经无效。</returns>
        public bool NotifyChange(GameUser user)
        {
            if (!Lock(user))
                return false;
            try
            {
                user.LastModifyDateTimeUtc = DateTime.UtcNow;
                _DirtyUsers.Enqueue(user);
            }
            finally
            {
                Unlock(user);
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
        /// 登录用户。
        /// </summary>
        /// <param name="uid">登录名。</param>
        /// <param name="pwd"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        public GameUser Login(string uid, string pwd, string region)
        {
            var loginName = uid;
            using var dwLoginName = World.LockString(ref loginName);   //锁定用户名
            Trace.Assert(null != dwLoginName);  //锁定该登录名不可失败
            GameUser gu = null;
            List<GameActionRecord> actionRecords = new List<GameActionRecord>();
            if (_Store._LoginName2Users.TryGetValue(loginName, out gu))    //若已经登录
            {
                var token = gu.CurrentToken;
                _Store._Token2Users.TryGetValue(token, out gu);   //获取用户对象
                lock (gu)
                {
                    if (gu.IsDisposed)   //若已经并发处置
                        return null;    //TO DO视同没有
                    if (!IsPwd(gu, pwd))   //若密码错误
                        return null;
                    var oldToken = gu.CurrentToken;
                    gu.CurrentToken = Guid.NewGuid(); //换新令牌
                    _Store.ChangeToken(gu, oldToken);
                    Nope(token);
                }
            }
            else //未登录
            {
                var db = World.CreateNewUserDbContext();
                //_Service.GetService(typeof(GY001UserContext)) as GY001UserContext;
                gu = db.GameUsers.FirstOrDefault(c => c.LoginName == loginName);
                if (null == gu)    //若未发现指定登录名
                    return null;
                lock (gu)
                {
                    if (gu.IsDisposed)   //若已经并发处置
                        return null;    //TO DO视同没有
                    if (!IsPwd(gu, pwd))   //若密码错误
                        return null;
                    //初始化属性
                    gu.Loaded(Service, db);
                    gu.LastModifyDateTimeUtc = DateTime.UtcNow;

                    //加入全局列表
                    var gc = gu.CurrentChar;
                    var tmp = _Store.Add(gu);
                    Trace.Assert(tmp);

                    gu.CurrentChar.SpecificExpandProperties.LastLogoutUtc = new DateTime(9999, 1, 1);   //标记在线
                    actionRecords.Add(new GameActionRecord()    //写入登录日志
                    {
                        ActionId = "Login",
                        ParentId = gc.Id,
                    });
                    NotifyChange(gu);
                }
            }
            if (null != actionRecords && actionRecords.Count > 0)
                World.AddToUserContext(actionRecords);
            return gu;
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
                    StringBuilder sb = new StringBuilder();
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
                //存储角色信息
                result.Initialize(Service, loginName, pwd, db);
                db.SaveChanges();
            }
            return result;
        }

        /// <summary>
        /// TO DO
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="pwd"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public GameUser CreateGameUserAsync(string loginName, string pwd, string displayName, DbContext db)
        {
            return null;
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
            if (!Lock(user))
                return false;
            try
            {
                user.LastModifyDateTimeUtc = DateTime.UtcNow;
            }
            finally
            {
                Unlock(user);
            }
            return true;
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
            var token = gu.CurrentToken;
            using var dwLoginName = World.LockString(ref loginName);    //锁定用户名
            Trace.Assert(null != dwLoginName);
            if (!_Store._Token2Users.TryGetValue(token, out gu)) //若未知情况
            {
                return false;   //TO DO
            }
            if (!Lock(gu))  //若已被处置
                return false;

            var actionRecord = new GameActionRecord()   //操做记录对象。
            {
                ActionId = "Logout",
                ParentId = gu.CurrentChar.Id,
            };
            List<GameActionRecord> actionRecords = new List<GameActionRecord>()
                    {
                        actionRecord
                    };
            gu.CurrentChar.SpecificExpandProperties.LastLogoutUtc = DateTime.UtcNow;   //记录下线时间
            try
            {
                gu.InvokeLogouting(reason);
                actionRecord.DateTimeUtc = DateTime.UtcNow;
            }
            catch (Exception)
            {
                //TO DO
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
            _Store._Token2Users.TryRemove(token, out _);
            _Store._LoginName2Users.TryRemove(loginName, out _);
            _Store._Id2GChars.Remove(gu.CurrentChar.Id, out _); //去除角色Id
            gu.Dispose();
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
            if (!_Store._Token2Users.TryGetValue(token, out GameUser gu)) //若没找到登录用户
                return false;
            lock (gu)
            {
                if (gu.IsDisposed)   //若已经无效
                    return false;
                using var ha = Service.GetService<HashAlgorithm>();
                gu.PwdHash = ha.ComputeHash(Encoding.UTF8.GetBytes(newPwd));
                _DirtyUsers.Enqueue(gu);
            }
            return true;
        }

        /// <summary>
        /// 向指定角色追加一组直属的数据。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="gameItems"></param>
        /// <param name="db">使用的数据库上下文。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddGameItems(GameChar gameChar, IEnumerable<GameItem> gameItems, DbContext db)
        {
            foreach (var item in gameItems)
                item.OwnerId = gameChar.Id;
            db.Set<GameItem>().AddRange(gameItems);
            gameChar.GameItems.AddRange(gameItems);
        }

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
                thing.ClientGutsString = guts;
            }
            finally
            {
                Unlock(gameChar.GameUser);
            }
            return true;
        }

        /// <summary>
        /// 获取活跃用户的Id集合。
        /// </summary>
        /// <returns>返回的是延迟查询。在线用户在最前方，随后是下线时间降序排序的。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IQueryable<CharSpecificExpandProperty> GetActiveUserIdsQuery(DbContext db)
        {
            return db.Set<CharSpecificExpandProperty>().OrderByDescending(c => c.LastLogoutUtc);
        }

        /// <summary>
        /// 按角色Id锁定或加载后锁定用户对象。
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="timeout"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">试图锁定用户登录名在拘留池中实例时超过默认超时时间。通常这是潜在的死锁问题。</exception>
        public IDisposable LockOrLoad(Guid charId, TimeSpan timeout, out GameUser user)
        {
            var gc = GetCharFromId(charId);
            var gu = gc?.GameUser;
            string loginName;
            IDisposable result;
            if (null != gu) //若得到用户
            {
                result = this.LockAndReturnDispose(gu, timeout);
                if (null != result)
                {
                    user = null;
                    return DisposerWrapper.Create(c => World.CharManager.Unlock(c as GameUser), gu);
                }
                else if (null != gu && !gu.IsDisposed) //若锁定超时
                {
                    VWorld.SetLastError(258);   //WAIT_TIMEOUT
                    user = null;
                    return null;
                }
            }
            //试图加载用户
            using (var context = World.CreateNewUserDbContext())
                loginName = (from gChar in context.Set<GameChar>()
                             join gUser in context.Set<GameUser>()
                             on gChar.GameUserId equals gUser.Id
                             where gChar.Id == charId
                             select gUser.LoginName).FirstOrDefault();
            if (string.IsNullOrEmpty(loginName))    //若找不到对象
            {
                VWorld.SetLastError(1317);  //ERROR_NO_SUCH_USER
                user = null;
                return null;
            }
            result = LockOrLoad(loginName, timeout, out user);
            return result;
        }

        /// <summary>
        /// 按登录名锁定用户或加载后锁定用户信息并返回清理接口。
        /// 这个是主要接口基本接口其他相关接口调用此函数才能加载数据并锁定。派生类可以重载此成员以控制加载锁定过程，但必须调用此成员。
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="timeout"></param>
        /// <param name="user"></param>
        /// <returns>成功锁定返回非空，调用者应负责释放！(<see cref="IDisposable.Dispose"/>)。
        /// null则表示出错，<see cref="VWorld.GetLastError"/>可以获取详细错误码。</returns>
        /// <exception cref="InvalidOperationException">试图锁定用户登录名在拘留池中实例时超过默认超时时间。通常这是潜在的死锁问题。</exception>
        public virtual IDisposable LockOrLoad([NotNull] string loginName, TimeSpan timeout, out GameUser user)
        {
            if (!World.LockString(ref loginName, TimeSpan.FromSeconds(Options.DefaultLockTimeout/*务必使用较小超时，避免死锁*/)))  //若锁定此登录名用户的登入登出进程失败
            {
                throw new InvalidOperationException("试图锁定用户登录名在拘留池中实例时超过默认超时时间。通常这是潜在的死锁问题。");
            }
            using var dwLoginName = DisposerWrapper.Create(c => World.UnlockString(c, true), loginName);  //保证解除锁定
            IDisposable result; //返回值
            GameUser gu = null;
            if (_Store._LoginName2Users.TryGetValue(loginName, out gu))   //若找到用户
            {
                user = gu;
                result = this.LockAndReturnDispose(gu, timeout);
                if (null != result) //若锁定成功
                    return result;
            }
            if (null != gu && !gu.IsDisposed) //若超时
            {
                VWorld.SetLastError(258);   //WAIT_TIMEOUT
                user = null;
                return null;
            }
            //此时内存中没有用户对象，试图加载对象
            var context = World.CreateNewUserDbContext();
            gu = context.GameUsers.Include(c => c.GameChars).FirstOrDefault(c => c.LoginName == loginName);
            if (gu is null) //若指定的Id无效
            {
                VWorld.SetLastError(1317);  //ERROR_NO_SUCH_USER
                user = null;
                return null;
            }
            //此时应不存在已加载的对象
            Trace.Assert(!_Store._LoginName2Users.ContainsKey(loginName));
            gu.Loaded(World.Service, context);  //初始化
            if (!Monitor.TryEnter(gu) || gu.IsDisposed)
                throw new InvalidOperationException("异常的锁定失败。");
            result = DisposerWrapper.Create(c => World.CharManager.Unlock(c as GameUser), gu);
            //加入
            _Store.Add(gu);
            user = gu;
            return result;
        }

        /// <summary>
        /// 锁定一组用户对象。
        /// </summary>
        /// <param name="users"></param>
        /// <param name="timeout"></param>
        /// <returns>null表示失败。否则返回一个解锁所有锁定对象的接口。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable Lock(IEnumerable<GameUser> users, TimeSpan timeout)
        {
            return OwHelper.LockWithOrder(users.OrderBy(c => c.Id), (gu, ts) => Lock(gu, ts), gu => Unlock(gu, true), timeout);
        }

        /// <summary>
        /// 锁定一组指定Id的角色。
        /// 可以避免乱序死锁。
        /// </summary>
        /// <param name="charIds"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable LockOrLoadWithUserIds(IEnumerable<Guid> userIds, TimeSpan timeout)
        {
            var result = OwHelper.LockWithOrder(userIds.OrderBy(c => c), (charId, ts) => LockOrLoad(charId, timeout, out _), timeout);
            return result;
        }

        /// <summary>
        /// 根据给定的角色Id集合获取用户Id集合。
        /// </summary>
        /// <param name="charIds"></param>
        /// <returns></returns>
        public IEnumerable<Guid> GetUserIdsFromCharIds(IEnumerable<Guid> charIds)
        {
            var result = new List<Guid>();
            var dbIds = new List<Guid>();   //需要从数据库读取用户Id的角色Id集合
            foreach (var item in charIds)   //获取内存中的用户Id
            {
                var gu = GetCharFromId(item)?.GameUser;
                if (gu is null)
                    dbIds.Add(item);
                else
                    result.Add(gu.Id);
            }
            if (dbIds.Count > 0)   //若需要从数据库读取
            {
                using var db = World.CreateNewUserDbContext();
                var coll = from gChar in db.Set<GameChar>()
                           join gUser in db.Set<GameUser>()
                           on gChar.GameUserId equals gUser.Id
                           where dbIds.Contains(gChar.Id)
                           select gUser.Id;
                var ary = coll.ToArray();
                if (ary.Length != dbIds.Count)
                    return null;
                result.AddRange(ary);
            }
            return result;
        }

        #endregion 公共方法

        #region 事件及相关

        #endregion 事件及相关
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
        /// <summary>
        /// 锁定用户。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="user"></param>
        /// <param name="timeout"></param>
        /// <returns>返回解锁的处置接口，该接口处置时，自动解锁。如果锁定失败则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable LockAndReturnDispose(this GameCharManager obj, GameUser user, TimeSpan timeout)
        {
            if (!obj.Lock(user, timeout))
                return null;
            return DisposerWrapper.Create(() => obj.Unlock(user));
        }

        /// <summary>
        /// 使用默认超时试图锁定用户。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="user"></param>
        /// <returns>null无效的令牌或锁定超时。返回处置接口用于解锁。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable LockAndReturnDispose(this GameCharManager obj, GameUser user) => obj.LockAndReturnDispose(user, TimeSpan.FromSeconds(obj.Options.DefaultLockTimeout));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="token"></param>
        /// <param name="user"></param>
        /// <returns>null无效的令牌或锁定超时。返回处置接口用于解锁。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable LockAndReturnDispose(this GameCharManager obj, Guid token, out GameUser user)
        {
            if (!obj.Lock(token, out user))
                return null;
            var tmp = user;
            return DisposerWrapper.Create(() => obj.Unlock(tmp));
        }

    }
}
