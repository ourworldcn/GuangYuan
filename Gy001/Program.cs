using GuangYuan.GY001.BLL;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OW.Game;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Gy001
{
    public class Program
    {
        private static IHost _Host;

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
        /// ���Ե㡣
        /// </summary>
        [Conditional("DEBUG")]
        private static void Test(IHost host)
        {
            var world = host.Services.GetRequiredService<VWorld>();
            using var db = world.CreateNewUserDbContext();
            var dic = world.PropertyManager.Filter(new string[] { "count", "mtid23087402", "mcount123", "mbtidds32d", "mhtidde32", "stceqw", "fht" });
            var conv = TypeDescriptor.GetConverter(typeof(string));
            string s = "3.5";
            int? i = 2;
            float? f = null;
            object obj1 = DateTime.UtcNow;
            object obj2 = i;
            object obj3 = f;
            var d1 =Convert.ToString(obj1);
            d1 = Convert.ToString(obj2);
            d1 = Convert.ToString(obj3);
            //db.SaveChanges();
        }

        public void Test2()
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();

            //CPU
            //Intel(R) Core(TM) i5-10500 CPU @ 3.10GHz
            //��׼�ٶ�:	3.10 GHz
            //���:	1
            //�ں�:	6
            //�߼�������:	12
            //���⻯:	������
            //L1 ����:	384 KB
            //L2 ����:	1.5 MB
            //L3 ����:	12.0 MB


        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        /// <summary>
        /// �������ݿ⡣
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
                logger.LogInformation($"{DateTime.UtcNow}�û����ݿ�������������");
                var context = services.GetRequiredService<GY001UserContext>();
                MigrateDbInitializer.Initialize(context);
                logger.LogInformation($"{DateTime.UtcNow}�û����ݿ�������������");
                var model = context.Model;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred creating the DB����{err.Message}");
            }
        }

        private void CreateDbTest(DbContext dbContext)
        {
            dbContext.Database.EnsureCreated();
            IModel lastModel = null;
            var lastMigration = dbContext.Set<MigrationLog>()
                    .OrderByDescending(e => e.Id)
                    .FirstOrDefault();
            lastModel = lastMigration == null ? null : (CreateModelSnapshot(lastMigration.SnapshotDefine).Result?.Model);

            var modelDiffer = dbContext.GetInfrastructure().GetService<IMigrationsModelDiffer>();
            var isDiff = modelDiffer.HasDifferences(lastModel, dbContext.Model); //�����������ֵ��true����false��������ԱȽ��ϰ汾��model�͵�ǰ�汾��model�Ƿ���ָ��ġ�

            var upOperations = modelDiffer.GetDifferences(lastModel, dbContext.Model);  //����������ص�Ǩ�ƵĲ�������

            dbContext.GetInfrastructure().GetRequiredService<IMigrationsSqlGenerator>().Generate(upOperations, dbContext.Model).ToList();   //��������Ǹ���Ǩ�ƶ���͵�ǰ��model����Ǩ��sql�ű���

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
            // ���ɿ��գ���Ҫ�浽���ݿ��й����°汾��
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

    }

}
