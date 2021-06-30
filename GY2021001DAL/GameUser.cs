using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace GY2021001DAL
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

    public class GameUser : GameObjectBase
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
        /// 导航到多个角色的属性。
        /// </summary>
        public virtual List<GameChar> GameChars { get; } = new List<GameChar>();

        #region 非数据库属性

        [NotMapped]
        public Guid CurrentToken { get; set; }

        /// <summary>
        /// 最后一次操作的时间。
        /// </summary>
        [NotMapped]
        public DateTime LastModifyDateTimeUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 该对象是否有效。
        /// </summary>
        [NotMapped]
        public bool IsDisposed { get; set; } = false;

        /// <summary>
        /// 管理该用户数据存储的上下文。
        /// </summary>
        [NotMapped]
        public Microsoft.EntityFrameworkCore.DbContext DbContext { get; set; }

        /// <summary>
        /// 创建该对象的通用协调时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 玩家当前使用的角色。
        /// 选择当前角色后，需要设置该属性。
        /// </summary>
        [NotMapped]
        public GameChar CurrentChar { get; set; }
        #endregion 非数据库属性

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
        #endregion 事件
    }
}
