using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
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
            var coll = ChangeTracker.Entries().Select(c => c.Entity).OfType<IBeforeSave>().Where(c => !c.SuppressSave);
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

        /// <summary>
        /// 在新建一个对象后调用此方法初始化其内容。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parameters">初始化用到的附属参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {
            InitializeCore(service, parameters);
            //如果有初始化器则调用项目初始化函数
            var init = service.GetService(typeof(IGameObjectInitializer)) as IGameObjectInitializer;
            init?.Created(this);
        }

        /// <summary>
        /// 新建对象后此方法被<see cref="Initialize(IServiceProvider, IReadOnlyDictionary{string, object})"/>调用以实际初始化本对象。
        /// 此实现立即返回。
        /// </summary>
        /// <param name="service">服务容器。</param>
        /// <param name="parameters">初始化用到的附属参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected virtual void InitializeCore(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="context"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Loaded([NotNull] IServiceProvider service, [NotNull] DbContext context)
        {
            Loaded(service, new Dictionary<string, object>()
            {
                { "db",context},
            });
        }

        /// <summary>
        /// 在将一个对象从数据库加载到内存后调用此函数初始化。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parameters">各个类可能需要不同的参数以指导初始化工作。目前仅有"db"是必须的，是加载此对象的数据库上下文对象。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Loaded([NotNull] IServiceProvider service, [NotNull] IReadOnlyDictionary<string, object> parameters)
        {
            LoadedCore(service, parameters);
            //调用项目特定的加载函数。
            var init = service.GetService(typeof(IGameObjectInitializer)) as IGameObjectInitializer;
            var db = parameters["db"] as DbContext;
            init?.Loaded(this, db);
        }

        /// <summary>
        /// 对象从内存加载到数据库后调用此函数初始化。派生类应重载此方法。
        /// 此实现立即返回。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parameters"></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected virtual void LoadedCore([NotNull] IServiceProvider service, [NotNull] IReadOnlyDictionary<string, object> parameters)
        {

        }

        #region 事件及相关

        #endregion 事件及相关
    }


}
