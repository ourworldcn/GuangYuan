using GY2021001DAL;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OwGame;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace GY2021001BLL
{
    public class VWorldOptions
    {
        public DbContextOptions<GY2021001DbContext> UserDbOptions { get; set; }
        public DbContextOptions<GameTemplateContext> TemplateDbOptions { get; set; }
    }

    /// <summary>
    /// 游戏世界的服务。
    /// </summary>
    public class VWorld : GameManagerBase<VWorldOptions>
    {
        public readonly DateTime StartDateTimeUtc = DateTime.UtcNow;

        CancellationTokenSource _CancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// 该游戏世界因为种种原因已经请求卸载。
        /// </summary>
        public CancellationToken RequestShutdown;

        #region 构造函数

        public VWorld()
        {
            Initialize();
        }

        public VWorld(IServiceProvider serviceProvider, VWorldOptions options) : base(serviceProvider, options)
        {
            Initialize();
        }

        #endregion 构造函数

        #region 属性及相关
        private GameItemTemplateManager _ItemTemplateManager;
        public GameItemTemplateManager ItemTemplateManager { get => _ItemTemplateManager ??= Service.GetRequiredService<GameItemTemplateManager>(); }

        private GameCharManager _GameCharManager;
        public GameCharManager CharManager { get => _GameCharManager ??= Service.GetRequiredService<GameCharManager>(); }

        private CombatManager _CombatManager;
        public CombatManager CombatManager { get => _CombatManager ??= Service.GetRequiredService<CombatManager>(); }

        private GameItemManager _GameItemManager;

        public GameItemManager ItemManager { get => _GameItemManager ??= Service.GetRequiredService<GameItemManager>(); }
        #endregion 属性及相关

        private void Initialize()
        {
            RequestShutdown = _CancellationTokenSource.Token;
        }

        public TimeSpan GetServiceTime()
        {
            return DateTime.UtcNow - StartDateTimeUtc;
        }

        /// <summary>
        /// 通知游戏世界开始下线。
        /// </summary>
        public void NotifyShutdown()
        {
            _CancellationTokenSource.Cancel();
        }

        /// <summary>
        /// 新建一个用户数据库的上下文对象。
        /// 调用者需要自行负责清理对象。
        /// </summary>
        /// <returns></returns>
        public GY2021001DbContext CreateNewUserDbContext()
        {
            return new GY2021001DbContext(Options.UserDbOptions);
        }

        /// <summary>
        /// 创建模板数据库上下文对象。
        /// 调用者需要自行负责清理对象。
        /// </summary>
        /// <returns></returns>
        public GameTemplateContext CreateNewTemplateDbContext()
        {
            return new GameTemplateContext(Options.TemplateDbOptions);
        }
    }


}
