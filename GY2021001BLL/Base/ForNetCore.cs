using Game.Social;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OW.Game;
using OW.Game.Expression;
using OW.Game.Item;
using OW.Game.Mission;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
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
            using var scope = _Services.CreateScope();
            var service = scope.ServiceProvider;
            CreateDb(service);
            Test();
            var result = Task.Factory.StartNew(c =>
            {
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
                Task.Run(CreateGameManager);
                var logger = _Services.GetService<ILogger<GameHostedService>>();
                logger.LogInformation("游戏虚拟世界服务成功上线。");
            }, _Services, cancellationToken);
            return result;
        }

        private void SendMail()
        {
            var world = _Services.GetRequiredService<VWorld>();
            using var db = world.CreateNewUserDbContext();
            var gu = db.Set<GameUser>().FirstOrDefault(c => c.LoginName == "test100");
            while (gu is null)
            {
                Thread.Sleep(1000);
                gu = db.Set<GameUser>().FirstOrDefault(c => c.LoginName == "test100");
            }
            var gc = gu.GameChars.First();
            if (db.Set<GameMail>().Where(c => c.Addresses.Any(c1 => c1.ThingId == gc.Id)).Count() > 100)
                return;
            for (int i = 0; i < 100; i++)
            {
                var mail = new GameMail()
                {
                    Subject = $"测试邮件{i}",
                };
                mail.Attachmentes.Add(new GameMailAttachment()
                {
                    PropertiesString = $"TName=这是一个测试的附件对象,tid={ProjectConstant.JinbiId},ptid={ProjectConstant.CurrencyBagTId},count=100,desc=tid是送的物品模板id count是数量 ptid是放入容器的模板Id。",
                });

                world.SocialManager.SendMail(mail, new Guid[] { gu.GameChars.First().Id }, SocialConstant.FromSystemId);
            }
        }

        private void CreateNewUserAndChar()
        {
            Task.Run(SendMail);
#if DEBUG
            var maxCount = 1500;
#else
            var maxCount = 1500;
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
            var charTemplate = world.ItemTemplateManager.GetTemplateFromeId(ProjectConstant.CharTemplateId);
            var maxExp = (int)charTemplate.GetSequenceProperty<decimal>("expLimit").Last();
            //生成角色
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var gu = world.CharManager.CreateNewUserAndLock(item.Item1, item.Item1);
                if (gu is null)
                    continue;
                gu.CurrentChar.DisplayName = $"{item.Item1}";
                var zhanli = gu.CurrentChar.ExtendProperties.FirstOrDefault(c => c.Name == ProjectConstant.ZhangLiName);
                if (null != zhanli)
                {
                    zhanli.StringValue = gu.CurrentChar.DisplayName;
                }
                world.CharManager.SetExp(gu.CurrentChar,VWorld.WorldRandom.Next(maxExp));
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
            Trace.WriteLine("游戏虚拟世界服务开始下线。");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 测试点。
        /// </summary>
        /// <remarks> 
        /// CPU
        /// Intel(R) Core(TM) i5-10500 CPU @ 3.10GHz
        /// 基准速度:	3.10 GHz
        /// 插槽:	1
        /// 内核:	6
        /// 逻辑处理器:	12
        /// 虚拟化:	已启用
        /// L1 缓存:	384 KB
        /// L2 缓存:	1.5 MB
        /// L3 缓存:	12.0 MB
        /// </remarks>
        [Conditional("DEBUG")]
        private void Test()
        {
            var world = _Services.GetRequiredService<VWorld>();
            //using var db = world.CreateNewUserDbContext();
            var str1 = new Guid("893024d4-f02a-4a51-ba8e-0d8f2bba17d9").ToBase64String();
            var str2 = new Guid("e7148146-87d3-48c8-b0d5-675792320f01").ToBase64String();
        }

        /// <summary>
        /// 创建所有<see cref="VWorld"/>链接的游戏管理器以初始化。
        /// </summary>
        private void CreateGameManager()
        {
            var world = _Services.GetRequiredService<VWorld>();
            var coll = from pi in world.GetType().GetProperties().OfType<PropertyInfo>()
                       where pi.Name.EndsWith("Manager")
                       select pi;
            int succ = 0;
            foreach (var item in coll)
            {
                if (null != item.GetValue(world))
                    succ++;
            }
            var logger = _Services.GetService<ILogger<GameHostedService>>();
            try
            {
                var gitm = _Services.GetService<GameItemTemplateManager>();
                var templates = gitm.Id2Template;
            }
            catch (Exception err)
            {
                logger?.LogError($"{DateTime.UtcNow:s}初始化发生异常{err.Message}", err);
            }
            logger.LogInformation($"[{DateTime.UtcNow:s}]通用加速功能运行完毕。");
        }

        /// <summary>
        /// 升级数据库结构。
        /// </summary>
        /// <param name="services"></param>
        private void CreateDb(IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILogger<GameHostedService>>();
            try
            {
                var tContext = services.GetRequiredService<GY001TemplateContext>();
                TemplateMigrateDbInitializer.Initialize(tContext);
                logger.LogInformation($"{DateTime.UtcNow}模板数据库已正常升级。");

                var context = services.GetRequiredService<GY001UserContext>();
                MigrateDbInitializer.Initialize(context);
                logger.LogInformation($"{DateTime.UtcNow}用户数据库已正常升级。");
            }
            catch (Exception err)
            {
                logger.LogError(err, $"升级数据库出现错误——{err.Message}");
            }
        }

        #region 自动生成数据库迁移文件

        private void CreateDbTest(DbContext dbContext)
        {
            dbContext.Database.EnsureCreated();
            IModel lastModel = null;
            var lastMigration = dbContext.Set<MigrationLog>()
                    .OrderByDescending(e => e.Id)
                    .FirstOrDefault();
            lastModel = lastMigration == null ? null : (CreateModelSnapshot(lastMigration.SnapshotDefine).Result?.Model);

            var modelDiffer = dbContext.GetInfrastructure().GetService<IMigrationsModelDiffer>();
            var isDiff = modelDiffer.HasDifferences(lastModel, dbContext.Model); //这个方法返回值是true或者false，这个可以比较老版本的model和当前版本的model是否出现更改。

            var upOperations = modelDiffer.GetDifferences(lastModel, dbContext.Model);  //这个方法返回的迁移的操作对象。

            dbContext.GetInfrastructure().GetRequiredService<IMigrationsSqlGenerator>().Generate(upOperations, dbContext.Model).ToList();   //这个方法是根据迁移对象和当前的model生成迁移sql脚本。

        }

        public class MigrationLog
        {
            public Guid Id { get; set; }
            public string SnapshotDefine { get; internal set; }
        }

        private Task<ModelSnapshot> CreateModelSnapshot(string codedefine, DbContext db = null)
        {
            var ModuleDbContext = db.GetType();
            var ContextAssembly = ModuleDbContext.Assembly.FullName;
            string SnapshotName = "";
            // 生成快照，需要存到数据库中供更新版本用
            var references = ModuleDbContext.Assembly
                .GetReferencedAssemblies()
                .Select(e => MetadataReference.CreateFromFile(Assembly.Load(e).Location))
                .Union(new MetadataReference[]
                {
                    MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                    MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(ModuleDbContext.Assembly.Location)
                });

            var compilation = CSharpCompilation.Create(ContextAssembly)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(references)
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(codedefine));

            return Task.Run(() =>
            {
                using var stream = new MemoryStream();
                var compileResult = compilation.Emit(stream);
                return compileResult.Success
                    ? Assembly.Load(stream.GetBuffer()).CreateInstance(ContextAssembly + "." + SnapshotName) as ModelSnapshot
                    : null;
            });
        }

        #endregion 自动生成数据库迁移文件

    }

    public static class GameHostedServiceExtensions
    {
        public static void AddGameManagers(this IServiceCollection services)
        {
            services.AddHostedService<GameHostedService>();

            services.TryAddTransient<HashAlgorithm>(c => SHA512.Create());

            services.AddSingleton(c => new GameItemTemplateManager(c, new GameItemTemplateManagerOptions()
            {
                Loaded = SpecificProject.ItemTemplateLoaded,
            }));
            services.AddSingleton(c => new GameItemManager(c, new GameItemManagerOptions()
            {
                ItemCreated = SpecificProject.GameItemCreated,
            }));
            services.AddSingleton(c => new GameCharManager(c, new GameCharManagerOptions()
            {
            }));
            services.AddSingleton(c => new CombatManager(c, new CombatManagerOptions()
            {
                CombatStart = SpecificProject.CombatStart,
                CombatEnd = SpecificProject.CombatEnd,
            }));
            services.AddSingleton<GamePropertyHelper, GameManagerPropertyHelper>();
            services.AddSingleton(c => new BlueprintManager(c, new BlueprintManagerOptions()
            {
                DoApply = SpecificProject.ApplyBlueprint,
            }));
            services.AddSingleton<IGameThingHelper>(c => c.GetService<GameItemManager>());

            services.AddSingleton(c => new GameSocialManager(c, new SocialManagerOptions()));
            //加入任务/成就管理器
            services.AddSingleton(c => new GameMissionManager(c, new GameMissionManagerOptions()));

            //加入属性管理器
            services.AddSingleton<IGamePropertyManager>(c => new PropertyManager(c, new PropertyManagerOptions()));

            //services.AddSingleton<IGameObjectInitializer>(c => new Gy001Initializer(c, new Gy001InitializerOptions()));

            //加入事件管理器
            services.TryAddSingleton(c => new GameEventsManager(c, new GameEventsManagerOptions()));

            //加入任务管理器
            services.AddSingleton(c => new GameSchedulerManager(c, new SchedulerManagerOptions()));

            //加入管理员服务
            services.AddSingleton(c => new GameAdminManager(c, new AdminManagerOptions()));
        }
    }
}