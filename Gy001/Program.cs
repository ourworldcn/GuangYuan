using GY2021001BLL;
using GY2021001DAL;
using Gy2021001Template;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gy001
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            CreateDb(host);
            LoadCache(host);

            host.Run();
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
