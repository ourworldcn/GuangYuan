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

    /// <summary>
    /// 虚拟物品管理器。
    /// </summary>
    public class GameItemManager : GameManagerBase<GameItemManagerOptions>
    {
        #region 构造函数

        public GameItemManager() : base()
        {

        }

        public GameItemManager(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
        public GameItemManager(IServiceProvider serviceProvider, GameItemManagerOptions options) : base(serviceProvider, options)
        {

        }
        #endregion 构造函数

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
                Count = 1,
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
                decimal[] seq = item.Value as decimal[];
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
            var gitm = World.ItemTemplateManager;
            result.Children.AddRange(template.ChildrenTemplateIds.Select(c => CreateGameItem(gitm.GetTemplateFromeId(c))));
            try
            {
                var dirty = Options?.ItemCreated?.Invoke(Service, result) ?? false;
            }
            catch (Exception)
            {
            }
            return result;
        }

        /// <summary>
        /// 变换物品等级。会对比原等级的属性增减属性数值。如模板中原等级mhp=100,而物品mhp=120，则会用新等级mhp+20。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="newLevel"></param>
        public void ChangeLevel(GameItem gameItem, int newLevel)
        {
            var template = World.ItemTemplateManager.GetTemplateFromeId(gameItem.TemplateId);
            var lv = Convert.ToInt32(gameItem.Properties.GetValueOrDefault("lv", decimal.Zero));
            foreach (var item in gameItem.Properties.Keys.ToArray())    //遍历属性
            {
                var seq = template.Properties.GetValueOrDefault(item) as decimal[];
                if (null != seq)   //若是一个序列属性
                {
                    var oov = seq[lv];  //原级别模板值
                    var val = (decimal)gameItem.Properties.GetValueOrDefault(item, oov);  //物品的属性值
                    gameItem.Properties[item] = seq[newLevel] + val - oov;
                }
            }
        }

        /// <summary>
        /// 将一组物品加入一个容器下。
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="gameItems"></param>
        public void AddItem(GameItem dest, IEnumerable<GameItem> gameItems)
        {
            int cap = (int)dest.Properties.GetValueOrDefault(ProjectConstant.ContainerCapacity, -1m);   //容量限制，-1表示不限制
            var gitm = World.ItemTemplateManager;
            foreach (var item in gameItems) //逐一放入
            {
                var template = gitm.GetTemplateFromeId(item.TemplateId);
                if (template.Properties.TryGetValue(ProjectConstant.StackUpperLimit, out object obj)) //若存在堆叠限制
                {
                    decimal stc = (decimal)obj; //堆叠上限
                    var exists = (from tmp in dest.Children
                                  where tmp.TemplateId == item.TemplateId
                                  let sc = stc - tmp.Count.GetValueOrDefault(1) //剩余堆叠数量
                                  where sc > 0  //可以加入物品
                                  select (tmp, sc)).ToArray();
                    if (exists.Length > 0)   //若存在同类可堆叠物品
                    {
                        foreach (var tmp in exists) //填满可堆叠物品
                        {
                            if (item.Count.GetValueOrDefault(1) > tmp.sc) //若需加入数量大于可以堆叠数量
                            {
                                item.Count -= tmp.sc;
                                tmp.tmp.Count += tmp.sc;
                            }
                            else //若全能堆入此物品
                            {
                                tmp.tmp.Count += item.Count;
                                item.Count = 0;
                                break;
                            }
                        }
                    }
                    if (item.Count > 0)    //若还有物品没有放入
                    {
                        dest.Children.Add(item);
                    }
                }
                else //若无堆叠限制
                {
                    dest.Children.Add(item);
                }
            }
        }

        public void test()
        {
            
        }
    }

}
