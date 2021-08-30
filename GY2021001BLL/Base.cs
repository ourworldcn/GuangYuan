using GuangYuan.GY001.UserDb;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

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
        public IServiceProvider Services => _Services;

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
        /// <param name="service"></param>
        public GameManagerBase(IServiceProvider service)
        {
            _Services = service;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        public GameManagerBase(IServiceProvider service, TOptions options)
        {
            _Services = service;
            _Options = options;
        }

        #endregion 构造函数

    }

    /// <summary>
    /// 复杂工作的参数返回值封装类的基类。
    /// </summary>
    public abstract class ComplexWorkDatsBase : IDisposable
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ComplexWorkDatsBase()
        {

        }

        #region 入参


        /// <summary>
        /// 登录角色的令牌。
        /// </summary>
        public Guid Token { get; set; }

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

        private GameUser _GameUser;

        /// <summary>
        /// 当前角色对象。
        /// 成功调用<see cref="LockUser(GameCharManager)"/>之后才能获取有效对象，否则返回 <see cref="null"/>。
        /// </summary>
        public GameChar GameChar => _GameUser.CurrentChar;
        #endregion 入参

        #region 出参

        /// <summary>
        /// 返回的参数。
        /// 不同操作自行定义该属性内的内容。
        /// </summary>
        public Dictionary<string, object> Result { get; } = new Dictionary<string, object>();

        [Conditional("DEBUG")]
        public void SetDebugMessage(string msg) => DebugMessage = msg;

        private string _DebugMessage;
        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage
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
        public int ResultCode { get; set; }

        #endregion 出参

        /// <summary>
        /// 设置令牌并试图锁定。
        /// </summary>
        /// <param name="tokenString"></param>
        /// <param name="manager"></param>
        /// <returns>清理锁定的帮助器，如果失败则返回null。</returns>
        public IDisposable SetTokenStringAndLock(string tokenString, GameCharManager manager)
        {
            try
            {
                Token = GameHelper.FromBase64String(tokenString);
                return LockUser(manager);
            }
            catch (FormatException)
            {
                HasError = true;
                DebugMessage = $"令牌格式错误，TokenString={tokenString}";
                ResultCode = (int)HttpStatusCode.BadRequest;
                return null;
            }
        }

        private GameCharManager _Manager;

        /// <summary>
        /// 获取使用的角色管理器。
        /// </summary>
        public GameCharManager Manager => _Manager;

        /// <summary>
        /// 试图锁定用户。
        /// </summary>
        /// <param name="manager"></param>
        /// <returns>返回释放器，如果锁定失败则返回null,并填写必要的错误信息。</returns>
        public IDisposable LockUser(GameCharManager manager)
        {
            if (manager.Lock(Token, out _GameUser))
            {
                _Manager = manager;
                return new DisposerWrapper(() => manager.Unlock(_GameUser));
            }
            else
            {
                DebugMessage = $"无法锁定用户，Token={Token}";
                HasError = true;
                ResultCode = (int)HttpStatusCode.Unauthorized;
                return null;
            }
        }
        #region IDisposable接口

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~ComplexWorkDatsBase()
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
        #endregion IDisposable接口
    }

    /// <summary>
    /// 带变化物品返回值的类的接口。
    /// </summary>
    public abstract class ChangeItemsWorkDatsBase : ComplexWorkDatsBase
    {
        public ChangeItemsWorkDatsBase()
        {

        }

        private List<ChangeItem> _ChangeItems;

        /// <summary>
        /// 工作后，物品变化数据。
        /// 不同操作自行定义该属性内的内容。
        /// </summary>
        public List<ChangeItem> ChangeItems => GetOrAdd(nameof(_ChangeItems), ref _ChangeItems);
    }

    /// <summary>
    /// 带变化物品和发送邮件返回值的类的接口
    /// </summary>
    public abstract class ChangeItemsAndMailWorkDatsBase : ChangeItemsWorkDatsBase
    {
        public ChangeItemsAndMailWorkDatsBase()
        {

        }

        private List<Guid> _MailIds;

        /// <summary>
        /// 工作后发送邮件的邮件Id。
        /// </summary>
        public List<Guid> MailIds => GetOrAdd(nameof(MailIds), ref _MailIds);

    }
}
