using GY2021001DAL;
using Gy2021001Template;
using Microsoft.Extensions.DependencyInjection;
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

        /// <summary>
        /// 按照指定模板创建一个对象。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="parentId">指定一个父Id,如果不指定或为null则忽略。</param>
        /// <returns></returns>
        public GameItem CreateGameItem(GameItemTemplate template, Guid? parentId = null)
        {
            _ServiceProvider.GetRequiredService<GameItemTemplateManager>();
            var result = new GameItem()
            {
                TemplateId = template.Id,
                PropertiesString = template.PropertiesString,
            };
            if (parentId != null && parentId.HasValue)
                result.UserId = parentId;
            return result;
        }
    }


}
