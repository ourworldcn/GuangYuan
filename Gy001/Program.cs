using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.TemplateDb;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

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
            //var JsonOptions = new JsonSerializerOptions()
            //{
            //    PropertyNamingPolicy = null,
            //    DictionaryKeyPolicy = null,
            //};
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
                var logger = host.Services.GetService<ILogger<Program>>();

                try
                {
                    var tContext = services.GetRequiredService<GY001TemplateContext>();
                    TemplateMigrateDbInitializer.Initialize(tContext);
                    logger.LogInformation($"{DateTime.UtcNow}用户数据库已正常升级。ConnectionString={tContext.Database.GetDbConnection().ConnectionString}");
                    var context = services.GetRequiredService<GY001UserContext>();
                    MigrateDbInitializer.Initialize(context);
                    logger.LogInformation($"{DateTime.UtcNow}用户数据库已正常升级。ConnectionString={context.Database.GetDbConnection().ConnectionString}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred creating the DB——{err.Message}");
                }
            }
        }

    }

}
