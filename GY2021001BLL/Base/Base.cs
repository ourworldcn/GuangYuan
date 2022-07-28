using GuangYuan.GY001.UserDb;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OW.Game
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
        /// <summary>
        /// 超时，没有在指定时间内完成操作，通常是锁定超时。
        /// </summary>
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
        /// 没有足够的权限来完成请求的操作
        /// </summary>
        public const int ERROR_NO_SUCH_PRIVILEGE = 1313;

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

        /// <summary>
        /// 无效的账号名称。
        /// </summary>
        public const int ERROR_INVALID_ACCOUNT_NAME = 1315;

        /// <summary>
        /// 指定账号已经存在。
        /// </summary>
        public const int ERROR_USER_EXISTS = 1316;

        /// <summary>
        /// 用户名或密码错误。
        /// </summary>
        public const int ERROR_LOGON_FAILURE = 1326;

        /// <summary>
        /// 无效的ACL——权限令牌包含的权限不足,权限不够。
        /// </summary>
        public const int ERROR_INVALID_ACL = 1336;

        /// <summary>
        /// 无法登录，通常是被封停账号。
        /// </summary>
        public const int ERROR_LOGON_NOT_GRANTED = 1380;
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

        /// <summary>
        /// 从另一个对象填充错误。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="src"></param>
        public static void FillErrorFrom(this IResultWorkData obj, IResultWorkData src)
        {
            obj.ErrorCode = src.ErrorCode;
            obj.ErrorMessage = src.ErrorMessage;
            obj.HasError = src.HasError;
        }
    }

    #region 工作数据基类

    /// <summary>
    /// 复杂工作的参数返回值封装类的基类。
    /// </summary>
    public abstract class ComplexWorkGameContext : GameCharGameContext, IResultWorkData
    {
        protected ComplexWorkGameContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        protected ComplexWorkGameContext([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        protected ComplexWorkGameContext([NotNull] VWorld world, [NotNull] string token) : base(world, token)
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
    /// 涉及到两个角色的的功能函数使用的工作数据基类。
    /// </summary>
    public abstract class BinaryRelationshipGameContext : GameCharGameContext
    {
        protected BinaryRelationshipGameContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, Guid otherGCharId) : base(service, gameChar)
        {
            _OtherCharId = otherGCharId;
        }

        protected BinaryRelationshipGameContext([NotNull] VWorld world, [NotNull] GameChar gameChar, Guid otherGCharId) : base(world, gameChar)
        {
            _OtherCharId = otherGCharId;
        }

        protected BinaryRelationshipGameContext([NotNull] VWorld world, [NotNull] string token, Guid otherGCharId) : base(world, token)
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
                var logger = Service?.GetService<ILogger<BinaryRelationshipGameContext>>();
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
                    // 释放托管状态(托管对象)
                    if (null != _SocialRelationships)
                        _SocialRelationships.CollectionChanged -= new NotifyCollectionChangedEventHandler(OnRelationshipsCollectionChanged);
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
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
                        coll = from sr in UserDbContext.Set<GameSocialRelationship>()
                               where sr.Id == GameChar.Id && KeyTypes.Contains(sr.KeyType)
                               select sr;
                    else
                        coll = from sr in UserDbContext.Set<GameSocialRelationship>()
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
        protected virtual void OnRelationshipsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    UserDbContext.AddRange(e.NewItems.OfType<GameSocialRelationship>());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    UserDbContext.RemoveRange(e.OldItems.OfType<GameSocialRelationship>());
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotSupportedException();
                case NotifyCollectionChangedAction.Reset:
                    UserDbContext.RemoveRange(e.OldItems.OfType<GameSocialRelationship>());
                    UserDbContext.AddRange(e.NewItems.OfType<GameSocialRelationship>());
                    break;
                case NotifyCollectionChangedAction.Move:
                default:
                    break;
            }
        }

        #endregion 关系数据相关

        public override void Save()
        {
            base.Save();
        }

    }

    /// <summary>
    /// 涉及多个次要角色的工作数据基类。
    /// </summary>
    public class RelationshipGameContext : GameCharGameContext
    {
        public RelationshipGameContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public RelationshipGameContext([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public RelationshipGameContext([NotNull] VWorld world, [NotNull] string token) : base(world, token)
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
                var logger = Service?.GetService<ILogger<BinaryRelationshipGameContext>>();
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
