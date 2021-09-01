using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OW.Game.Expression;
using System;
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
            var world = _Services.GetRequiredService<VWorld>();
            var logger = _Services.GetService<ILogger<GameHostedService>>();
            int start = 1;
            using var db = world.CreateNewUserDbContext();
            {
                var loginNames = db.GameUsers.Where(c => c.LoginName.StartsWith("test")).Select(c => c.LoginName).ToArray();
                var suffs = loginNames.Select(c =>
                {
                    return int.TryParse(c.Replace("test", string.Empty), out var suff) ? suff : 0;
                }).OrderBy(c => c).ToArray();
                for (int i = 0; i < suffs.Length; i++)
                {
                    if (suffs[i] != i + 1)
                        break;
                    start = i + 1;
                }
            }
#if DEBUG
            var maxCount = 2000;
#else
            var maxCount = 25000;
#endif

            try
            {
                for (int i = start; i < maxCount; i++)
                {
                    try
                    {
                        var pwd = "test" + i.ToString();
                        var loginName = "test" + i;
                        if (db.GameUsers.Any(c => c.LoginName == loginName))
                        {
                            continue;
                        }
                        using var gu = world.CharManager.QuicklyRegister(ref pwd, loginName);
                        if (gu is null)
                        {
                            i--;
                            continue;
                        }
                        if (i % 100 == 0)
                        {
                            logger.LogDebug($"[{DateTime.UtcNow:s}]已经创建了{i}个账号。");
                        }
#if DEBUG
                        Thread.Sleep(i / 10);

#else
                        Thread.Sleep(i/10);
#endif
                    }
                    catch (DbUpdateException err)
                    {
                        logger.LogWarning($"创建账号出错已重试——{err.Message}");
                        Thread.Sleep(1);
                        i--;
                    }
                    Thread.Yield();
                }

            }
            catch (Exception err)
            {
                logger.LogWarning($"创建账号时出现未处理错误，将停止创建。——{err.Message}");
            }
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