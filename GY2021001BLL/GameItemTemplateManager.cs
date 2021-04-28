using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GY2021001BLL
{
    public class GameItemTemplateManagerOptions
    {
        public GameItemTemplateManagerOptions()
        {

        }

        /// <summary>
        /// 当模板加载后调用该委托。
        /// </summary>
        public Func<DbContext, bool> Loaded { get; set; }
    }

    public class GameItemTemplateManager
    {
        private readonly IServiceProvider _ServiceProvider;

        private Task _InitializeTask;

        private ConcurrentDictionary<Guid, GameItemTemplate> _Id2Template;

        private readonly GameItemTemplateManagerOptions _Options;

        /// <summary>
        /// 所有模板的字典。键是模板Id,值是模板对象。
        /// </summary>
        public ConcurrentDictionary<Guid, GameItemTemplate> Id2Template
        {
            get
            {
                _InitializeTask.Wait();
                return _Id2Template;
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameItemTemplateManager()
        {
            Initialize();
        }

        public GameItemTemplateManager(IServiceProvider service)
        {
            _ServiceProvider = service;
            Initialize();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service">所使用的服务容器。</param>
        public GameItemTemplateManager(IServiceProvider service, GameItemTemplateManagerOptions options)
        {
            _ServiceProvider = service;
            _Options = options;
            Initialize();
        }

        private void Initialize()
        {
            _InitializeTask = Task.Factory.StartNew(() =>
            {
                try
                {
                    var db = _ServiceProvider.GetService(typeof(GameTemplateContext)) as GameTemplateContext;
                    //追加数据
                    #region 追加模板数据
                    db.ItemTemplates.Load();
                    bool dbDirty = _Options?.Loaded?.Invoke(db) ?? false;
                    if (dbDirty)
                        db.SaveChanges();
                    _Id2Template = new ConcurrentDictionary<Guid, GameItemTemplate>(db.ItemTemplates.ToDictionary(c => c.Id));
                    #endregion 追加模板数据

                }
                catch (Exception err)
                {
                    Trace.WriteLine(err.Message);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public object ThisLocker { get; } = new object();
        private VWorld _VWorld;
        public VWorld VWorld
        {
            get
            {
                lock (ThisLocker)
                    return _VWorld ?? (_VWorld = _ServiceProvider.GetService<VWorld>());
            }
        }

        public GameItemTemplate GetTemplateFromeId(Guid id)
        {
            _InitializeTask.Wait();
            if (!_Id2Template.TryGetValue(id, out GameItemTemplate result))
                return null;
            return result;
        }
    }
}
