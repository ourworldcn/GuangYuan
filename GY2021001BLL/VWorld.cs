using GY2021001DAL;
using Gy2021001Template;
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
    public class VWorld
    {
        public readonly DateTime StartDateTimeUtc = DateTime.UtcNow;

        private readonly IServiceProvider _ServiceProvider;

        CancellationTokenSource _CancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// 该游戏世界因为种种原因已经请求卸载。
        /// </summary>
        public CancellationToken RequestShutdown;

        public VWorld()
        {
            Initialize();
        }

        public VWorld(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
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

    }


}
