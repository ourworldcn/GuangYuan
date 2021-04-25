using Gy2021001Template;
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
    /// <summary>
    /// 该项目使用的特定常量。
    /// </summary>
    public static class ProjectConstant
    {
        #region MyRegion
        /// <summary>
        /// 当前装备的坐骑头容器模板Id。
        /// </summary>
        public const string ZuojiTou = "{A06B7496-F631-4D51-9872-A2CC84A56EAB}";

        /// <summary>
        /// 当前装备的坐骑身体容器模板Id。
        /// </summary>
        public const string ZuojiShen = "{7D191539-11E1-49CD-8D0C-82E3E5B04D31}";

        /// <summary>
        /// 未装备的坐骑头和身体需要一个容器组合起来。此类容器的模板Id就是这个。
        /// </summary>
        public const string ZuojiZuheRongqi = "{6E179D54-5836-4E0B-B30D-756BD07FF196}";

        #endregion

        public const string LevelPropertyName = "lv";
    }

    public class GameItemTemplateManager
    {
        public static List<GameItemTemplate> StoreTemplates = new List<GameItemTemplate>()
        {
            new GameItemTemplate(new Guid(ProjectConstant.ZuojiTou))
            {
                DisplayName="当前坐骑头",
            },
            new GameItemTemplate(new Guid(ProjectConstant.ZuojiShen))
            {
                DisplayName="当前坐骑身",

            },
            new GameItemTemplate(new Guid(ProjectConstant.ZuojiZuheRongqi))
            {
                DisplayName="坐骑组合",
            },
        };

        private readonly IServiceProvider _ServiceProvider;

        private Task _InitializeTask;

        private ConcurrentDictionary<Guid, GameItemTemplate> _Id2Template;

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

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service">所使用的服务容器。</param>
        public GameItemTemplateManager(IServiceProvider service)
        {
            _ServiceProvider = service;
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
                    _Id2Template = new ConcurrentDictionary<Guid, GameItemTemplate>(db.ItemTemplates.ToDictionary(c => c.Id));

                    bool dbDirty = false;
                    foreach (var item in StoreTemplates)
                    {
                        if (!_Id2Template.ContainsKey(item.Id))    //若需要添加坐骑头槽
                        {
                            _Id2Template[item.Id] = item;
                            db.ItemTemplates.Add(item);
                            dbDirty = true;
                        }
                    }
                    if (dbDirty)
                        db.SaveChanges();
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
