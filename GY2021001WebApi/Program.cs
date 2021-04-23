using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GY2021001BLL;
using GY2021001DAL;
using Gy2021001Template;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GY2021001WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //CreateWebHostBuilder(args).Build().Run();
            var host = CreateWebHostBuilder(args).Build();
            CreateDb(host);
            LoadCache(host);
            host.Run();
        }

        /// <summary>
        /// 加载缓存。
        /// </summary>
        /// <param name="host"></param>
        private static void LoadCache(IWebHost host)
        {
            var gitm = host.Services.GetService<GameItemTemplateManager>();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

        /// <summary>
        /// 创建数据库。
        /// </summary>
        /// <param name="host"></param>
        private static void CreateDb(IWebHost host)
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
