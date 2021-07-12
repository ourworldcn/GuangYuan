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
using System.ComponentModel;
using System.Diagnostics;
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
            Test(_Host);
            _Host.Run();
        }

        /// <summary>
        /// 测试点。
        /// </summary>
        private static void Test(IHost host)
        {
            Random rnd = new Random();
            var sw = Stopwatch.StartNew();
            for (int i = 10000 - 1; i >= 0; i--)
            {
                Math.Sqrt(rnd.NextDouble());
            }
            sw.Stop();
            var ss = sw.Elapsed;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

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
