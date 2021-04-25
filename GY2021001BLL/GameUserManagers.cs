using GY2021001DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace GY2021001BLL
{
    public class GameCharManager
    {
        #region 字段

        private bool _QuicklyRegisterSuffixSeqInit = false;
        private int _QuicklyRegisterSuffixSeq;

        private readonly IServiceProvider _ServiceProvider;
        Timer _LogoutTimer;
        /// <summary>
        /// 登录、注销时刻锁定的登录名存储对象。
        /// </summary>
        ConcurrentDictionary<string, string> _LoginName = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 登录名到令牌的转换字典。
        /// </summary>
        ConcurrentDictionary<string, Guid> _LoginName2Token = new ConcurrentDictionary<string, Guid>();

        /// <summary>
        /// 存储令牌到用户对象的转换对象。
        /// </summary>
        ConcurrentDictionary<Guid, GameUser> _Token2User = new ConcurrentDictionary<Guid, GameUser>();

        /// <summary>
        /// 提示一些对象已经处于"脏"状态，后台线程应尽快保存。
        /// </summary>
        ConcurrentQueue<GameUser> _DirtyUsers = new ConcurrentQueue<GameUser>();

        /// <summary>
        /// 保存数据的线程。
        /// </summary>
        Thread _SaveThread;
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
        public GameCharManager(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider; //GetRequiredService
            Initialize();
        }
        #endregion 构造函数

        #region 私有方法

        private void Initialize()
        {
            VWorld world = _ServiceProvider.GetService(typeof(VWorld)) as VWorld;
            _LogoutTimer = new Timer(LogoutFunc, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            _SaveThread = new Thread(SaveFunc) { IsBackground = false, };
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
            VWorld world = _ServiceProvider.GetService(typeof(VWorld)) as VWorld;

            foreach (var item in _Token2User.Values)
            {
                if (_LoginName.TryGetValue(item.LoginName, out string loginName))
                {
                    if (!Monitor.TryEnter(loginName))   //若锁定用户名失败
                        continue;
                    try
                    {
                        if (!Monitor.TryEnter(item))    //若锁定用户失败
                            continue;
                        try
                        {
                            if (item.IsDisposed) //若已被处置
                                continue;
                            if (DateTime.UtcNow - item.LastModifyDateTimeUtc > TimeSpan.FromMinutes(15))
                                Logout(item, LogoutReason.Timeout);
                        }
                        finally
                        {
                            Monitor.Exit(item);
                        }
                    }
                    finally
                    {
                        Monitor.Exit(loginName);
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
            VWorld world = _ServiceProvider.GetService(typeof(VWorld)) as VWorld;
            List<GameUser> lst = new List<GameUser>();
            while (true)
            {
                lock (_DirtyUsers)
                {
                    lst.AddRange(_DirtyUsers.Distinct());
                    _DirtyUsers.Clear();
                }
                for (int i = 0; i < lst.Count; i++)
                {
                    try
                    {
                        var item = lst[i];
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
                    catch (Exception)
                    {
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
            while (_Token2User.Count > 0)   //把所有还在线的用户强制注销
                foreach (var item in _Token2User.Values)
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
        /// 内部同步锁。
        /// </summary>
        public object ThisLocker { get; } = new object();
        #endregion 公共属性

        #region 公共方法

        /// <summary>
        /// 获取指定令牌的用户对象。
        /// </summary>
        /// <param name="token">令牌。</param>
        /// <returns>用户对象，如果无效则返回null。</returns>
        public GameUser GetUsreFromToken(Guid token)
        {
            if (!_Token2User.TryGetValue(token, out GameUser result))
                return null;
            return result;
        }

        /// <summary>
        /// 锁定用户。务必要用<seealso cref="Unlock(GameUser)"/>解锁。两者配对使用。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="timeout">超时时间,单位:毫秒。-1(默认值)表示一直等待。</param>
        /// <returns>true该用户已经锁定且有效。false锁定超时或用户已经无效。</returns>
        public bool Lock(GameUser user, int timeout = Timeout.Infinite)
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
        /// 锁定用户。务必要用<seealso cref="Unlock(GameUser)"/>解锁。两者配对使用。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="timeout">超时时间。</param>
        /// <returns>true该用户已经锁定且有效。false锁定超时或用户已经无效。</returns>
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
        /// 解锁用户。与<seealso cref="GetUsreFromTokenAndLock(Guid)"/>配对使用。
        /// </summary>
        /// <param name="user">用户对象。</param>
        public void Unlock(GameUser user)
        {
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
            var hashAlgorithm = _ServiceProvider.GetService(typeof(HashAlgorithm)) as HashAlgorithm;
            var hash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(pwd));
            return Enumerable.SequenceEqual(hash, user.PwdHash);
        }

        /// <summary>
        /// 登录用户。
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="pwd"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        public GameUser Login(string loginName, string pwd, string region)
        {
            var innerLoginName = _LoginName.GetOrAdd(loginName, loginName);
            GameUser gu = null;
            lock (innerLoginName)    //锁定该登录名
            {
                if (!_LoginName.ContainsKey(loginName))  //若被注销了
                    return null;    //TO DO当作服务器忙处理
                if (_LoginName2Token.TryGetValue(innerLoginName, out Guid token))    //若已经登录
                {
                    _Token2User.TryGetValue(token, out gu);   //获取用户对象
                    token = Guid.NewGuid(); //换新令牌
                    lock (gu)
                    {
                        if (gu.IsDisposed)   //若已经并发处置
                            return null;    //TO DO视同没有
                        if (!IsPwd(gu, pwd))   //若密码错误
                            return null;
                        gu.CurrentToken = token;
                        _LoginName2Token.TryRemove(loginName, out Guid oldToken);
                        _LoginName2Token.AddOrUpdate(loginName, token, (c1, c2) => token);
                        _Token2User.TryRemove(oldToken, out GameUser oldGu);
                        _Token2User.TryAdd(token, gu);
                        Nope(token);
                    }
                }
                else //未登录
                {
                    var db = _ServiceProvider.GetService(typeof(GY2021001DbContext)) as GY2021001DbContext;
                    gu = db.GameUsers.FirstOrDefault(c => c.LoginName == loginName);
                    if (null == gu)    //若未发现指定登录名
                        return null;
                    lock (gu)
                    {
                        if (gu.IsDisposed)   //若已经并发处置
                            return null;    //TO DO视同没有
                        if (!IsPwd(gu, pwd))   //若密码错误
                            return null;
                        token = Guid.NewGuid();
                        gu.CurrentToken = token;
                        gu.DbContext = db;
                        foreach (var item in gu.GameChars)
                        {
                            item.GameItems.AddRange(db.GameItems.Where(c => c.OwnerId == item.Id));
                        }
                        _LoginName2Token.AddOrUpdate(loginName, token, (c1, c2) => token);
                        _Token2User.AddOrUpdate(token, gu, (c1, c2) => gu);
                    }
                }
            }
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
                    using (var db = _ServiceProvider.GetService(typeof(GY2021001DbContext)) as GY2021001DbContext)
                    {
                        var maxSeqStr = db.GameUsers.OrderByDescending(c => c.CreateUtc).FirstOrDefault()?.LoginName ?? "000000";
                        _QuicklyRegisterSuffixSeq = int.Parse(maxSeqStr.Substring(maxSeqStr.Length - 6, 6));
                        _QuicklyRegisterSuffixSeqInit = true;
                    }
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
            var result = new GameUser(); //gy210415123456 密码12位大小写
            var db = _ServiceProvider.GetService(typeof(GY2021001DbContext)) as GY2021001DbContext;
            {
                //生成返回值
                var rnd = new Random();
                var dt = DateTime.Now;
                if (string.IsNullOrEmpty(loginName)) //若需要生成登录名
                {
                    result.LoginName = $"gy{dt.Year % 100:00}{dt.Month:00}{dt.Day:00}{GetQuicklyRegisterSuffixSeq() % 1000000:000000}";
                }
                else
                {
                    if (db.GameUsers.Any(c => c.LoginName == result.LoginName))
                        return null;
                    result.LoginName = loginName;
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
                var hash = _ServiceProvider.GetService(typeof(HashAlgorithm)) as HashAlgorithm;
                var pwdHash = hash.ComputeHash(Encoding.UTF8.GetBytes(pwd));
                var gu = new GameUser()
                {
                    LoginName = result.LoginName,
                    PwdHash = pwdHash,
                    DbContext = db,
                };
                db.GameUsers.Add(gu);
                var vw = _ServiceProvider.GetService<VWorld>();
                var gc = vw.CreateChar(gu);
                db.SaveChanges();
            }
            return result;
        }

        /// <summary>
        /// 发送一个空操作以保证闲置下线重新开始计时。
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool Nope(Guid token)
        {
            if (!_Token2User.TryGetValue(token, out GameUser gu))
                return false;
            lock (gu)
            {
                if (gu.IsDisposed) //若被并发处置
                    return false;
                gu.LastModifyDateTimeUtc = DateTime.UtcNow;
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
            if (!_LoginName.TryGetValue(gu.LoginName, out string loginName))    //若没有登录
                return false;
            lock (loginName)
            {
                if (!_LoginName2Token.TryGetValue(loginName, out Guid token)) //若未知情况
                {
                    return false;   //TO DO
                }
                if (!_Token2User.TryGetValue(token, out gu)) //若未知情况
                {
                    return false;   //TO DO
                }
                lock (gu)
                {
                    if (gu.IsDisposed)   //若已被处置
                        return false;
                    try
                    {
                        gu.InvokeLogouting(reason);
                    }
                    catch (Exception)
                    {
                        //TO DO
                    }
                    try
                    {
                        gu.DbContext.SaveChanges();
                        gu.DbContext.Dispose();
                    }
                    catch (Exception)
                    {
                        //TO DO
                    }
                    gu.IsDisposed = true;
                    _Token2User.TryRemove(token, out GameUser gu1);
                    _LoginName2Token.TryRemove(loginName, out Guid token1);
                    _LoginName.Remove(loginName, out string loginName1);    //去除登录名
                }
            }
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
            if (!_Token2User.TryGetValue(token, out GameUser gu)) //若没找到登录用户
                return false;
            lock (gu)
            {
                if (gu.IsDisposed)   //若已经无效
                    return false;
                var ha = _ServiceProvider.GetService(typeof(HashAlgorithm)) as HashAlgorithm;
                gu.PwdHash = ha.ComputeHash(Encoding.UTF8.GetBytes(newPwd));
                _DirtyUsers.Enqueue(gu);
            }
            return true;
        }

        #endregion 公共方法
    }
}
