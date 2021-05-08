﻿using Gy2021001Template;
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

    public class GameItemTemplateManager : GameManagerBase<GameItemTemplateManagerOptions>
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameItemTemplateManager()
        {
            Initialize();
        }

        public GameItemTemplateManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service">所使用的服务容器。</param>
        public GameItemTemplateManager(IServiceProvider service, GameItemTemplateManagerOptions options) : base(service, options)
        {
            Initialize();
        }
        #endregion 构造函数

        #region 属性及相关

        private GameTemplateContext _TemplateContext;

        /// <summary>
        /// 使用该上下文加载所有模板对象，以保证其单例性。
        /// </summary>
        protected GameTemplateContext TemplateContext
        {
            get
            {
                lock (ThisLocker)
                    return _TemplateContext ??= World.CreateNewTemplateDbContext();
            }
        }

        private ConcurrentDictionary<Guid, GameItemTemplate> _Id2Template;

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

        #endregion 属性及相关

        private Task _InitializeTask;
        private void Initialize()
        {
            _InitializeTask = Task.Factory.StartNew(() =>
            {
                try
                {
                    var db = TemplateContext;
                    //追加数据
                    #region 追加模板数据
                    db.ItemTemplates.Load();
                    bool dbDirty = Options?.Loaded?.Invoke(db) ?? false;
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

        public GameItemTemplate GetTemplateFromeId(Guid id)
        {
            _InitializeTask.Wait();
            if (!_Id2Template.TryGetValue(id, out GameItemTemplate result))
                return null;
            return result;
        }
    }
}
