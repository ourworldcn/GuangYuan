using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Game.Store
{
    /// <summary>
    /// 游戏的玩家数据库上下文。
    /// </summary>
    /// <remarks>保存时会对跟踪的数据中支持<see cref="IBeforeSave"/>接口的对象调用<see cref="IBeforeSave.PrepareSaving(DbContext)"/></remarks>
    public class GameUserContext : DbContext
    {
        public GameUserContext([NotNull] DbContextOptions options) : base(options)
        {
        }

        protected GameUserContext()
        {
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            PrepareSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
            Task.Run(() =>
            {
                PrepareSaving();
                return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            });

        /// <summary>
        /// 在保存被调用。
        /// </summary>
        private void PrepareSaving()
        {
            var coll = ChangeTracker.Entries().Select(c => c.Entity).OfType<IBeforeSave>();
            foreach (var item in coll)
            {
                item.PrepareSaving(this);
            }
        }


    }

    /// <summary>
    /// 玩家数据对象的基类。
    /// </summary>
    public abstract class GameObjectBase : SimpleExtendPropertyBase, IDisposable
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public GameObjectBase()
        {

        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public GameObjectBase(Guid id) : base(id)
        {

        }

        private string _IdString;

        /// <summary>
        /// 获取或设置Id的字符串表现形式。
        /// </summary>
        [NotMapped]
        public string IdString
        {
            get
            {
                return _IdString ??= Id.ToString();
            }
            set
            {
                Id = Guid.Parse(value);
                _IdString = null;
            }
        }

        private string _Base64IdString;

        /// <summary>
        /// 获取或设置Id的Base64字符串表现形式。
        /// </summary>
        [NotMapped]
        public string Base64IdString
        {
            get { return _Base64IdString ??= Id.ToBase64String(); }
            set
            {
                Id = GameHelper.FromBase64String(value);
                _Base64IdString = value;
            }
        }

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
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _Base64IdString = null;
                _IdString = null;
                base.Dispose(disposing);
            }
        }
        #region 事件及相关

        #endregion 事件及相关
    }


}
