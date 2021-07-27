using GY2021001DAL;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    /// <summary>
    /// GameCharManager类的配置类。
    /// </summary>
    public class GameCharManagerOptions
    {
        public GameCharManagerOptions()
        {

        }

        /// <summary>
        /// 角色被创建后调用。
        /// </summary>
        public Func<IServiceProvider, GameChar, bool> CharCreated { get; set; }

        /// <summary>
        /// 默认锁定超时。单位：秒
        /// 在指定时间内无法锁定对象就返回失败。
        /// </summary>
        public double DefaultLockTimeout { get; set; } = 2;
    }

    public class GameCharManager : GameManagerBase<GameCharManagerOptions>
    {
        #region 字段
        private bool _QuicklyRegisterSuffixSeqInit = false;
        private int _QuicklyRegisterSuffixSeq;
        private Timer _LogoutTimer;

        /// <summary>
        /// 登录、注销时刻锁定的登录名存储对象。
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _LoginName = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 登录名到令牌的转换字典。
        /// </summary>
        private readonly ConcurrentDictionary<string, Guid> _LoginName2Token = new ConcurrentDictionary<string, Guid>();

        /// <summary>
        /// 存储令牌到用户对象的转换对象。
        /// </summary>
        private readonly ConcurrentDictionary<Guid, GameUser> _Token2User = new ConcurrentDictionary<Guid, GameUser>();

        /// <summary>
        /// 提示一些对象已经处于"脏"状态，后台线程应尽快保存。
        /// </summary>
        private readonly ConcurrentQueue<GameUser> _DirtyUsers = new ConcurrentQueue<GameUser>();

        /// <summary>
        /// 保存数据的线程。
        /// </summary>
        private Thread _SaveThread;

        /// <summary>
        /// 角色Id到角色对象的字段。
        /// </summary>
        private readonly ConcurrentDictionary<Guid, GameChar> _Id2GameChar = new ConcurrentDictionary<Guid, GameChar>();

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
                    catch (Exception err)
                    {
                        var logger = Services.GetRequiredService<ILogger<GameCharManager>>();
                        logger.LogError($"{err.Message}{Environment.NewLine}@{err.StackTrace}");
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
            VWorld world = World;
            List<GameUser> lst = new List<GameUser>();
            while (true)
            {
                lock (_DirtyUsers)
                {
                    lst.AddRange(_DirtyUsers.Distinct());
                    _DirtyUsers.Clear();
                }
                GameUser item = null;
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
                        Trace.WriteLine($"保存数据时出现未知错误{err.Message}");
                    }
                    catch (Exception err)
                    {
                        Trace.WriteLine($"保存数据时出现未知错误{err.Message}");
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
        /// 获取在线人数。
        /// </summary>
        public int OnlineCount => _Token2User.Count;

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
            return _Id2GameChar.GetValueOrDefault(id, null);
        }

        /// <summary>
        /// 获取指定令牌的用户对象。
        /// </summary>
        /// <param name="token">令牌。</param>
        /// <returns>用户对象，如果无效则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameUser GetUserFromToken(Guid token)
        {
            return _Token2User.TryGetValue(token, out GameUser result) ? result : null;
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
            return null == gameUser ? false : Lock(gameUser, TimeSpan.FromSeconds(Options.DefaultLockTimeout));
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
            var hashAlgorithm = Services.GetService<HashAlgorithm>();
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
                    var db = World.CreateNewUserDbContext();
                    //_Service.GetService(typeof(GY2021001DbContext)) as GY2021001DbContext;
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
                        token = Guid.NewGuid();
                        gu.CurrentToken = token;
                        gu.DbContext = db;
                        gu.Services = Services;
                        gu.CurrentChar = gu.GameChars[0];
                        gu.LastModifyDateTimeUtc = DateTime.UtcNow;

                        //加入全局列表
                        var gc = gu.CurrentChar;
                        _Id2GameChar[gc.Id] = gc;
                        _LoginName2Token.AddOrUpdate(loginName, token, (c1, c2) => token);
                        _Token2User.AddOrUpdate(token, gu, (c1, c2) => gu);
                        OnCharLoaded(new CharLoadedEventArgs(gu.CurrentChar));
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
                    using (var db = World.CreateNewUserDbContext())
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
            GameUser result = new GameUser()
            {

            }; //gy210415123456 密码12位大小写
            using (var db = World.CreateNewUserDbContext())
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
                var hash = Services.GetService<HashAlgorithm>();
                var pwdHash = hash.ComputeHash(Encoding.UTF8.GetBytes(pwd));
                var gu = new GameUser()
                {
                    LoginName = result.LoginName,
                    PwdHash = pwdHash,
                    DbContext = db,
                };
                var vw = World;
                var charTemplate = ItemTemplateManager.GetTemplateFromeId(ProjectConstant.CharTemplateId);
                var gc = CreateChar(charTemplate, gu);
                gu.GameChars.Add(gc);
                gu.CurrentChar = gu.GameChars[0];
                db.GameUsers.Add(gu);
                db.SaveChanges();
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
            return gu == null ? false : Nope(gu);
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
                    }
                    catch (Exception err)
                    {
                        var logger = Services.GetService<ILogger<GameChar>>();
                        logger?.LogError("保存用户(Number={Number})信息时发生错误。——{err}", gu.Id, err);
                    }
                    _Token2User.TryRemove(token, out _);
                    _LoginName2Token.TryRemove(loginName, out _);
                    _Id2GameChar.Remove(gu.CurrentChar.Id, out _); //去除角色Id
                    _LoginName.Remove(loginName, out _);    //去除登录名
                    gu.Dispose();
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
                var ha = Services.GetService<HashAlgorithm>();
                gu.PwdHash = ha.ComputeHash(Encoding.UTF8.GetBytes(newPwd));
                _DirtyUsers.Enqueue(gu);
            }
            return true;
        }

        /// <summary>
        /// 创建一个角色对象。此对象没有加入上下文，调用者需要自己存储。
        /// 特别地，GameChar.Children中的元素需要额外加入，这个属性当前不是自动导航属性。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public GameChar CreateChar(GameItemTemplate template, GameUser user)
        {
            var result = new GameChar()
            {
                TemplateId = template.Id,
            };
            //初始化级别
            decimal lv;
            if (!result.Properties.TryGetValue(ProjectConstant.LevelPropertyName, out object lvObj))
            {
                lv = 0;
                result.Properties[ProjectConstant.LevelPropertyName] = lv;
            }
            else
                lv = (decimal)lvObj;
            //初始化属性
            foreach (var item in template.Properties)
            {
                var seq = item.Value as decimal[];
                if (null != seq)   //若是属性序列
                {
                    result.Properties[item.Key] = seq[(int)lv];
                }
                else
                    result.Properties[item.Key] = item.Value;
            }
            result.PropertiesString = OwHelper.ToPropertiesString(result.Properties);   //改写属性字符串
            //建立关联
            result.GameUser = user;
            result.GameUserId = user.Id;
            //初始化容器
            var gim = World.ItemManager;
            var ary = template.ChildrenTemplateIds.Select(c => gim.CreateGameItem(c, result.Id)).ToArray();
            user.DbContext.Set<GameItem>().AddRange(ary);
            result.GameItems.AddRange(ary);

            //调用外部创建委托
            try
            {
                result.InitialCreation();
                var dirty = Options?.CharCreated?.Invoke(Services, result);
            }
            catch (Exception)
            {
            }
            //累计属性
            //var allProps = OwHelper.GetAllSubItemsOfTree(result.GameItems, c => c.Children).SelectMany(c => c.Properties);
            //var coll = from tmp in allProps
            //           where tmp.Value is decimal && tmp.Key != ProjectConstant.LevelPropertyName   //避免累加级别属性
            //           group (decimal)tmp.Value by tmp.Key into g
            //           select ValueTuple.Create(g.Key, g.Sum());
            //foreach (var item in coll)
            //{
            //    result.Properties[item.Item1] = item.Item2;
            //}
            //result.PropertiesString = OwHelper.ToPropertiesString(result.Properties);   //改写属性字符串
            //创建欢迎邮件
            var mail = new GameMail()
            {
                Subject = "欢迎您加入XXX世界",
                Body = "此邮件是测试目的，正式版将删除。",
            };
            mail.MailAddresses.Add(new GameMailAddress()
            {
                Kind = Game.Social.MailAddressKind.From,
                DisplayName = "xxx管理员",
                ThingId = Guid.Empty
            });
            mail.MailAddresses.Add(new GameMailAddress()
            {
                Kind = Game.Social.MailAddressKind.To,
                DisplayName = $"尊敬的玩家{result.DisplayName}",
                ThingId = result.Id,
            });
            mail.Attachmentes.Add(new GameMailAttachment()
            {
                PropertyString = "TName=这是一个测试的附件对象,tid={89A586A8-CD8D-40FF-BDA2-41E68B6EC505},count=1,desc=tid是送的物品模板id;count是数量。",
            });
            World.SocialManager.AddMails(mail, result);
            return result;
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

        #endregion 公共方法

        #region 事件及相关
        public event EventHandler<CharLoadedEventArgs> CharLoaded;
        protected virtual void OnCharLoaded(CharLoadedEventArgs e)
        {
            try
            {
                e.GameChar.InvokeLoaded();
            }
            catch (Exception)
            {
            }
            try
            {
                //补足角色的槽
                var tt = World.ItemTemplateManager.GetTemplateFromeId(e.GameChar.TemplateId);
                List<Guid> ids = new List<Guid>();
                tt.ChildrenTemplateIds.ApartWithWithRepeated(e.GameChar.GameItems, c => c, c => c.TemplateId, ids, null, null);
                foreach (var item in ids.Select(c => World.ItemTemplateManager.GetTemplateFromeId(c)))
                {
                    var gameItem = World.ItemManager.CreateGameItem(item, e.GameChar.Id);
                    e.GameChar.GameUser.DbContext.Set<GameItem>().Add(gameItem);
                    e.GameChar.GameItems.Add(gameItem);
                }
                //补足所属物品的槽
                World.ItemManager.Normalize(e.GameChar.GameItems);
                //通知所属物品加载完毕
                var coll = OwHelper.GetAllSubItemsOfTree(e.GameChar.GameItems, c => c.Children).ToArray();
                foreach (var item in coll)
                    item.InvokeLoading(Services);
                //清除锁定属性槽内物品，放回道具背包中
                var gim = World.ItemManager;
                var daojuBag = e.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.DaojuBagSlotId); //道具背包
                var slot = e.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockAtkSlotId); //锁定槽
                gim.MoveItems(slot, c => true, daojuBag);
                slot = e.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockMhpSlotId); //锁定槽
                gim.MoveItems(slot, c => true, daojuBag);
                slot = e.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockQltSlotId); //锁定槽
                gim.MoveItems(slot, c => true, daojuBag);

            }
            catch (Exception)
            {
            }
            CharLoaded?.Invoke(this, e);

        }


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


}
