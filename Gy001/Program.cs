using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.TemplateDb;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Runtime;
using System.IO.Compression;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        [Conditional("DEBUG")]
        private static void Test(IHost host)
        {
        }

        static int _Di = 0;
        static void PostEvictionDelegate(object key, object value, EvictionReason reason, object state)
        {
            Debug.WriteLine($"[{DateTime.Now}]PostEvictionDelegate:{key},reason:{reason},total:{Interlocked.Increment(ref _Di)}");
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

            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = host.Services.GetService<ILogger<Program>>();

            try
            {
                var tContext = services.GetRequiredService<GY001TemplateContext>();
                TemplateMigrateDbInitializer.Initialize(tContext);
                logger.LogInformation($"{DateTime.UtcNow}用户数据库已正常升级。");
                var context = services.GetRequiredService<GY001UserContext>();
                MigrateDbInitializer.Initialize(context);
                logger.LogInformation($"{DateTime.UtcNow}用户数据库已正常升级。");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred creating the DB——{err.Message}");
            }
        }

    }

}
