using GY2021001DAL;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GY2021001BLL
{
    /// <summary>
    /// 该项目使用的特定常量。
    /// </summary>
    public static class ProjectConstant
    {
        #region 固定模板Id
        #region 废弃模板Id

        /// <summary>
        /// 当前装备的坐骑头容器模板Id。已废弃。
        /// </summary>
        public static readonly Guid ZuojiTou = new Guid("{A06B7496-F631-4D51-9872-A2CC84A56EAB}");

        /// <summary>
        /// 当前装备的坐骑身体容器模板Id。已废弃
        /// </summary>
        public static readonly Guid ZuojiShen = new Guid("{7D191539-11E1-49CD-8D0C-82E3E5B04D31}");
        #endregion 废弃模板Id

        #region 坐骑相关Id

        /// <summary>
        /// 坐骑头和身体需要一个容器组合起来。此类容器的模板Id就是这个。
        /// </summary>
        public static readonly Guid ZuojiZuheRongqi = new Guid("{6E179D54-5836-4E0B-B30D-756BD07FF196}");

        /// <summary>
        /// 坐骑组合中的头容器。
        /// </summary>
        public static readonly Guid ZuojiZuheTou = new Guid("{740FEBF3-7472-43CB-8A10-798F6C61335B}");

        /// <summary>
        /// 坐骑组合中的身体容器。
        /// </summary>
        public static readonly Guid ZuojiZuheShenti = new Guid("{F8B1987D-FDF3-4090-9E9B-EBAF1DB2DCCD}");
        #endregion 坐骑相关Id

        /// <summary>
        /// 当前坐骑的容器Id。
        /// </summary>
        public static readonly Guid DangqianZuoqiCao = new Guid("{B19EE5AB-57E3-4513-8228-9F2A8364358E}");

        /// <summary>
        /// 角色模板Id。当前只有一个模板。
        /// </summary>
        public static readonly Guid CharTemplateId = new Guid("{0CF39269-6301-470B-8527-07AF29C5EEEC}");

        /// <summary>
        /// 神纹槽Id。
        /// </summary>
        public static readonly Guid ShenWenSlotId = new Guid("{88A4EED6-0AEB-4A70-8FDE-67F75E5E2C0A}");

        #endregion 固定模板Id

        public const string LevelPropertyName = "lv";
    }

    /// <summary>
    /// 封装项目特定逻辑。
    /// </summary>
    public class SpecificProject
    {
        private readonly IServiceProvider _ServiceProvider;

        public SpecificProject()
        {
        }

        public SpecificProject(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// 所有需要额外加入的模板对象。
        /// </summary>
        public static List<GameItemTemplate> StoreTemplates = new List<GameItemTemplate>()
        {
#region 已废弃

            new GameItemTemplate(ProjectConstant.ZuojiTou)
            {
                DisplayName="当前坐骑头",
            },
            new GameItemTemplate(ProjectConstant.ZuojiShen)
            {
                DisplayName="当前坐骑身",

            },
	#endregion 已废弃
            new GameItemTemplate(ProjectConstant.ZuojiZuheRongqi)
            {
                DisplayName="坐骑组合",
                ChildrenTemplateIdString=$"{ProjectConstant.ZuojiZuheTou},{ProjectConstant.ZuojiZuheShenti}",
            },
            new GameItemTemplate(ProjectConstant.ZuojiZuheTou)
            {
                DisplayName="坐骑组合的头",
            },
            new GameItemTemplate(ProjectConstant.ZuojiZuheShenti)
            {
                DisplayName="坐骑组合的身体",
            },
            new GameItemTemplate(ProjectConstant.DangqianZuoqiCao)
            {
                DisplayName="当前坐骑槽"
            },
            new GameItemTemplate(ProjectConstant.CharTemplateId)
            {
                DisplayName="角色的模板",
                ChildrenTemplateIdString=$"{ProjectConstant.DangqianZuoqiCao},{ProjectConstant.ShenWenSlotId}",
            },
        };

        /// <summary>
        /// 所有模板调赴后调用。此处追加模板。
        /// </summary>
        /// <param name="itemTemplates"></param>
        /// <returns></returns>
        public static bool ItemTemplateLoaded(DbContext itemTemplates)
        {
            Comparer<GameItemTemplate> comparer = Comparer<GameItemTemplate>.Create((l, r) =>
            {
                if (l == r)
                    return 0;
                int result = Comparer<Guid>.Default.Compare(l.Id, r.Id);
                if (0 != result)
                    return result;
                return result;
            });
            bool dbDirty = false;
            var dbSet = itemTemplates.Set<GameItemTemplate>();
            foreach (var item in StoreTemplates)
            {
                var template = dbSet.Local.FirstOrDefault(c => c.Id == item.Id);
                if (null == template)
                {
                    dbSet.Add(item);
                    dbDirty = true;
                }
                else
                {
                    //TO DO 应判断是否脏
                    template.DisplayName = item.DisplayName;
                    template.ChildrenTemplateIdString = item.ChildrenTemplateIdString;
                    dbDirty = true;
                }
            }
            return dbDirty;
        }

        /// <summary>
        /// 角色创建后被调用。
        /// </summary>
        /// <param name="service">服务提供者。</param>
        /// <param name="gameChar">已经创建的对象。</param>
        /// <returns></returns>
        public static bool CharCreated(IServiceProvider service, GameChar gameChar)
        {
            var gitm = service.GetService<GameItemTemplateManager>();
            var result = false;

            var mountsSlot = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.DangqianZuoqiCao);   //当前坐骑槽

            var ts = gitm.Id2Template.Values.Where(c => c.Properties.ContainsKey("nm") && "羊" == c.Properties["nm"] as string).ToList();   //获取羊
            var headTemplate = ts.FirstOrDefault(c => c.IsHead());
            var bodyTemplate = ts.FirstOrDefault(c => c.IsBody());
            var mounts = CreateMounts(service, headTemplate, bodyTemplate);
            mountsSlot.Children.Add(mounts);
            result = true;
            return result;
        }

        /// <summary>
        /// 创建一个坐骑。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="head">头的模板，若是null，则不创建。</param>
        /// <param name="body">身体的模板，若是null，则不创建。</param>
        /// <returns></returns>
        public static GameItem CreateMounts(IServiceProvider service, GameItemTemplate head, GameItemTemplate body)
        {
            var gim = service.GetService<GameItemManager>();
            var gitm = service.GetService<GameItemTemplateManager>();
            var result = gim.CreateGameItem(gitm.GetTemplateFromeId(ProjectConstant.ZuojiZuheRongqi));
            if (null != head)
            {
                var hGi = gim.CreateGameItem(head);
                result.Children.First(c => c.TemplateId == ProjectConstant.ZuojiZuheTou).Children.Add(hGi);
            }
            if (null != body)
            {
                var bGi = gim.CreateGameItem(body);
                result.Children.First(c => c.TemplateId == ProjectConstant.ZuojiZuheShenti).Children.Add(bGi);
            }
            return result;
        }

        /// <summary>
        /// 当一个虚拟事物创建后调用。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="gameItem"></param>
        /// <returns></returns>
        public static bool GameItemCreated(IServiceProvider service, GameItem gameItem)
        {
            var result = false;
            return result;
        }
    }
}
