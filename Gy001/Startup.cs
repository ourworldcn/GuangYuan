using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.TemplateDb;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.OpenApi.Models;
using OW.Game.Expression;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using OW.Game;

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

            services.AddHostedService<GameHostedService>();
            #region ����ͨ�÷���

            services.AddResponseCompression();
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
                builder.AddDebug();
#endif //DEBUG
            });

            services.AddDbContext<GY001UserContext>(options => options.UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Scoped);
            services.AddDbContext<GY001TemplateContext>(options => options.UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Singleton);

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

            services.AddSingleton<ObjectPool<List<GameItem>>>(c => new DefaultObjectPool<List<GameItem>>(new ListGameItemPolicy(), Environment.ProcessorCount * 8));    //Ƶ��ʹ�õ��б����Ķ����
            #endregion ����ͨ�÷���

            #region ����Swagger
            //ע��Swagger������������һ��Swagger �ĵ�
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
            services.AddTransient<HashAlgorithm>(c => SHA512.Create());
            //services.AddTransient(options => new GY001UserContext(new DbContextOptionsBuilder<GY001UserContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).Options));
            //services.AddSingleton(UserDbOptions => new GY001TemplateContext(new DbContextOptionsBuilder<GY001TemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).Options));

            services.AddSingleton(c => new VWorld(c, new VWorldOptions()
            {
                UserDbOptions = new DbContextOptionsBuilder<GY001UserContext>().UseLazyLoadingProxies().UseSqlServer(userDbConnectionString).EnableSensitiveDataLogging().Options,
                TemplateDbOptions = new DbContextOptionsBuilder<GY001TemplateContext>().UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).Options,
            }));
            services.AddSingleton(c => new GameItemTemplateManager(c, new GameItemTemplateManagerOptions()
            {
                Loaded = SpecificProject.ItemTemplateLoaded,
            }));
            services.AddSingleton(c => new GameItemManager(c, new GameItemManagerOptions()
            {
                ItemCreated = SpecificProject.GameItemCreated,
            }));
            services.AddSingleton(c => new GameCharManager(c, new GameCharManagerOptions()
            {
            }));
            services.AddSingleton(c => new CombatManager(c, new CombatManagerOptions()
            {
                CombatStart = SpecificProject.CombatStart,
                CombatEnd = SpecificProject.CombatEnd,
            }));
            services.AddSingleton<GamePropertyHelper, GameManagerPropertyHelper>();
            services.AddSingleton(c => new BlueprintManager(c, new BlueprintManagerOptions()
            {
                DoApply = SpecificProject.ApplyBlueprint,
            }));
            services.AddSingleton<IGameThingHelper>(c => c.GetService<GameItemManager>());

            services.AddSingleton(c => new GameSocialManager(c, new SocialManagerOptions()));

            services.AddSingleton<IGameObjectInitializer>(c => new Gy001Initializer(c, new Gy001InitializerOptions()));

            #endregion ������Ϸר�÷���
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
            #endregion ����ͨ�÷���

            #region �����м����������Swagger
            app.UseSwagger();
            //�����м����������SwaggerUI��ָ��Swagger JSON�ս��
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", env.EnvironmentName + $" V1({env.EnvironmentName})");
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
