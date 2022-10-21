using AutoMapper;
using Game.Logging;
using Game.Social;
using GuangYuan.GY001.BLL.GeneralManager;
using GuangYuan.GY001.BLL.Script;
using GuangYuan.GY001.BLL.Specific;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Social;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using OW.DDD;
using OW.Game;
using OW.Game.Caching;
using OW.Game.Entity.Log;
using OW.Game.Item;
using OW.Game.Log;
using OW.Game.Managers;
using OW.Game.Mission;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.Game.Validation;
using OW.Script;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
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
                Thread thread = new Thread(() => CreateNewUserAndChar())
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest,
                };
#if DEBUG
                thread.Start();
#else
                thread.Start();
#endif
                Task.Run(CreateGameManager);    //强制初始化所有服务以加速
                Task.Run(SetDbConfig);  //设置数据库配置项
                var logger = _Services.GetService<ILogger<GameHostedService>>();
                logger.LogTrace("游戏虚拟世界服务成功上线。");
            }, _Services, cancellationToken);

            #region 版本升级
            // TO DO应放入专门的版本管理服务中
            using var db = service.GetRequiredService<GY001UserContext>();
            var sql = $"{db.Model.FindEntityType(typeof(GameItem)).FindProperty(nameof(GameThingBase.ExtraGuid)).GetColumnName()}";
            db.Database.ExecuteSqlRaw($"DELETE FROM [dbo].[GameItems] WHERE [TemplateId]='{ProjectConstant.GuildSlotId}' and [ExtraString] is null;" +
                $"DELETE FROM[dbo].[GameItems] where [TemplateId] = '{ProjectConstant.GuildSlotId}' and[ExtraString] = ''");  //清理无效个人工会槽
            #endregion 版本升级
            return result;
        }

        /// <summary>
        /// 设置数据库选项。
        /// </summary>
        void SetDbConfig()
        {
            #region 设置sql server使用内存，避免sql server 贪婪使用内存导致内存过大

            var world = _Services.GetRequiredService<VWorld>();
            using var db = world.CreateNewUserDbContext();
            var sql = @$"EXEC sys.sp_configure N'show advanced options', N'1'  RECONFIGURE WITH OVERRIDE;" +
                "EXEC sys.sp_configure N'max server memory (MB)', N'4096';" +
                "RECONFIGURE WITH OVERRIDE;" +
                "EXEC sys.sp_configure N'show advanced options', N'0'  RECONFIGURE WITH OVERRIDE;";
            try
            {
                db.Database.ExecuteSqlRaw(sql);
            }
            catch (Exception)
            {
            }
            #endregion

            try
            {
                var tn = db.Model.FindEntityType(typeof(GameItem)).GetTableName();
                sql = "ALTER TABLE [dbo].[GameItems] REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = ROW);" +
                    "ALTER INDEX IX_GameItems_TemplateId_ExtraString_ExtraDecimal ON [dbo].[GameItems] REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE)";   //按行压缩

                //TODO 压缩索引
                var indexNames = new string[] { "IX_VirtualThings_ExtraGuid_ExtraString_ExtraDecimal", "IX_VirtualThings_ExtraGuid_ExtraDecimal_ExtraString" };
                var alterIndex = "ALTER INDEX {0} ON[dbo].[GameItems] REBUILD PARTITION = ALL WITH(DATA_COMPRESSION = PAGE); ";

                db.Database.ExecuteSqlRaw(sql);
                tn = db.Model.FindEntityType(typeof(VirtualThing)).GetTableName();
                if (tn != null)
                {
                    sql = $"ALTER TABLE {tn} REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = ROW);";
                    db.Database.ExecuteSqlRaw(sql);
                }
            }
            catch (Exception err)
            {
                Trace.WriteLine(err.Message);
            }
#if !DEBUG  //若正式运行版本

#endif
        }

        /// <summary>
        /// 给指定账号发送测试邮件。
        /// </summary>
        [Conditional("DEBUG")]
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

        /// <summary>
        /// 生成测试账号。
        /// </summary>
        private void CreateNewUserAndChar()
        {
            //Task.Run(() => SendMail());
            int maxCount = 0;
#if DEBUG
            maxCount = 20;
#else
           var env = _Services.GetRequiredService<IHostEnvironment>();
            if (env.EnvironmentName == "Staging_2")
                return;
            else if (env.EnvironmentName == "Test0706")
                 maxCount = 10;
            else
                return;
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
                world.CharManager.SetExp(gu.CurrentChar, VWorld.WorldRandom.Next(maxExp));
                gu.Timeout = TimeSpan.FromSeconds(1);
                //GameItem pvp = new GameItem() { Count = 1 };
                //world.EventsManager.GameItemCreated(pvp, ProjectConstant.PvpObjectTId);
                //world.ItemManager.MoveItem(pvp, pvp.Count.Value, gu.CurrentChar.GetCurrencyBag());
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
            logger.LogTrace("完成了测试账号生成。");
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
        /// 逻辑处理器:	12		DateTime.UtcNow.ToString("s")	"2022-03-08T02:59:32"	string
        /// 虚拟化:	已启用
        /// L1 缓存:	384 KB
        /// L2 缓存:	1.5 MB
        /// L3 缓存:	12.0 MB
        /// </remarks>
        [Conditional("DEBUG")]
        private void Test()
        {
            var world = _Services.GetRequiredService<VWorld>();
            using var db = world.CreateNewUserDbContext();

            var cache = world.Service.GetService<GameObjectCache>();
            using var scope = _Services.CreateScope();
            var service = scope.ServiceProvider;
            var id = Guid.NewGuid();
            var sw = Stopwatch.StartNew();
            try
            {
                Span<string> span = ArrayPool<string>.Shared.Rent(3);
                var span1 = span[1..3];

                var logger = _Services.GetRequiredService<ILogger<GameHostedService>>();
                logger.LogCritical("Test:LogCritical");
                var loggingDb = service.GetRequiredService<GameLoggingDbContext>();
                var entity = new PayOrder() { Id = Guid.NewGuid().ToString() };
                var dto = entity.GetJsonObject<PayCallbackT78ParamsDto>();
                dto.UserId = Guid.NewGuid().ToString();
                //loggingDb.Add(entity);
                loggingDb.SaveChanges();
            }
            catch (Exception)
            {
            }
            finally
            {
                sw.Stop();
                Debug.WriteLine($"测试代码完成时间{sw.Elapsed}");
            }
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
            logger.LogTrace("通用加速功能运行完毕。");
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
                logger.LogTrace($"模板数据库已正常升级。");

                var context = services.GetRequiredService<GY001UserContext>();
                MigrateDbInitializer.Initialize(context);
                logger.LogTrace("用户数据库已正常升级。");

                var loggingDb = services.GetService<GameLoggingDbContext>();
                if (loggingDb != null)
                {
                    GameLoggingMigrateDbInitializer.Initialize(loggingDb);
                    logger.LogTrace("日志数据库已正常升级。");
                }
            }
            catch (Exception err)
            {
                logger.LogError(err, $"升级数据库出现错误——{err.Message}");
            }
        }

        #region 自动生成数据库迁移文件

        //        private void CreateDbTest(DbContext dbContext)
        //        {
        //            dbContext.Database.EnsureCreated();
        //            IModel lastModel = null;
        //            var lastMigration = dbContext.Set<MigrationLog>()
        //                    .OrderByDescending(e => e.Id)
        //                    .FirstOrDefault();
        //            lastModel = lastMigration == null ? null : (CreateModelSnapshot(lastMigration.SnapshotDefine).Result?.Model);

        //            var modelDiffer = dbContext.GetInfrastructure().GetService<IMigrationsModelDiffer>();
        //            var isDiff = modelDiffer.HasDifferences(lastModel, dbContext.Model); //这个方法返回值是true或者false，这个可以比较老版本的model和当前版本的model是否出现更改。

        //            var upOperations = modelDiffer.GetDifferences(lastModel, dbContext.Model);  //这个方法返回的迁移的操作对象。

        //#pragma warning disable CA1806 // 不要忽略方法结果
        //            dbContext.GetInfrastructure().GetRequiredService<IMigrationsSqlGenerator>().Generate(upOperations, dbContext.Model).ToList();   //这个方法是根据迁移对象和当前的model生成迁移sql脚本。
        //#pragma warning restore CA1806 // 不要忽略方法结果

        //        }

        public class MigrationLog
        {
            public Guid Id { get; set; }
            public string SnapshotDefine { get; internal set; }
        }

        //private Task<ModelSnapshot> CreateModelSnapshot(string codedefine, DbContext db = null)
        //{
        //    var ModuleDbContext = db.GetType();
        //    var ContextAssembly = ModuleDbContext.Assembly.FullName;
        //    string SnapshotName = "";
        //    // 生成快照，需要存到数据库中供更新版本用
        //    var references = ModuleDbContext.Assembly
        //        .GetReferencedAssemblies()
        //        .Select(e => MetadataReference.CreateFromFile(Assembly.Load(e).Location))
        //        .Union(new MetadataReference[]
        //        {
        //            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
        //            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
        //            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        //            MetadataReference.CreateFromFile(ModuleDbContext.Assembly.Location)
        //        });

        //    var compilation = CSharpCompilation.Create(ContextAssembly)
        //        .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
        //        .AddReferences(references)
        //        .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(codedefine));

        //    return Task.Run(() =>
        //    {
        //        using var stream = new MemoryStream();
        //        var compileResult = compilation.Emit(stream);
        //        return compileResult.Success
        //            ? Assembly.Load(stream.GetBuffer()).CreateInstance(ContextAssembly + "." + SnapshotName) as ModelSnapshot
        //            : null;
        //    });
        //}

        #endregion 自动生成数据库迁移文件

    }

    public static class GameHostedServiceExtensions
    {
        /// <summary>
        /// 向指定服务容器添加游戏用到的各种服务。
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddGameServices(this IServiceCollection services)
        {
            #region 基础服务

            services.TryAddTransient<HashAlgorithm>(c => SHA512.Create());  //Hash服务

            services.TryAddSingleton(c => ArrayPool<byte>.Create());    //字节数组池服务

            services.TryAddSingleton(c => StringBuilderPool.Shared);  //StringBuilder池服务

            services.AddMemoryCache(c =>
            {
                c.SizeLimit = null;
                c.ExpirationScanFrequency = TimeSpan.FromSeconds(10);
            });

            services.AddSingleton<GameObjectCache>();

            services.AddCommandManager().RegisterCommandHandler(AppDomain.CurrentDomain.GetAssemblies());
            services.AddOwEventBus().RegisterNotificationHandler(AppDomain.CurrentDomain.GetAssemblies());


            services.UseGameCommand(AppDomain.CurrentDomain.GetAssemblies());

            #endregion 基础服务

            #region 游戏专用服务

            services.AddSingleton(c => new VirtualThingManager(c, new VirtualThingManagerOptions()));

            services.AddHostedService<GameHostedService>();

            services.AddSingleton(c => new VWorld(c, new VWorldOptions()
            {
                //#if DEBUG
                //                UserDbOptions = new DbContextOptionsBuilder<GY001UserContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString)/*.UseLoggerFactory(LoggerFactory)*/.EnableSensitiveDataLogging().Options,
                //                TemplateDbOptions = new DbContextOptionsBuilder<GY001TemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString)/*.UseLoggerFactory(LoggerFactory)*/.Options,
                //#else
                //                UserDbOptions = new DbContextOptionsBuilder<GY001UserContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging().Options,
                //                TemplateDbOptions = new DbContextOptionsBuilder<GY001TemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).Options,
                //#endif //DEBUG
            }));
            services.AddSingleton(c => new ChatManager(c, new ChatManagerOptions()
            {
            }));
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
            services.AddSingleton(c => new GameCombatManager(c, new GameCombatManagerOptions()
            {
                CombatStart = SpecificProject.CombatStart,
                CombatEnd = SpecificProject.CombatEnd,
            }));
            services.AddSingleton(c => new BlueprintManager(c, new BlueprintManagerOptions()
            {
                DoApply = SpecificProject.ApplyBlueprint,
            }));

            services.AddSingleton(c => new GameSocialManager(c, new SocialManagerOptions()));
            //加入任务/成就管理器
            services.AddSingleton(c => new GameMissionManager(c, new GameMissionManagerOptions()));

            //加入属性管理器
            services.AddSingleton<IGamePropertyManager>(c => new GamePropertyManager(c, new PropertyManagerOptions()));

            //services.AddSingleton<IGameObjectInitializer>(c => new Gy001Initializer(c, new Gy001InitializerOptions()));

            //加入事件管理器
            services.TryAddSingleton(c => new GameEventsManager(c, new GameEventsManagerOptions()));

            //加入任务管理器
            services.AddSingleton(c => new GameSchedulerManager(c, new SchedulerManagerOptions()));

            //加入管理员服务
            services.AddSingleton(c => new GameAdminManager(c, new AdminManagerOptions()));

            //加入商城服务
            services.AddSingleton(c => new GameShoppingManager(c, new GameShoppingManagerOptions { }));

            //加入脚本服务
            services.AddSingleton(c => new GameScriptManager(c, new GameScriptManagerOptions { }));

            //加入属性变化管理器
            services.TryAddSingleton(c => new GamePropertyChangeManager(c, new GamePropertyChangeManagerOptions() { }));

            //加入联盟/工会管理器
            services.TryAddSingleton(c => new GameAllianceManager(c, new GameAllianceManagerOptions() { }));


            #endregion  游戏专用服务

            return services;
        }
    }
}