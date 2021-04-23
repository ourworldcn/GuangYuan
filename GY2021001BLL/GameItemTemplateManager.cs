using Gy2021001Template;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY2021001BLL
{
    public class GameItemTemplateManager
    {
        public static List<GameItemTemplate> StoreTemplates = new List<GameItemTemplate>()
        {
            new GameItemTemplate(Guid.Parse("{A06B7496-F631-4D51-9872-A2CC84A56EAB}"))
            {
                DisplayName="当前坐骑头",
            },
            new GameItemTemplate(Guid.Parse("{7D191539-11E1-49CD-8D0C-82E3E5B04D31}"))
            {
                DisplayName="当前坐骑身",

            },
            new GameItemTemplate(Guid.Parse("{6E179D54-5836-4E0B-B30D-756BD07FF196}"))
            {
                DisplayName="坐骑组合",
            },
        };

        private readonly IServiceProvider _ServiceProvider;

        private Task _InitializeTask;

        private ConcurrentDictionary<Guid, GameItemTemplate> _Id2Template;

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

        public GameItemTemplate GetTemplateFromeId(Guid id)
        {
            _InitializeTask.Wait();
            if (!_Id2Template.TryGetValue(id, out GameItemTemplate result))
                return null;
            return result;
        }
    }
}
