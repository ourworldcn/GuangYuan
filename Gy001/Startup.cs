using Game.Logging;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.Specific;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using Gy01.AutoMapper.Profiles;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.ObjectPool;
using Microsoft.OpenApi.Models;
using OW.Game;
using OW.Game.PropertyChange;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
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
            #region ����ͨ�÷���
            var dd = typeof(GameCommandManager);    //ȷ�����س���

            services.AddResponseCompression(c => c.EnableForHttps = true);
            //��־����
            services.AddLogging(builder =>
            {
                builder.AddEventSourceLogger();
                builder.AddConfiguration(Configuration.GetSection("Logging"));

                builder.AddConsole(option =>
                {
                    option.IncludeScopes = true;
                    option.TimestampFormat = "hh:mm:ss";
                });
#if DEBUG
                //builder.AddDebug();
#endif //DEBUG
            });

            var userDbConnectionString = Configuration.GetConnectionString("DefaultConnection");
            var templateDbConnectionString = Configuration.GetConnectionString("TemplateDbConnection");
            var loggingDbConnectionString = Configuration.GetConnectionString("LoggingDbConnection");
#if DEBUG

            //LoggerFactory LoggerFactory = new LoggerFactory(new[] { new DebugLoggerProvider() });
            services.AddDbContext<GY001TemplateContext>(options => options.UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString)/*.UseLoggerFactory(LoggerFactory)*/.EnableSensitiveDataLogging(), ServiceLifetime.Singleton);
            services.AddDbContext<GY001UserContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString)/*.UseLoggerFactory(LoggerFactory)*/.EnableSensitiveDataLogging(), ServiceLifetime.Scoped);
#else
            services.AddDbContext<GY001TemplateContext>(options => options.UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString), ServiceLifetime.Singleton);
            services.AddDbContext<GY001UserContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString), ServiceLifetime.Scoped);
#endif //DEBUG
            services.AddDbContext<GameLoggingDbContext>(options => options.UseLazyLoadingProxies().UseSqlServer(loggingDbConnectionString), ServiceLifetime.Scoped);

            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0).AddJsonOptions(UserDbOptions =>
            //{
            //    UserDbOptions.SerializerSettings.ContractResolver = new DefaultContractResolver()
            //    {
            //    };
            //});
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;  //ֱ����������
                options.JsonSerializerOptions.IgnoreReadOnlyProperties = true;  //����ֻ�����ԡ�
            });

            services.AddAutoMapper(Array.Empty<Assembly>());
            services.AutoRegister();

            #endregion ����ͨ�÷���

            #region ����Swagger
            //ע��Swagger������������һ�� Swagger �ĵ�
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = $"��Ԫ001",
                    Description = "�ӿ��ĵ�v1.1.2"
                });
                // Ϊ Swagger ����xml�ĵ�ע��·��
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
            #endregion ����Swagger

            #region ������Ϸר�÷���
            services.AddGameServices();


            services.AddSingleton(c => new VWorld(c, new VWorldOptions()
            {
#if DEBUG
                UserDbOptions = new DbContextOptionsBuilder<GY001UserContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString)/*.UseLoggerFactory(LoggerFactory)*/.EnableSensitiveDataLogging().Options,
                TemplateDbOptions = new DbContextOptionsBuilder<GY001TemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString)/*.UseLoggerFactory(LoggerFactory)*/.Options,
#else
                UserDbOptions = new DbContextOptionsBuilder<GY001UserContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging().Options,
                TemplateDbOptions = new DbContextOptionsBuilder<GY001TemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).Options,
#endif //DEBUG
            }));
            services.Replace(ServiceDescriptor.Singleton<GameEventsManager>(c => new Gy001GameEventsManager(c, new Gy001GameEventsManagerOptions())));

            services.TryAddSingleton(c => new GameMapperManager(c, new GameMapperManagerOptions()) { });//����ת����������
            services.AddAutoMapper(c =>
            {
                c.AddMaps(AppDomain.CurrentDomain.GetAssemblies());
            });
            #endregion ������Ϸר�÷���

            services.AddPublisherT78();  //������ʱ���sdk�ķ���
            services.AddPublisherT89();  //�����������sdk�ķ���

        }

        private Task ExceptionHandler(HttpContext context)
        {
            return Task.Run(() =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                context.Response.WriteAsync(exceptionHandlerPathFeature.Error.Message);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            #region ����ͨ�÷���
            app.UseResponseCompression();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseDeveloperExceptionPage();
                //app.UseExceptionHandler(build => build.Run(ExceptionHandler));
            }

            #endregion ����ͨ�÷���

            #region �����м����������Swagger
            app.UseSwagger();
            //�����м����������SwaggerUI��ָ��Swagger JSON�ս��
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", env.EnvironmentName + $" V1");
                c.RoutePrefix = string.Empty;//���ø��ڵ����
            });
            #endregion �����м����������Swagger

            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

    }

    //public class AadAuthenticationInterceptor : DbConnectionInterceptor
    //{
    //    public override InterceptionResult ConnectionOpening(
    //        DbConnection connection,
    //        ConnectionEventData eventData,
    //        InterceptionResult result)
    //        => throw new InvalidOperationException("Open connections asynchronously when using AAD authentication.");

    //    //public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
    //    //    DbConnection connection,
    //    //    ConnectionEventData eventData,
    //    //    InterceptionResult result,
    //    //    CancellationToken cancellationToken = default)
    //    //{
    //    //    var sqlConnection = (SqlConnection)connection;

    //    //    //var provider = new AzureServiceTokenProvider();
    //    //    // Note: in some situations the access token may not be cached automatically the Azure Token Provider.
    //    //    // Depending on the kind of token requested, you may need to implement your own caching here.
    //    //    //sqlConnection.AccessToken = await provider.GetAccessTokenAsync("https://database.windows.net/", null, cancellationToken);

    //    //    return result;
    //    //}
    //    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    //    {
    //        base.ConnectionOpened(connection, eventData);
    //    }
    //}
}
