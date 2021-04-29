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
        public DbContextOptions<GY2021001DbContext> DbContextOptions { get; set; }
    }

    /// <summary>
    /// 游戏世界的服务。
    /// </summary>
    public class VWorld
    {
        public readonly DateTime StartDateTimeUtc = DateTime.UtcNow;

        private readonly IServiceProvider _ServiceProvider;

        private readonly VWorldOptions _VWorldOptions;

        CancellationTokenSource _CancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// 该游戏世界因为种种原因已经请求卸载。
        /// </summary>
        public CancellationToken RequestShutdown;

        public VWorld()
        {
            Initialize();
        }

        public VWorld(IServiceProvider serviceProvider, VWorldOptions options)
        {
            _ServiceProvider = serviceProvider;
            _VWorldOptions = options;
            Initialize();
        }

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
            return new GY2021001DbContext(_VWorldOptions.DbContextOptions);
        }
    }


}
