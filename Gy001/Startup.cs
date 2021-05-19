using GY2021001BLL;
using GY2021001DAL;
using Gy2021001Template;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Gy001
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var userDbConnectionString = Configuration.GetConnectionString("DefaultConnection");
            var templateDbConnectionString = Configuration.GetConnectionString("TemplateDbConnection");

            #region 配置通用服务

            services.AddResponseCompression();
            services.AddDbContext<GY2021001DbContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString), ServiceLifetime.Scoped);
            services.AddDbContext<GameTemplateContext>(options => options.UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString), ServiceLifetime.Singleton);

            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0).AddJsonOptions(UserDbOptions =>
            //{
            //    UserDbOptions.SerializerSettings.ContractResolver = new DefaultContractResolver()
            //    {
            //    };
            //});
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;  //直接用属性名
                options.JsonSerializerOptions.IgnoreReadOnlyProperties = true;  //忽略只读属性。
            });

            services.AddSingleton<ObjectPool<List<GameItem>>>(c => new DefaultObjectPool<List<GameItem>>(new ListGameItemPolicy()));    //频繁使用的列表对象的对象池
            #endregion 配置通用服务

            #region 配置Swagger
            //注册Swagger生成器，定义一个Swagger 文档
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "光元001",
                    Description = "接口文档v1.1.2"
                });
                // 为 Swagger 设置xml文档注释路径
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
            #endregion 配置Swagger

            #region 配置游戏专用服务
            services.AddTransient<HashAlgorithm>(c => SHA256.Create());

            //services.AddTransient(options => new GY2021001DbContext(new DbContextOptionsBuilder<GY2021001DbContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).Options));
            //services.AddSingleton(UserDbOptions => new GameTemplateContext(new DbContextOptionsBuilder<GameTemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).Options));

            services.AddSingleton(c => new VWorld(c, new VWorldOptions()
            {
                UserDbOptions = new DbContextOptionsBuilder<GY2021001DbContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).Options,
                TemplateDbOptions = new DbContextOptionsBuilder<GameTemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).Options,
            }));
            services.AddSingleton(c => new GameItemTemplateManager(c, new GameItemTemplateManagerOptions()
            {
                Loaded = SpecificProject.ItemTemplateLoaded,
            }));
            services.AddSingleton(c => new BlueprintManager(c, new BlueprintManagerOptions()
            {
                DoApply = SpecificProject.ApplyBlueprint,
            }));
            services.AddSingleton(c => new GameItemManager(c, new GameItemManagerOptions()
            {
                ItemCreated = SpecificProject.GameItemCreated,
            }));
            services.AddSingleton(c => new GameCharManager(c, new GameCharManagerOptions()
            {
                CharCreated = SpecificProject.CharCreated,
            }));
            services.AddSingleton(c => new CombatManager(c, new CombatManagerOptions()
            {
                CombatStart = SpecificProject.CombatStart,
                CombatEnd = SpecificProject.CombatEnd,
            }));
            #endregion 配置游戏专用服务
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            #region 启用通用服务
            app.UseResponseCompression();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            #endregion 启用通用服务

            #region 启用中间件服务生成Swagger
            app.UseSwagger();
            //启用中间件服务生成SwaggerUI，指定Swagger JSON终结点
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", env.EnvironmentName + " V1");
                c.RoutePrefix = string.Empty;//设置根节点访问
            });
            #endregion 启用中间件服务生成Swagger

            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
