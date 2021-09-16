using GuangYuan.GY001.UserDb;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GuangYuan.GY001.BLL
{
    public class Compiler
    {
        public Assembly Compile(string text, params Assembly[] referencedAssemblies)
        {
            var references = referencedAssemblies.Select(it => MetadataReference.CreateFromFile(it.Location));
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var assemblyName = "_" + Guid.NewGuid().ToString("D");
            var syntaxTrees = new SyntaxTree[] { CSharpSyntaxTree.ParseText(text) };
            var compilation = CSharpCompilation.Create(assemblyName, syntaxTrees, references, options);
            using var stream = new MemoryStream();
            var compilationResult = compilation.Emit(stream);
            if (compilationResult.Success)
            {
                stream.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(stream.ToArray());
            }
            throw new InvalidOperationException("Compilation error");
        }
    }

    /// <summary>
    /// 游戏管理类(服务)的基类。原则上派生类应该都是单例生存期的服务。
    /// </summary>
    /// <remarks>派生类如果没有特别说明，非私有成员都应该可以支持多线程并发调用。</remarks>
    public abstract class GameManagerBase<TOptions>
    {
        #region 属性及相关

        private readonly IServiceProvider _Services;
        public IServiceProvider Service => _Services;

        private readonly TOptions _Options;
        public TOptions Options => _Options;

        private VWorld _VWorld;
        /// <summary>
        /// 获取游戏世界的服务对象。
        /// </summary>
        public VWorld World
        {
            get => _VWorld ??= _Services.GetRequiredService<VWorld>();   //一定是单例，所以无所谓并发
        }

        /// <summary>
        /// 同步锁
        /// </summary>
        protected readonly object ThisLocker = new object();

        #endregion 属性及相关

        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameManagerBase()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service">配置数据。</param>
        public GameManagerBase(IServiceProvider service)
        {
            _Services = service;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service">使用的服务容器。</param>
        /// <param name="options">配置数据。</param>
        public GameManagerBase(IServiceProvider service, TOptions options)
        {
            _Services = service;
            _Options = options;
        }

        #endregion 构造函数

    }

    public interface IResultWorkData
    {
        /// <summary>
        /// 是否有错误。
        /// </summary>
        /// <value>false无错，true有错误，具体错误参见错误码。</value>
        public bool HasError { get; set; }

        /// <summary>
        /// 错误码。
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 错误信息。
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 从<see cref="VWorld"/>对象获取错误信息。
        /// </summary>
        public void RefreshErrorFromWorld()
        {
            ErrorCode = VWorld.GetLastError();
            ErrorMessage = VWorld.GetLastErrorMessage();
        }
    }

    /// <summary>
    /// 复杂工作的参数返回值封装类的基类。
    /// </summary>
    public abstract class ComplexWorkDatasBase : GameCharWorkDataBase, IResultWorkData
    {
        protected ComplexWorkDatasBase([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        protected ComplexWorkDatasBase([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        protected ComplexWorkDatasBase([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        #region 入参

        /// <summary>
        /// 要求进行工作的Id。
        /// 可能不需要。
        /// </summary>
        public Guid ActionId { get; set; }

        /// <summary>
        /// 额外参数。
        /// 不同操作自行定义该属性内的内容。
        /// </summary>
        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        /// <summary>
        /// 初始化变量并加入字典的帮助器方法。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="cache">如果不是null就立即返回该值。否则声明一个新对象赋值并返回。</param>
        /// <returns>cache</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        protected T GetOrAdd<T>(string name, ref T cache) where T : class, new()
        {
            if (cache is null)
            {
                cache = new T();
                Parameters.Add(name, cache);
            }
            return cache;
        }

        #endregion 入参

        #region 出参

        /// <summary>
        /// 返回的参数。
        /// 不同操作自行定义该属性内的内容。
        /// </summary>
        public Dictionary<string, object> Result { get; } = new Dictionary<string, object>();

        [Conditional("DEBUG")]
        public void SetDebugMessage(string msg) => ErrorMessage = msg;

        private string _ErrorMessage;

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string ErrorMessage
        {
            get => _ErrorMessage ??= new Win32Exception(ErrorCode).Message;
            set => _ErrorMessage = value;
        }

        /// <summary>
        /// 是否有错误。
        /// false没有错误，true有错误。
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// 返回码。当前版本默认使用<see cref="HttpStatusCode"/>。如果派生类不打算使用，则需要自行定义说明。
        /// </summary>
        public int ErrorCode { get; set; }

        #endregion 出参

        #region IDisposable接口

        #endregion IDisposable接口
    }

    /// <summary>
    /// 提供基类，用于在类群之间传递数据的基类。
    /// </summary>
    public abstract class WorkDataBase : IDisposable
    {

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="world"></param>
        protected WorkDataBase([NotNull] VWorld world)
        {
            _World = world;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        protected WorkDataBase([NotNull] IServiceProvider service)
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
        public VWorld World => _World ??= (VWorld)_Service.GetService(typeof(VWorld));

        private bool _UserContextOwner;
        private GameUserContext _UserContext;

        /// <summary>
        /// 获取用户数据库上下文。
        /// 如果是自动生成的(未赋值第一次读取时自动生成)，将在<see cref="Dispose"/>调用时自动处置。
        /// 如果设置该值，调用者需要自己处置上下文。
        /// </summary>
        public GameUserContext UserContext
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

        private bool _Disposed;

        /// <summary>
        /// 是否已经被处置。
        /// </summary>
        protected bool Disposed { get => _Disposed; }


        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
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
                _Disposed = true;
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
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class GameCharWorkDataBase : WorkDataBase
    {
        public const string Separator = "`";

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="gameChar"></param>
        protected GameCharWorkDataBase([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service)
        {
            _GameChar = gameChar;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="world"></param>
        /// <param name="gameChar"></param>
        protected GameCharWorkDataBase([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world)
        {
            _GameChar = gameChar;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="world"></param>
        /// <param name="token"></param>
        protected GameCharWorkDataBase([NotNull] VWorld world, [NotNull] string token) : base(world)
        {
            Token = GameHelper.FromBase64String(token);
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
        public IDisposable LockUser() => World.CharManager.LockAndReturnDispose(GameChar.GameUser);

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _GameChar = null;
                base.Dispose(disposing);
            }
        }
    }

    /// <summary>
    /// 涉及到多个角色的的功能函数使用的工作数据基类。
    /// </summary>
    public abstract class RelationshipWorkDataBase : GameCharWorkDataBase, IResultWorkData
    {
        protected RelationshipWorkDataBase([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, Guid otherGCharId) : base(service, gameChar)
        {
            _OtherCharId = otherGCharId;
        }

        protected RelationshipWorkDataBase([NotNull] VWorld world, [NotNull] GameChar gameChar, Guid otherGCharId) : base(world, gameChar)
        {
            _OtherCharId = otherGCharId;
        }

        protected RelationshipWorkDataBase([NotNull] VWorld world, [NotNull] string token, Guid otherGCharId) : base(world, token)
        {
            _OtherCharId = otherGCharId;
        }

        private readonly List<Guid> _OtherCharIds = new List<Guid>();

        /// <summary>
        /// 其他相关角色。
        /// </summary>
        public List<Guid> OtherCharIds => _OtherCharIds;

        /// <summary>
        /// 锁定所有角色。
        /// </summary>
        /// <returns></returns>
        public virtual IDisposable LockAll()
        {
            var coll = _OtherCharIds.Append(GameChar.Id);
            return World.CharManager.LockOrLoadWithCharIds(coll, World.CharManager.Options.DefaultLockTimeout);
        }

        private Guid _OtherCharId;
        public Guid OtherCharId { get => _OtherCharId; set => _OtherCharId = value; }

        public GameChar OtherChar { get => World.CharManager.GetCharFromId(OtherCharId); }

        #region IResultWorkData接口相关

        public bool HasError { get; set; }
        public int ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        #endregion IResultWorkData接口相关

        /// <summary>
        /// 锁定相关的角色对象。
        /// </summary>
        /// <returns></returns>
        public virtual IDisposable Lock()
        {
            var ary = new Guid[] { GameChar.Id, _OtherCharId };
            return World.CharManager.LockOrLoadWithCharIds(ary, World.CharManager.Options.DefaultLockTimeout);
        }

        private List<ChangeItem> _ChangeItems;

        /// <summary>
        /// 工作后，物品变化数据。
        /// 不同操作自行定义该属性内的内容。
        /// </summary>
        public List<ChangeItem> ChangeItems => _ChangeItems ??= new List<ChangeItem>();

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    if (null != _GameSocialRelationships)
                        _GameSocialRelationships.CollectionChanged -= new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnRelationshipsCollectionChanged);
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _GameSocialRelationships = null;
                _KeyTypes = null;
                base.Dispose(disposing);
            }
        }

        #region 关系数据相关
        private List<int> _KeyTypes;

        /// <summary>
        /// 关系数据类型限定。
        /// </summary>
        public List<int> KeyTypes => _KeyTypes ??= new List<int>();

        private ObservableCollection<GameSocialRelationship> _GameSocialRelationships;

        /// <summary>
        /// 相关的一组关系数据。
        /// </summary>
        public virtual ObservableCollection<GameSocialRelationship> SocialRelationships
        {
            get
            {
                if (_GameSocialRelationships is null)
                {
                    IQueryable<GameSocialRelationship> coll;
                    if (KeyTypes.Count > 0)
                        coll = from sr in UserContext.Set<GameSocialRelationship>()
                               where sr.Id == GameChar.Id && KeyTypes.Contains(sr.KeyType)
                               select sr;
                    else
                        coll = from sr in UserContext.Set<GameSocialRelationship>()
                               where sr.Id == GameChar.Id
                               select sr;
                    _GameSocialRelationships = new ObservableCollection<GameSocialRelationship>(coll);
                    _GameSocialRelationships.CollectionChanged += OnRelationshipsCollectionChanged;
                }
                return _GameSocialRelationships;
            }
        }

        /// <summary>
        /// 关系数据集合内容发生变化时发生。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnRelationshipsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    UserContext.AddRange(e.NewItems.OfType<GameSocialRelationship>());
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    UserContext.AddRange(e.OldItems.OfType<GameSocialRelationship>());
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    throw new NotSupportedException();
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    UserContext.AddRange(e.OldItems.OfType<GameSocialRelationship>());
                    UserContext.AddRange(e.NewItems.OfType<GameSocialRelationship>());
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                default:
                    break;
            }
        }

        #endregion 关系数据相关

        public virtual void Save()
        {
            UserContext.SaveChanges();
        }


    }

    /// <summary>
    /// <see cref="GameItem"/>的简要类。
    /// <see cref="Id"/>有非<see cref="Guid.Empty"/>则以Id为准，否则以<see cref="TemplateId"/>。
    /// </summary>
    public class GameItemSummery : SimpleExtendPropertyBase
    {
        public GameItemSummery() : base(Guid.Empty)
        {
        }

        public GameItemSummery(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public decimal? Count { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class GY001GameItemSummery : GameItemSummery
    {
        public GY001GameItemSummery()
        {
        }

        public GY001GameItemSummery(Guid id) : base(id)
        {
        }
        /// <summary>
        /// 头Id。
        /// </summary>
        public Guid? HeadTId { get; set; }

        /// <summary>
        /// 身体Id。
        /// </summary>
        public Guid? BodyTId { get; set; }

    }
}
