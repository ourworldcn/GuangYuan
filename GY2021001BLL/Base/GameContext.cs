using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace OW.Game
{
    /// <summary>
    /// 提供基类，用于在类群之间传递数据的基类。
    /// </summary>
    public abstract class GameContextBase : IDisposable
    {

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="world"></param>
        protected GameContextBase([NotNull] VWorld world)
        {
            _World = world;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        protected GameContextBase([NotNull] IServiceProvider service)
        {
            _Service = service;
        }

        private IServiceProvider _Service;
        /// <summary>
        /// 获取服务提供者接口。
        /// </summary>
        public IServiceProvider Service => _Service;

        private VWorld _World;
        /// <summary>
        /// 获取世界服务。
        /// </summary>
        public VWorld World => _World ??= _Service.GetService(typeof(VWorld)) as VWorld;

        private bool _UserContextOwner;
        private GameUserContext _UserContext;
        /// <summary>
        /// 获取用户数据库上下文。
        /// 如果是自动生成的(未赋值第一次读取时自动生成)，将在<see cref="Dispose"/>调用时自动处置。
        /// 如果设置该值，调用者需要自己处置上下文。
        /// </summary>
        public GameUserContext UserDbContext
        {
            get
            {
                if (_UserContext is null)
                {
                    _UserContext = World.CreateNewUserDbContext();
                    _UserContextOwner = true;
                }
                return _UserContext;
            }

            set
            {
                if (_UserContext != value && _UserContextOwner) //若需要处置原有的对象。
                    _UserContext?.Dispose();
                _UserContext = value;
                _UserContextOwner = false;
            }
        }

        #region IDisposable接口相关

        /// <summary>
        /// 标识当前对象是否已经被清理。
        /// </summary>
        /// <remarks>可能出现在一个线程中写入值，另一个线程读取值的情况。</remarks>
        private volatile bool _IsDisposed;
        /// <summary>
        /// 是否已经被处置。
        /// </summary>
        protected bool IsDisposed { get => _IsDisposed; }

        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    if (_UserContextOwner) _UserContext?.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _UserContext = null;
                _World = null;
                _Service = null;
                _IsDisposed = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameCharWorkDataBase()
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

        //public async ValueTask DisposeAsync()
        //{
        //    await DisposeAsyncCore();

        //    Dispose(disposing: false);
        //    GC.SuppressFinalize(this);
        //}

        //protected virtual async ValueTask DisposeAsyncCore()
        //{
        //     Dispose();
        //}

        #endregion IDisposable接口相关

        /// <summary>
        /// 保存数据。
        /// 该实现仅在<see cref="UserDbContext"/>有效时保存数据。若其后备字段未生成有效实例则立即返回。
        /// </summary>
        public virtual void Save()
        {
            _UserContext?.SaveChanges();
        }
    }

    /// <summary>
    /// 有当前角色对象的数据上下文对象。
    /// </summary>
    public abstract class GameCharGameContext : GameContextBase, IResultWorkData
    {
        public const string Separator = "`";

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="gameChar"></param>
        protected GameCharGameContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service)
        {
            _GameChar = gameChar;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="world"></param>
        /// <param name="gameChar"></param>
        protected GameCharGameContext([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world)
        {
            _GameChar = gameChar;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="world"></param>
        /// <param name="token"></param>
        protected GameCharGameContext([NotNull] VWorld world, [NotNull] string token) : base(world)
        {
            Token = OwConvert.ToGuid(token);
        }

        private GameChar _GameChar;

        /// <summary>
        /// 当前角色。
        /// </summary>
        public GameChar GameChar => _GameChar ??= World.CharManager.GetUserFromToken(Token).CurrentChar;

        /// <summary>
        /// 登录角色的令牌。
        /// </summary>
        public Guid Token { get; set; }

        /// <summary>
        /// 使用默认超时试图锁定<see cref="GameChar"/>用户。
        /// </summary>
        /// <returns></returns>
        public IDisposable LockUser()
        {
            try
            {
                return World.CharManager.LockAndReturnDisposer(GameChar.GameUser);
            }
            catch (Exception err)
            {
                var logger = Service?.GetService<ILogger<GameCharGameContext>>();
                logger?.LogWarning($"锁定单个角色时出现异常——{err.Message}");
                return null;
            }
        }

        ConcurrentQueue<GamePropertyChangeItem<object>> _GamePropertyChanges;
        /// <summary>
        /// 获取记录属性变化的集合。
        /// 首次读取该属性时，如果<see cref="GameChar"/>存在则通过<see cref="GamePropertyChangeManagerExtensions.GetOrCreatePropertyChangedList(GameChar)"/>创建。
        /// 否则会初始化新实例。
        /// </summary>
        public ConcurrentQueue<GamePropertyChangeItem<object>> PropertyChanges =>
            _GamePropertyChanges ??= GameChar is null ? new ConcurrentQueue<GamePropertyChangeItem<object>>() : GameChar.GetOrCreatePropertyChangedList();


        #region IResultWorkData接口相关

        private bool? _HasError;

        /// <summary>
        /// 是否有错误。不设置则使用<see cref="ErrorCode"/>来判定。
        /// </summary>
        public bool HasError { get => _HasError ??= ErrorCode != ErrorCodes.NO_ERROR; set => _HasError = value; }

        /// <summary>
        /// 错误码，参见 ErrorCodes。
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 调试用的提示性信息。
        /// </summary>
        private string _ErrorMessage;

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string ErrorMessage
        {
            get => _ErrorMessage ??= new Win32Exception(ErrorCode).Message;
            set => _ErrorMessage = value;
        }

        #endregion IResultWorkData接口相关

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
                _GamePropertyChanges = null;
                _GameChar = null;
                base.Dispose(disposing);
            }
        }
    }

}
