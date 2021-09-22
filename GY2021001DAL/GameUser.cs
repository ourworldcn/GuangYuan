using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 注销的原因。
    /// </summary>
    public enum LogoutReason
    {
        Timeout,
        UserRequest,
        SystemShutdown,
    }

    /// <summary>
    /// 用户账户数据类。
    /// </summary>
    public class GameUser : GameObjectBase, IDisposable
    {
        public GameUser()
        {

        }

        public GameUser(Guid id) : base(id)
        {

        }


        /// <summary>
        /// 登录名。
        /// </summary>
        [Required]
        [StringLength(64)]
        public string LoginName { get; set; }

        /// <summary>
        /// 密码的Hash值。
        /// </summary>
        public byte[] PwdHash { get; set; }

        /// <summary>
        /// 账号所处区域。目前可能值是IOS或Android。
        /// </summary>
        [StringLength(64)]
        public string Region { get; set; }

        /// <summary>
        /// <see cref="GameChars"/>属性的后备字段。
        /// </summary>
        private List<GameChar> _GameChars = new List<GameChar>();

        /// <summary>
        /// 导航到多个角色的属性。
        /// </summary>
        public virtual List<GameChar> GameChars => _GameChars;

        /// <summary>
        /// 创建该对象的通用协调时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        #region 非数据库属性

        [NotMapped]
        public Guid CurrentToken { get; set; }

        /// <summary>
        /// 最后一次操作的时间。
        /// </summary>
        [NotMapped]
        public DateTime LastModifyDateTimeUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 超时时间。
        /// </summary>
        /// <value>默认值15分钟。</value>
        [NotMapped]
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// 管理该用户数据存储的上下文。
        /// </summary>
        [NotMapped]
        public DbContext DbContext { get; set; }

        /// <summary>
        /// 玩家当前使用的角色。
        /// 选择当前角色后，需要设置该属性。
        /// </summary>
        /// <value>当前角色对象，null用户尚未选择角色。</value>
        [NotMapped]
        public GameChar CurrentChar { get; set; }

        /// <summary>
        /// 记录服务提供者。
        /// </summary>
        [NotMapped]
        public IServiceProvider Services { get; set; }

        #endregion 非数据库属性

        #region IDisposable 接口相关

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    CurrentChar?.Dispose();
                    DbContext?.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                CurrentChar = null;
                _GameChars = null;
                DbContext = null;
                Services = null;
                base.Dispose(disposing);
            }

        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameUser()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        #endregion IDisposable 接口相关

        #region 事件

        /// <summary>
        /// 该用户即将登出。
        /// </summary>
        public event EventHandler<LogoutReason> Logouting;
        protected virtual void OnLogouting(LogoutReason e)
        {
            Logouting?.Invoke(this, e);
        }

        /// <summary>
        /// 引发 Logouting 事件。
        /// </summary>
        /// <param _Name="e"></param>
        public void InvokeLogouting(LogoutReason e)
        {
            OnLogouting(e);
        }

        public void Initialize(IServiceProvider service, string loginName, string pwd, DbContext db, string charDisplayName = null)
        {
            var dic = new Dictionary<string, object>
            {
                {"uid",loginName },
                {"pwd",pwd },
                {"db",db },
                {"charDisplayName",charDisplayName },
            };
            Initialize(service, dic);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parameters"><inheritdoc/>,额外需要以下参数<code>
        /// {
        ///     {"uid",loginName }, //登录名，字符串
        ///     {"pwd",pwd},    //密码，字符串明文。
        ///     {"db",db}, //数据库上下文对象。
        /// }
        /// </code>。</param>
        protected override void InitializeCore(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {
            base.InitializeCore(service, parameters);
            //初始化本类型的数据
            Services = service;
            DbContext = parameters["db"] as DbContext;

            CurrentToken = Guid.NewGuid();
            LoginName = (string)parameters["uid"];
            var pwd = (string)parameters["pwd"];
            using var hash = (HashAlgorithm)service.GetService(typeof(HashAlgorithm));
            var pwdHash = hash.ComputeHash(Encoding.UTF8.GetBytes(pwd));
            PwdHash = pwdHash;

            DbContext.Add(this);
        }

        protected override void LoadedCore(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {
            base.LoadedCore(service, parameters);
            //初始化本类型的数据
            Services = service;
            DbContext = parameters["db"] as DbContext;
            CurrentToken = Guid.NewGuid();
        }
        #endregion 事件
    }
}
