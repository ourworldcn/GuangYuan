using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;

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
        /// <summary>
        /// 某种原因被强制下线。
        /// </summary>
        Force,
    }

    /// <summary>
    /// 用户账户数据类。
    /// </summary>
    public class GameUser : GameObjectBase, IDisposable
    {
        public GameUser()
        {
            Initialize();
        }

        public GameUser(Guid id) : base(id)
        {
            Initialize();
        }

        private void Initialize()
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
        public virtual List<GameChar> GameChars { get => _GameChars; set => _GameChars = value; }

        /// <summary>
        /// 创建该对象的通用协调时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 当前承载此用户的服务器节点号。空则表示此用户尚未被任何节点承载（未在线）。但有节点号，不代表用户登录，可能只是维护等其他目的将用户承载到服务器中。
        /// </summary>
        [JsonIgnore]
        public int? NodeNum { get; set; }

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
        [JsonConverter(typeof(TimeSpanJsonConverter))]
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// 管理该用户数据存储的上下文。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public DbContext DbContext { get; set; }

        /// <summary>
        /// 玩家当前使用的角色。
        /// 选择当前角色后，需要设置该属性。
        /// </summary>
        /// <value>当前角色对象，null用户尚未选择角色。</value>
        [NotMapped]
        [JsonIgnore]
        public GameChar CurrentChar { get; set; }

        /// <summary>
        /// 记录服务提供者。
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public IServiceProvider Services { get; set; }

        #endregion 非数据库属性

        #region 扩展属性

        /// <summary>
        /// 禁言到期时间。空表示没有禁言。
        /// </summary>
        [NotMapped, JsonIgnore]
        public DateTime? SilenceUtc { get; set; }

        /// <summary>
        /// 封停账号到期时间。空表示没有被封停。
        /// </summary>
        [NotMapped, JsonIgnore]
        public DateTime? BlockUtc
        {
            get
            {
                if (Properties.TryGetDateTime(nameof(BlockUtc), out var result))
                    return result;
                return null;
            }
            set
            {
                if (value is null)
                {
                    Properties.Remove(nameof(BlockUtc));
                }
                else
                {
                    Properties[nameof(BlockUtc)] = value.Value.ToString();
                }
            }
        }

        #endregion 扩展属性

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
                    // 释放托管状态(托管对象)
                    CurrentChar?.Dispose();
                    DbContext?.Dispose();
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                CurrentChar = null;
                _GameChars = null;
                DbContext = null;
                Services = null;
                base.Dispose(disposing);
            }

        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
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
        /// <param name="e"></param>
        public void InvokeLogouting(LogoutReason e)
        {
            OnLogouting(e);
        }

        public override void PrepareSaving(DbContext db)
        {
            base.PrepareSaving(db);
        }
        #endregion 事件
    }
}
