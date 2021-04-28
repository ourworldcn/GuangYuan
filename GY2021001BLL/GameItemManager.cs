using GY2021001DAL;
using Gy2021001Template;
using Microsoft.Extensions.DependencyInjection;
using OwGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GY2021001BLL
{
    /// <summary>
    /// 虚拟物品管理器。
    /// </summary>
    public class GameItemManager
    {
        private readonly IServiceProvider _ServiceProvider;
        private readonly GameItemManagerOptions _Options;
        #region 构造函数

        public GameItemManager()
        {

        }

        public GameItemManager(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
        }
        public GameItemManager(IServiceProvider serviceProvider, GameItemManagerOptions options)
        {
            _ServiceProvider = serviceProvider;
            _Options = options;

        }
        #endregion 构造函数

        GameItemTemplateManager _ItemTemplateManager;

        /// <summary>
        /// 物品模板管理器。
        /// </summary>
        public GameItemTemplateManager ItemTemplateManager { get => _ItemTemplateManager ?? (_ItemTemplateManager = _ServiceProvider.GetService<GameItemTemplateManager>()); }

        /// <summary>
        /// 按照指定模板创建一个对象。
        /// </summary>
        /// <param name="template">创建事物所需模板。</param>
        /// <param name="ownerId">指定一个父Id,如果不指定或为null则忽略。</param>
        /// <returns></returns>
        public GameItem CreateGameItem(GameItemTemplate template, Guid? ownerId = null)
        {
            var result = new GameItem()
            {
                TemplateId = template.Id,
                OwnerId = ownerId,
            };
            //初始化级别
            decimal lv;
            if (!template.Properties.TryGetValue(ProjectConstant.LevelPropertyName, out object lvObj))  //若模板没有指定级别属性
            {
                lv = 0;
                result.Properties[ProjectConstant.LevelPropertyName] = lv;
            }
            else
                lv = (decimal)lvObj;
            //初始化属性
            foreach (var item in template.Properties)
            {
                var seq = item.Value as decimal[];
                if (null != seq)   //若是属性序列
                {
                    result.Properties[item.Key] = seq[(int)lv];
                }
                else
                    result.Properties[item.Key] = item.Value;
            }
            if (result.Properties.Count > 0)    //若需要改写属性字符串。
                result.PropertiesString = OwHelper.ToPropertiesString(result.Properties);   //改写属性字符串
            //递归初始化容器
            var gitm = ItemTemplateManager;
            result.Children.AddRange(template.ChildrenTemplateIds.Select(c => CreateGameItem(gitm.GetTemplateFromeId(c))));
            try
            {
                var dirty = _Options?.ItemCreated?.Invoke(_ServiceProvider, result) ?? false;
            }
            catch (Exception)
            {
            }
            return result;
        }

    }

    public class GameItemManagerOptions
    {
        public GameItemManagerOptions()
        {

        }

        /// <summary>
        /// 创建一个物品后调用此回调。
        /// </summary>
        public Func<IServiceProvider, GameItem, bool> ItemCreated { get; set; }
    }
}
