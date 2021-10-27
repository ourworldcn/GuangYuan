using GuangYuan.GY001.UserDb;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

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
    /// 错误码封装。
    /// </summary>
    public static class ErrorCodes
    {
        public const int NO_ERROR = 0;
        public const int WAIT_TIMEOUT = 258;
        public const int ERROR_INVALID_TOKEN = 315;
        public const int ERROR_NO_SUCH_USER = 1317;
        /// <summary>
        /// 并发或交错操作更改了对象的状态，使此操作无效。
        /// </summary>
        public const int E_CHANGED_STATE = unchecked((int)0x8000000C);
        public const int Unauthorized = unchecked((int)0x80190191);
        public const int RO_E_CLOSED = unchecked((int)0x80000013);
        public const int ObjectDisposed = RO_E_CLOSED;

        /// <summary>
        /// 参数错误。One or more arguments are not correct.
        /// </summary>
        public const int ERROR_BAD_ARGUMENTS = 160;

        /// <summary>
        /// 没有足够资源完成操作。
        /// </summary>
        public const int RPC_S_OUT_OF_RESOURCES = 1721;

        /// <summary>
        /// 没有足够的配额来处理此命令。通常是超过某些次数的限制。
        /// </summary>
        public const int ERROR_NOT_ENOUGH_QUOTA = 1816;

        /// <summary>
        /// The data is invalid.
        /// </summary>
        public const int ERROR_INVALID_DATA = 13;

        /// <summary>
        /// 操作试图超过实施定义的限制。
        /// </summary>
        public const int ERROR_IMPLEMENTATION_LIMIT = 1292;
    }

    /// <summary>
    /// 带详细信息的返回值。
    /// </summary>
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

    }

    public static class ResultWorkDataExtensions
    {
        /// <summary>
        /// 从<see cref="VWorld"/>对象获取错误信息。
        /// </summary>
        /// <param name="obj"></param>
        public static void FillErrorFromWorld(this IResultWorkData obj)
        {
            obj.ErrorCode = VWorld.GetLastError();
            obj.ErrorMessage = VWorld.GetLastErrorMessage();

        }
    }

    #region 工作数据基类

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


        #endregion 出参

        [Conditional("DEBUG")]
        public void SetDebugMessage(string msg) => ErrorMessage = msg;

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
        public VWorld World => _World ??= _Service.GetService(typeof(VWorld)) as VWorld;

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

        private bool _IsDisposed;

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
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class GameCharWorkDataBase : WorkDataBase, IResultWorkData
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
        public IDisposable LockUser()
        {
            try
            {
                return World.CharManager.LockAndReturnDisposer(GameChar.GameUser);
            }
            catch (Exception err)
            {
                var logger = Service?.GetService<ILogger<GameCharWorkDataBase>>();
                logger?.LogWarning($"锁定单个角色时出现异常——{err.Message}");
                return null;
            }
        }

        #region IResultWorkData接口相关

        public bool HasError { get; set; }
        public int ErrorCode { get; set; }

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
                _GameChar = null;
                base.Dispose(disposing);
            }
        }
    }

    /// <summary>
    /// 涉及到多个角色的的功能函数使用的工作数据基类。
    /// </summary>
    public abstract class BinaryRelationshipWorkDataBase : GameCharWorkDataBase
    {
        protected BinaryRelationshipWorkDataBase([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, Guid otherGCharId) : base(service, gameChar)
        {
            _OtherCharId = otherGCharId;
        }

        protected BinaryRelationshipWorkDataBase([NotNull] VWorld world, [NotNull] GameChar gameChar, Guid otherGCharId) : base(world, gameChar)
        {
            _OtherCharId = otherGCharId;
        }

        protected BinaryRelationshipWorkDataBase([NotNull] VWorld world, [NotNull] string token, Guid otherGCharId) : base(world, token)
        {
            _OtherCharId = otherGCharId;
        }

        private readonly List<Guid> _OtherCharIds = new List<Guid>();

        /// <summary>
        /// 其他相关角色。
        /// </summary>
        public List<Guid> OtherCharIds => _OtherCharIds;

        private Guid _OtherCharId;
        /// <summary>
        /// 二元关系中，另一个角色的Id。
        /// </summary>
        public Guid OtherCharId { get => _OtherCharId; set => _OtherCharId = value; }

        public GameChar OtherChar { get => World.CharManager.GetCharFromId(OtherCharId); }

        /// <summary>
        /// 锁定所有的角色对象。
        /// </summary>
        /// <returns></returns>
        public virtual IDisposable LockAll()
        {
            try
            {
                var result = World.CharManager.LockOrLoadWithCharIds(AllCharIds, World.CharManager.Options.DefaultLockTimeout * AllCharIds.Count * 0.8);
                if (result is null)
                {
                    this.FillErrorFromWorld();
                }
                return result;
            }
            catch (Exception err)
            {
                var logger = Service?.GetService<ILogger<BinaryRelationshipWorkDataBase>>();
                logger?.LogWarning($"锁定多个角色时出现异常——{err.Message}");
                return null;
            }
        }

        private List<Guid> _AllCharIds;

        /// <summary>
        /// 组合所有相关角色Id，用于<see cref="LockAll"/>
        /// </summary>
        public List<Guid> AllCharIds
        {
            get
            {
                if (_AllCharIds is null)
                {
                    _AllCharIds = new List<Guid>(OtherCharIds)
                    {
                        GameChar.Id,
                        _OtherCharId
                    };
                    _AllCharIds.AddRange(OtherCharIds);
                }
                return _AllCharIds;
            }
        }

        private List<ChangeItem> _ChangeItems;

        /// <summary>
        /// 工作后，物品变化数据。
        /// 不同操作自行定义该属性内的内容。
        /// </summary>
        public List<ChangeItem> ChangeItems => _ChangeItems ??= new List<ChangeItem>();

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    if (null != _SocialRelationships)
                        _SocialRelationships.CollectionChanged -= new NotifyCollectionChangedEventHandler(OnRelationshipsCollectionChanged);
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _SocialRelationships = null;
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

        private ObservableCollection<GameSocialRelationship> _SocialRelationships;

        /// <summary>
        /// 相关的一组关系数据。
        /// </summary>
        public virtual ObservableCollection<GameSocialRelationship> SocialRelationships
        {
            get
            {
                if (_SocialRelationships is null)
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
                    _SocialRelationships = new ObservableCollection<GameSocialRelationship>(coll);
                    _SocialRelationships.CollectionChanged += OnRelationshipsCollectionChanged;
                }
                return _SocialRelationships;
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
                    UserContext.RemoveRange(e.OldItems.OfType<GameSocialRelationship>());
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    throw new NotSupportedException();
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    UserContext.RemoveRange(e.OldItems.OfType<GameSocialRelationship>());
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
    /// 涉及多个次要角色的工作数据基类。
    /// </summary>
    public class RelationshipWorkDataBase : GameCharWorkDataBase
    {
        public RelationshipWorkDataBase([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public RelationshipWorkDataBase([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public RelationshipWorkDataBase([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        private readonly List<Guid> _OtherCharIds = new List<Guid>();

        /// <summary>
        /// 其他相关角色。
        /// </summary>
        public List<Guid> OtherCharIds => _OtherCharIds;

        /// <summary>
        /// 锁定所有的角色对象。
        /// </summary>
        /// <returns></returns>
        public virtual IDisposable LockAll()
        {
            try
            {
                var result = World.CharManager.LockOrLoadWithCharIds(OtherCharIds.Append(GameChar.Id), World.CharManager.Options.DefaultLockTimeout * OtherCharIds.Count * 0.8);
                if (result is null)
                {
                    this.FillErrorFromWorld();
                }
                return result;
            }
            catch (Exception err)
            {
                var logger = Service?.GetService<ILogger<BinaryRelationshipWorkDataBase>>();
                logger?.LogWarning($"锁定多个角色时出现异常——{err.Message}");
                return null;
            }
        }



    }

    #endregion 工作数据基类

    /// <summary>
    /// <see cref="GameItem"/>的简要类。
    /// <see cref="Id"/>有非<see cref="Guid.Empty"/>则以Id为准，否则以<see cref="TemplateId"/>。
    /// </summary>
    public class GameItemSummery : SimpleDynamicPropertyBase
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
