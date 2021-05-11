using GY2021001DAL;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY2021001BLL
{
    /// <summary>
    /// 使用蓝图的数据。
    /// </summary>
    public class ApplyBluprintDatas
    {
        public ApplyBluprintDatas()
        {

        }

        /// <summary>
        /// 蓝图的模板Id。
        /// </summary>
        public Guid BlueprintId { get; set; }

        /// <summary>
        /// 角色对象。
        /// </summary>
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要执行的目标对象Id集合。目前仅有唯一元素，神纹的对象Id。
        /// </summary>
        public List<Guid> ObjectIds { get; set; }

        /// <summary>
        /// 指定强化的属性，如攻击则这里给出atk,质量是qlt,最大血量是mhp
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// 要执行的次数。
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 应用蓝图后，物品变化数据。
        /// </summary>
        public List<ChangesItem> ChangesItem { get; } = new List<ChangesItem>();


        /// <summary>
        /// 是否有错误。
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage { get; set; }
    }

    /// <summary>
    /// 蓝图管理器配置数据。
    /// </summary>
    public class BlueprintManagerOptions
    {
        public BlueprintManagerOptions()
        {

        }
    }

    /// <summary>
    /// 蓝图管理器。
    /// </summary>
    public class BlueprintManager : GameManagerBase<BlueprintManagerOptions>
    {
        public BlueprintManager()
        {
            Initialize();
        }

        public BlueprintManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public BlueprintManager(IServiceProvider service, BlueprintManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        private void Initialize()
        {
            lock (ThisLocker)
                _InitializeTask ??= Task.Run(() =>
                {
                    _Id2BlueprintTemplate = Context.Set<BlueprintTemplate>().ToDictionary(c => c.Id);
                });
        }

        Task _InitializeTask;

        private DbContext _DbContext;

        public DbContext Context { get => _DbContext ??= World.CreateNewTemplateDbContext(); }

        Dictionary<Guid, BlueprintTemplate> _Id2BlueprintTemplate;

        public Dictionary<Guid, BlueprintTemplate> Id2BlueprintTemplate
        {
            get
            {
                _InitializeTask.Wait();
                return _Id2BlueprintTemplate;
            }
        }

        public void ApplyBluprint(ApplyBluprintDatas datas)
        {
            _InitializeTask.Wait();
            if (!World.CharManager.Lock(datas.GameChar.GameUser))    //若无法锁定用户
            {
                datas.HasError = true;
                datas.DebugMessage = $"指定用户无效。";
                return;
            }
            try
            {
                if (datas.ObjectIds.Count != 1)
                {
                    datas.HasError = true;
                    datas.DebugMessage = $"目标对象过多";
                    return;
                }
                var objectId = datas.ObjectIds[0];
                var slot = datas.GameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShenWenSlotId);  //神纹装备槽
                var obj = slot.Children.FirstOrDefault(c => c.Id == objectId);    //目标物品对象,目前是神纹
                if (null == obj)    //找不到指定的目标物品
                {
                    datas.HasError = true;
                    datas.DebugMessage = $"找不到指定的目标物品。";
                    return;
                }

                if (datas.BlueprintId == ProjectConstant.ShenwenLvUpBlueprint)  //若要进行神纹升级
                {
                    var info = GetShenwenInfo(datas.GameChar, obj);
                    if (info.Level + datas.Count > info.MaxLv)
                    {
                        datas.HasError = true;
                        datas.DebugMessage = $"已达最大等级或升级次数过多。";
                        return;
                    }
                    var daojuSlot = datas.GameChar.GameItems.First(c => c.TemplateId == ProjectConstant.DaojuBagSlotId);
                    //var suipian = from tmp in daojuSlot.Children
                    //              let gid=
                    
                    var lv = Convert.ToInt32(obj.Properties[ProjectConstant.LevelPropertyName]);

                }
                else if (datas.BlueprintId == ProjectConstant.ShenWenTupoBlueprint) //若要进行神纹突破
                {
                }
                else
                {
                    datas.HasError = true;
                    datas.DebugMessage = $"找不到指定蓝图的Id:{datas.BlueprintId}";
                }
            }
            finally
            {
                World.CharManager.Unlock(datas.GameChar.GameUser);
            }
        }

        public (int Level, int MaxLv, int TupoCount) GetShenwenInfo(GameChar gameChar, GameItem shenwen)
        {
            var tt = World.ItemTemplateManager.GetTemplateFromeId(shenwen.TemplateId);  //模板
            int lv = 0; //等级
            if (!shenwen.Properties.TryGetValue(ProjectConstant.LevelPropertyName, out object lvObj) && lvObj is int lvVal)
                lv = lvVal;
            int tp = 0; //突破次数
            //if (!shenwen.Properties.TryGetValue(ProjectConstant.ShenwenTupoCountPropertyName, out object tpObj) && lvObj is int tpVal)
            //    tp = tpVal;
            //else
            //    shenwen.Properties[ProjectConstant.ShenwenTupoCountPropertyName] = (decimal)tp; //加入属性
            int maxLv;  //当前突破次数下最大等级。
            BlueprintTemplate tbp = Id2BlueprintTemplate[ProjectConstant.ShenWenTupoBlueprint];
            var seq = (decimal[])tbp.Properties["ssl"];
            maxLv = (int)seq[lv];
            return (lv, maxLv, tp);
        }
    }
}
