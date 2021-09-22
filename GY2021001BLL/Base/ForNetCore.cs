using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OW.Game;
using OW.Game.Expression;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GuangYuan.GY001.BLL
{
    /// <summary>
    /// 游戏的服务主机。
    /// </summary>
    public class GameHostedService : IHostedService
    {
        private readonly IServiceProvider _Services;

        public GameHostedService(IServiceProvider services)
        {
            _Services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var result = Task.Factory.StartNew(c =>
            {
                //CreateDb((IServiceProvider)c);
                Thread thread = new Thread(CreateNewUserAndChar)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest,
                };
#if DEBUG
                thread.Start();
#else
               thread.Start();
#endif
            }, _Services, cancellationToken);
            return result;
        }

        private void CreateNewUserAndChar()
        {
#if DEBUG
            var maxCount = 5000;
#else
            var maxCount = 25000;
#endif
            var world = _Services.GetRequiredService<VWorld>();
            var logger = _Services.GetService<ILogger<GameHostedService>>();

            List<(string, string)> list = new List<(string, string)>(maxCount);
            using (var db = world.CreateNewUserDbContext())
            {
                //生成登录名
                for (int i = 0; i < maxCount; i++)  //生成登录名
                {
                    list.Add(($"test{i + 1}", null));
                }
                HashSet<string> hsLn = new HashSet<string>(db.GameUsers.Where(c => c.LoginName.StartsWith("test")).Select(c => c.LoginName));  //获取已有登录名
                list.RemoveAll(c => hsLn.Contains(c.Item1));    //去除已有登录名
                                                                //生成角色昵称
                HashSet<string> hsCharNames = new HashSet<string>(db.GameChars.Select(c => c.DisplayName));  //获取已有角色名
                for (int i = 0; i < list.Count; i++)  //生成角色昵称
                {
                    string displayName;
                    for (displayName = CnNames.GetName(VWorld.IsHit(0.5)); !hsCharNames.Add(displayName); displayName = CnNames.GetName(VWorld.IsHit(0.5))) ;
                    list[i] = (list[i].Item1, displayName);
                }
            }
            //生成角色
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var gu = world.CharManager.CreateNewUserAndLock(item.Item1, item.Item1);
                if (gu is null)
                    continue;
                gu.CurrentChar.DisplayName = item.Item2;
                gu.Timeout = TimeSpan.FromSeconds(1);
                world.CharManager.Unlock(gu);

                if (i % 100 == 0 && i > 0)   //每n个账号
                {
                    try
                    {
                        logger.LogDebug($"[{DateTime.UtcNow:s}]已经创建了{i + 1}个账号。");
                        while (world.CharManager.Id2GameChar.Count > 10000)
                        {
                            Thread.Sleep(5000);
                        }
                    }
                    catch (Exception err)
                    {
                        logger.LogWarning($"创建账号出错已重试——{err.Message}");
                    }
                }

            }
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            logger.LogInformation("完成了测试账号生成。");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 加载缓存。
        /// </summary>
        private void LoadCache()
        {
            var logger = _Services.GetService<ILogger<GameItemTemplateManager>>();
            try
            {
                var gitm = _Services.GetService<GameItemTemplateManager>();
                var templates = gitm.Id2Template;
                logger?.LogInformation($"{DateTime.UtcNow:s}服务已上线，初始化加载{templates.Count}个模板数据进行缓存。");
            }
            catch (Exception err)
            {
                logger?.LogError($"{DateTime.UtcNow:s}初始化发生异常{err.Message}", err);
            }

            _Services.GetService<BlueprintManager>();
            _Services.GetService<GamePropertyHelper>();
        }

        private void CreateDb(IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILogger<GameHostedService>>();
            try
            {
                var tContext = services.GetRequiredService<GY001TemplateContext>();
                TemplateMigrateDbInitializer.Initialize(tContext);
                logger.LogInformation($"{DateTime.UtcNow}用户数据库已正常升级。");

                var context = services.GetRequiredService<GY001UserContext>();
                MigrateDbInitializer.Initialize(context);
                logger.LogInformation($"{DateTime.UtcNow}用户数据库已正常升级。");
            }
            catch (Exception err)
            {
                logger.LogError(err, $"An error occurred creating the DB.{err.Message}");
            }
        }

    }

}