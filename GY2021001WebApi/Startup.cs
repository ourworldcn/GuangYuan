using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;

namespace GY2021001WebApi
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

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2).AddJsonOptions(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver()
                {
                };
            });

            services.AddDbContext<GY2021001DbContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString));
            services.AddDbContext<GameTemplateContext>(options => options.UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString));


            #endregion 配置通用服务

            #region 配置Swagger
            //注册Swagger生成器，定义一个Swagger 文档
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
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
            services.AddTransient(options => new GY2021001DbContext(new DbContextOptionsBuilder<GY2021001DbContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).Options));
            services.AddSingleton(options => new GameTemplateContext(new DbContextOptionsBuilder<GameTemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).Options));

            services.AddSingleton<GameItemTemplateManager>();
            services.AddSingleton<VWorld>();
            services.AddSingleton<GameCharManager>();
            #endregion 配置游戏专用服务

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            #region 启用通用服务

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseMvc();
            #endregion 启用通用服务

            #region 启用中间件服务生成Swagger
            app.UseSwagger();
            //启用中间件服务生成SwaggerUI，指定Swagger JSON终结点
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Web App V1");
                c.RoutePrefix = string.Empty;//设置根节点访问
            });
            #endregion 启用中间件服务生成Swagger

            #region 初始化必要信息

            #endregion 初始化必要信息
        }
    }
}
