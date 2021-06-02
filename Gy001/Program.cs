using GY2021001BLL;
using GY2021001DAL;
using Gy2021001Template;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OwGame;
using OwGame.Expression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gy001
{
    public class Program
    {
        static private IHost _Host;

        public IHost DefaultHost => _Host;


        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            _Host = host;
            CreateDb(_Host);
            LoadCache(_Host);
            Test();
            _Host.Run();
        }

        /// <summary>
        /// 测试点。
        /// </summary>
        private static void Test()
        {
            var env = new GameExpressionCompileEnvironment();
            GameExpressionBase.CompileVariableDeclare(env, "a1=1,a2= BB040B31-6D00-427F-B158-3D6D7CE92B18,a3=\"str\", a4=5.5, a5 = rnd()");
            var exp = GameExpressionBase.CompileExpression(env, "a1=a4>=5");
            var envr = new GameExpressionRuntimeEnvironment();
            env.Variables.All(c =>
            {
                envr.Variables[c.Key] = c.Value; return true;
            });
            var result = exp.GetValueOrDefault(envr);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        /// <summary>
        /// 加载缓存。
        /// </summary>
        /// <param name="host"></param>
        private static void LoadCache(IHost host)
        {
            var gitm = host.Services.GetService<GameItemTemplateManager>();
            var bptm = host.Services.GetService<BlueprintManager>();
            var test = host.Services.GetService<GamePropertyHelper>();
        }

        /// <summary>
        /// 创建数据库。
        /// </summary>
        /// <param name="host"></param>
        private static void CreateDb(IHost host)
        {

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var tContext = services.GetRequiredService<GameTemplateContext>();
                    TemplateMigrateDbInitializer.Initialize(tContext);
                    var context = services.GetRequiredService<GY2021001DbContext>();
                    MigrateDbInitializer.Initialize(context);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred creating the DB.");
                }
            }
        }

    }
}
