using GuangYuan.GY001.UserDb;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Generic;
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

        private string _DebugMessage;
        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string ErrorMessage
        {
            get => _DebugMessage;
            set => _DebugMessage = value;
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
        /// 使用默认超时试图锁定用户。
        /// </summary>
        /// <returns></returns>
        public IDisposable LockUser() => World.CharManager.LockAndReturnDispose(GameChar.GameUser);
    }

    /// <summary>
    /// 涉及到两个角色的的功能函数使用的工作数据基类。
    /// </summary>
    public abstract class RelationshipWorkDataBase : GameCharWorkDataBase, IResultWorkData
    {
        protected RelationshipWorkDataBase([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, [NotNull] GameChar otherChar) : base(service, gameChar)
        {
            _OtherChar = otherChar;
        }

        protected RelationshipWorkDataBase([NotNull] VWorld world, [NotNull] GameChar gameChar, [NotNull] GameChar otherChar) : base(world, gameChar)
        {
            _OtherChar = otherChar;
        }

        protected RelationshipWorkDataBase([NotNull] VWorld world, [NotNull] string token, [NotNull] GameChar otherChar) : base(world, token)
        {
            _OtherChar = otherChar;
        }

        private readonly GameChar _OtherChar;

        public GameChar OtherChar { get => _OtherChar; }

        #region IResultWorkData接口相关

        public bool HasError { get; set; }
        public int ErrorCode { get; set; }
        
        public string ErrorMessage { get; set; }

        #endregion IResultWorkData接口相关

        public IDisposable LockBoth()
        {
            var ary = new Guid[] { GameChar.Id, OtherChar.Id };
            return World.CharManager.LockOrLoadWithUserIds(ary, TimeSpan.FromSeconds(World.CharManager.Options.DefaultLockTimeoutInSeconds));
        }
    }

    /// <summary>
    /// Id集合的帮助器类。
    /// 场景，经常遇到要记录一组Id，且这些Id要记录最后刷新时间。
    /// 当日可能更改，非当日将导致会清理后更改。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class CounterWrapper<T> : IDisposable
    {
        /// <summary>
        /// 最后一次刷新结果的键名后缀。
        /// </summary>
        public const string LastValuesKeySuffix = "LastValues";

        /// <summary>
        /// 当日刷新的所有值的键名后缀。
        /// </summary>
        public const string TodayValuesKeySuffix = "TodayValues";

        /// <summary>
        /// 最后一次刷新日期键名后缀。
        /// </summary>
        public const string LastDateKeySuffix = "LastDate";

        /// <summary>
        /// 分隔符。
        /// </summary>
        public const string Separator = "`";

        public static CounterWrapper<T> Create([NotNull] SimpleExtendPropertyBase entity, [NotNull] string prefix, DateTime now)
        {
            return new CounterWrapper<T>(entity, prefix, now);
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="gameThing">保存在该对象的<see cref="SimpleExtendPropertyBase.Properties"/>属性中。</param>
        /// <param name="prefix">记录这些属性的前缀。</param>
        /// <param name="now">当前日期时间。</param>
        private CounterWrapper([NotNull] SimpleExtendPropertyBase entity, [NotNull] string prefix, DateTime now)
        {
            _Entity = entity;
            _Prefix = prefix;
            _Now = now;
        }

        private readonly SimpleExtendPropertyBase _Entity;
        private readonly string _Prefix;
        private readonly DateTime _Now;

        /// <summary>
        /// 记录最后一次值的键名。
        /// </summary>
        public string LastValuesKey => $"{_Prefix}{LastValuesKeySuffix}";

        /// <summary>
        /// 记录当日值的键名。
        /// </summary>
        public string TodayValuesKey => $"{_Prefix}{TodayValuesKeySuffix}";

        /// <summary>
        /// 最后刷新时间键名。
        /// </summary>
        public string LastDateKey => $"{_Prefix}{LastDateKeySuffix}";

        private List<T> _TodayValues;
        /// <summary>
        /// 今日所有数据。
        /// </summary>
        public List<T> TodayValues
        {
            get
            {
                if (_TodayValues is null)
                    if (!_Entity.Properties.ContainsKey(LastDateKey) || _Entity.Properties.GetDateTimeOrDefault(LastDateKey) != _Now)  //若已经需要刷新
                    {
                        _TodayValues = new List<T>();
                    }
                    else
                    {
                        string val = _Entity.Properties.GetStringOrDefault(TodayValuesKey);
                        if (string.IsNullOrWhiteSpace(val))  //若没有值
                            _TodayValues = new List<T>();
                        else
                        {
                            var converter = TypeDescriptor.GetConverter(typeof(T));
                            _TodayValues = val.Split(Separator).Select(c => (T)converter.ConvertFrom(c)).ToList();
                        }
                    }
                return _TodayValues;
            }
        }

        #region IDisposable接口及相关

        private bool _Disposed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _Disposed = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~CounterWrapper()
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

        #endregion IDisposable接口及相关
    }
}
