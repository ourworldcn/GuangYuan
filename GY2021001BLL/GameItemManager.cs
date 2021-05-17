using GY2021001DAL;
using Gy2021001Template;
using Microsoft.Extensions.DependencyInjection;
using OwGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        /// 按照指定模板Id创建一个对象
        /// </summary>
        /// <param name="templateId">创建事物所需模板Id。</param>
        /// <param name="ownerId">指定一个父Id,如果不指定或为null则忽略。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameItem CreateGameItem(Guid templateId, Guid? ownerId = null)
        {
            var template = World.ItemTemplateManager.GetTemplateFromeId(templateId);
            var result = CreateGameItem(template, ownerId);
            return result;
        }

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
            result.Children.AddRange(template.ChildrenTemplateIds.Select(c => CreateGameItem(c)));
            try
            {
                var dirty = Options?.ItemCreated?.Invoke(Service, result) ?? false;
            }
            catch (Exception)
            {
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameItemTemplate GetTemplate(GameObjectBase gameObject)
        {
            return World.ItemTemplateManager.GetTemplateFromeId(gameObject.TemplateId);
        }

        /// <summary>
        /// 获取指定事物的指定名称属性的值。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        public object GetPropertyValue(GameItem gameItem, string propName)
        {
            if (propName.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                return gameItem.Id;
            else if (propName.Equals("tid", StringComparison.InvariantCultureIgnoreCase))
                return gameItem.TemplateId;
            else if (propName.Equals("pid", StringComparison.InvariantCultureIgnoreCase))
                return gameItem.ParentId ?? gameItem.OwnerId ?? null;
            else if (propName.Equals("ptid", StringComparison.InvariantCultureIgnoreCase))
            {
                var container = gameItem.Parent;
                if (null != container)  //若找到容器
                    return container.TemplateId;
                //找到依附物
                //TO DO
                throw new NotImplementedException();
            }
            return gameItem.Properties.GetValueOrDefault(propName);
        }

        public bool SetPropertyValue(GameItem gameItem, string propName, object val)
        {
            //TO DO
            if (propName.Equals("tid", StringComparison.InvariantCultureIgnoreCase))
            {
                gameItem.TemplateId = (Guid)val;
                return true;
            }
            else if (propName.Equals("pid", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            else if (propName.Equals("ptid", StringComparison.InvariantCultureIgnoreCase))
            {
                //    var container = gameItem.Parent;
                //    if (null != container)  //若找到容器
                //        return container.TemplateId;
                //找到依附物
                //TO DO
                throw new NotImplementedException();
            }
            gameItem.Properties[propName] = val;
            return true;
        }

        /// <summary>
        /// 变换物品等级。会对比原等级的属性增减属性数值。如模板中原等级mhp=100,而物品mhp=120，则会用新等级mhp+20。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="newLevel"></param>
        public void ChangeLevel(GameItem gameItem, int newLevel)
        {
            var template = GetTemplate(gameItem);
            var lv = Convert.ToInt32(gameItem.Properties.GetValueOrDefault(ProjectConstant.LevelPropertyName, decimal.Zero));   //当前等级
            foreach (var item in gameItem.Properties.Keys.ToArray())    //遍历属性
            {
                if (template.Properties.GetValueOrDefault(item) is decimal[] seq)   //若是一个序列属性
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

        public void GetItems(GameChar gameChar, Guid containerTId, IEnumerable<Guid> tids, ICollection<GameItem> gameItems)
        {
        }

        /// <summary>
        /// 标准化物品，避免有后增加的槽没有放置上去。
        /// </summary>
        public void Normalize(IEnumerable<GameItem> gameItems)
        {
            var gitm = World.ItemTemplateManager;
            var coll = (from tmp in OwHelper.GetAllSubItemsOfTree(gameItems, c => c.Children)
                        let tt = gitm.GetTemplateFromeId(tmp.TemplateId)
                        select (tmp, tt)).ToArray();
            var gim = World.ItemManager;
            List<Guid> adds = new List<Guid>();
            foreach (var item in coll)
            {
                adds.Clear();
                item.tmp.Children.ApartWithWithRepeated(item.tt.ChildrenTemplateIds, c => c.TemplateId, c => c, null, null, adds);
                foreach (var addItem in adds)
                {
                    var newItem = gim.CreateGameItem(gitm.GetTemplateFromeId(addItem));
                    item.tmp.Children.Add(newItem);
                }
            }
        }

    }

}
