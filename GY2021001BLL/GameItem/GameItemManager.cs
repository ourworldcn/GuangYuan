using Game.Social;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using OW.Extensions.Game.Store;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OW.Game.Item
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

        #region 属性及相关
        private GameItemTemplateManager _ItemTemplateManager;

        private GameItemTemplateManager ItemTemplateManager { get => _ItemTemplateManager ??= World.ItemTemplateManager; }
        #endregion

        /// <summary>
        /// 刷新指定角色的木材堆叠上限属性。
        /// </summary>
        /// <param name="gameChar"></param>
        public void ComputeMucaiStc(GameChar gameChar)
        {
            var mucai = gameChar.GetMucai();
            var stc = mucai.GetTemplate().Properties.GetDecimalOrDefault("stc");
            var coll = gameChar.GetHomeland().GetAllChildren().Where(c => c.TemplateId == ProjectConstant.MucaiStoreTId);
            stc += coll.Sum(c => c.Properties.GetDecimalOrDefault("stc"));
            mucai.Properties["stc"] = stc;
        }

        /// <summary>
        /// 获取对象的模板。
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns>如果无效的模板Id，则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameItemTemplate GetTemplate(GameItemBase gameObject) =>
            gameObject.GetTemplate() as GameItemTemplate ?? ItemTemplateManager.GetTemplateFromeId(gameObject.TemplateId);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="tId"></param>
        /// <returns></returns>
        public GameItemTemplate GetTemplateFromeId(Guid tId)
        {
            return ItemTemplateManager.GetTemplateFromeId(tId);
        }

        #region 动态属性相关
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
                if (!gameItem.OwnerId.HasValue)  //若也没有附属Id
                    return null;
                return World.CharManager.GetCharFromId(gameItem.OwnerId.Value)?.TemplateId;
            }
            else if (propName.Equals("Count", StringComparison.InvariantCultureIgnoreCase))
                return gameItem.Count ?? 1;
            else if (propName.Equals("tgenuscode", StringComparison.InvariantCultureIgnoreCase) || propName.Equals("tgcode", StringComparison.InvariantCultureIgnoreCase))
            {
                return ItemTemplateManager.GetTemplateFromeId(gameItem.TemplateId)?.GenusCode;
            }
            else if (propName.Equals("freecap", StringComparison.InvariantCultureIgnoreCase)) //容器剩余空间
            {
                var cap = GetCapacity(gameItem);
                if (cap is null)
                    return 0;
                else if (cap == -1)
                    return int.MaxValue;
                else
                    return cap.Value - gameItem.Children.Count;
            }
            else if ((propName.Equals("gid", StringComparison.InvariantCultureIgnoreCase)))
            {
                return GetTemplate(gameItem).GId ?? 0;
            }
            else
                return gameItem.Properties.GetValueOrDefault(propName, 0m);
        }

        /// <summary>
        /// 设置动态属性。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="propName"></param>
        /// <param name="val"></param>
        /// <param name="gameChar"></param>
        /// <returns>true成功设置,false未能成功设置属性。</returns>
        public bool SetPropertyValue(GameItem gameItem, string propName, object val, GameChar gameChar = null)
        {
            gameItem.Properties.TryGetValue(propName, out var oldValue);
            //TO DO
            if (propName.Equals("tid", StringComparison.InvariantCultureIgnoreCase))
            {
                gameItem.TemplateId = (Guid)val;
            }
            else if (propName.Equals("pid", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!OwConvert.TryToGuid(val, out var pid))
                    return false;
                gameItem.ParentId = pid;
            }
            else if (propName.Equals("ptid", StringComparison.InvariantCultureIgnoreCase))  //移动到新的父容器
            {
                var coll = OwHelper.GetAllSubItemsOfTree(new GameItem[] { gameItem }, c => c.Children);
                var tid = (Guid)val;
                var parent = coll.FirstOrDefault(c => c.TemplateId == tid); //获取目标容器
                var oldParent = GetContainer(gameItem); //现有容器
                if (null == oldParent) //若不是属于其他物品
                {
                    AddItem(gameItem, parent);
                }
                else
                {
                    MoveItems(oldParent, c => c.Id == gameItem.Id, parent);
                }
            }
            else if (propName.Equals("Count", StringComparison.InvariantCultureIgnoreCase))
            {
                var count = Convert.ToDecimal(val);
                if (count != 0)
                {
                    gameItem.Count = count;
                }
                else
                {
                    if (null != gameItem.Parent)
                        gameItem.Parent.Children.Remove(gameItem);
                    else if (null != gameItem.OwnerId) //若直属于角色
                    {
                        gameChar ??= World.CharManager.GetCharFromId(gameItem.OwnerId.Value);
                        if (null == gameChar)
                            return false;
                        gameChar.GameItems.Remove(gameItem);

                    }
                    else if (!(gameItem.ParentId is null))  //若是新加入物品
                    {
                        if (null == gameChar)
                            return false;
                        var tmp = OwHelper.GetAllSubItemsOfTree(gameChar.GameItems, c => c.Children).FirstOrDefault(c => c.Children.Contains(gameItem));
                        if (null == tmp)
                            return false;
                        tmp.Children.Remove(gameItem);
                    }
                    gameItem.Count = count;
                }

            }
            else if (propName.StartsWith(ProjectConstant.LevelPropertyName))  //若是一个级别属性
            {
                var olv = gameItem.GetDecimalWithFcpOrDefault(propName, 0m);    //当前等级
                var nlv = Convert.ToDecimal(val);   //新等级
                if (olv != nlv)    //若需要改变等级
                {
                    string seqPName;
                    seqPName = propName.Length > 2 ? propName[2..] : ProjectConstant.LevelPropertyName;
                    if (seqPName == ProjectConstant.LevelPropertyName)
                        SetLevel(gameItem, (int)nlv);
                    else
                        SetLevel(gameItem, seqPName, (int)nlv);
                    gameItem.Properties[propName] = nlv;    //设置等级
                }
            }
            else if (propName.StartsWith(ProjectConstant.FastChangingPropertyName))  //若设置一个快速变化属性
            {
                if (propName.Length <= ProjectConstant.FastChangingPropertyName.Length)    //若名字太短
                    return false;
                string tmp = propName[ProjectConstant.FastChangingPropertyName.Length..];
                if (tmp.Length < 2)    //若名字太短
                    return false;
                var prefix = tmp[0];
                string innerName = tmp[1..];   //获得实际属性名
                if (!gameItem.Name2FastChangingProperty.TryGetValue(innerName, out var fcp))    //若不存在该属性
                {
                    fcp = new FastChangingProperty(default, default, default, default, DateTime.UtcNow);
                    gameItem.Name2FastChangingProperty[innerName] = fcp;
                }
                fcp.SetPropertyValue(prefix, val);
            }
            else
                gameItem.SetPropertyValue(propName, val);
            DynamicPropertyChangedCollection args = new DynamicPropertyChangedCollection();
            var item = new SimplePropertyChangedCollection() { Thing = gameItem };
            item.Add(new GamePropertyChangedItem<object>(null, name: propName, oldValue: oldValue, newValue: gameItem.Properties[propName]));
            args.Add(item);
            World.EventsManager.OnDynamicPropertyChanged(args);
            return true;
        }

        /// <summary>
        /// 将模板的属性与对象上的属性合并。添加没有的属性。
        /// </summary>
        /// <param name="gameItem"><see cref="GameObjectBase.TemplateId"/>属性必须正确设置。</param>
        /// <returns>true成功设置，false,找不到指定的模板。</returns>
        public bool MergeProperty(GameItem gameItem)
        {
            var template = ItemTemplateManager.GetTemplateFromeId(gameItem.TemplateId); //获取模板
            if (null == template)   //若找不到模板
                return false;
            var seqKeys = template.Properties.Where(c => c.Value is decimal[]).Select(c => (SeqPn: c.Key, IndexPn: ItemTemplateManager.GetIndexPropName(template, c.Key))).ToArray();    //序列属性的名字
            foreach (var (SeqPn, IndexPn) in seqKeys)   //设置序列属性
            {
                SetLevel(gameItem, SeqPn, Convert.ToInt32(gameItem.Properties.GetValueOrDefault(IndexPn, 0m)));
            }
            var keys = template.Properties.Where(c => !(c.Value is decimal[])).Select(c => c.Key).Except(gameItem.Properties.Keys).ToArray(); //需要增加的简单属性的名字
            foreach (var item in keys)  //添加简单属性
                gameItem.Properties[item] = template.Properties[item];
            return true;
        }

        /// <summary>
        /// 仅设置由lv控制的序列属性。
        /// 特别地，并不更改级别属性，调用者要自己更改。如lv并没有变化
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="newLevel"></param>
        public void SetLevel(GameItem gameItem, int newLevel)
        {
            var template = GetTemplate(gameItem);
            var coll = template.Properties.Where(c => c.Value is decimal[] && World.ItemTemplateManager.GetIndexPropName(template, c.Key) == ProjectConstant.LevelPropertyName);    //取得序列属性且其索引属性是通用序列的
            foreach (var item in coll.ToArray())
            {
                SetLevel(gameItem, item.Key, newLevel);
            }
        }

        /// <summary>
        /// 变换物品等级。会对比原等级的属性增减属性数值。如模板中原等级mhp=100,而物品mhp=120，则会用新等级mhp+20。
        /// 特别地，并不更改级别属性，调用者要自己更改。如lv并没有变化
        /// </summary>
        /// <param name="gameItem">要改变的对象。</param>
        /// <param name="seqPName">序列属性的名字。如果对象中没有索引必须的属性，则视同初始化属性。若无序列属性的值，但找到索引属性的话，则视同此属性值是模板中指定的值。</param>
        /// <param name="newLevel">新等级。</param>
        /// <exception cref="ArgumentException">无法找到指定模板。</exception>
        public void SetLevel(GameItem gameItem, string seqPName, int newLevel)
        {
            var template = GetTemplate(gameItem);
            if (null == template)   //若无法找到模板
                throw new ArgumentException($"无法找到指定模板(TemplateId={gameItem.TemplateId}),对象Id={gameItem.Id}", nameof(gameItem));
            if (!template.Properties.TryGetValue(seqPName, out object objSeq) || !(objSeq is decimal[] seq))
                throw new ArgumentOutOfRangeException($"模板{template.Id}({template.DisplayName})中没有指定 {seqPName} 属性，或其不是序列属性");
            var indexPN = ItemTemplateManager.GetIndexPropName(template, seqPName); //索引属性的名字

            if (!gameItem.Properties.TryGetValue(indexPN, out object objLv))  //若没有指定当前等级
            {
                //当前视同需要初始化属性
                gameItem.Properties[seqPName] = seq[newLevel];
            }
            else
            {
                var lv = Convert.ToInt32(objLv);   //当前等级
                var oov = seq[lv];  //原级别模板值

                var val = Convert.ToDecimal(gameItem.Properties.GetValueOrDefault(seqPName, oov));  //物品的属性值
                var old = newLevel < seq.Length ? seq[newLevel] : oov;  //可能缺失最后一级数据
                gameItem.Properties[seqPName] = old + val - oov;
            }
            return;
        }

        /// <summary>
        /// 获取指定角色所有物品的字典，键是物品Id,值是物品对象。
        /// </summary>
        /// <param name="gc"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyDictionary<Guid, GameItem> GetAllChildrenDictionary(GameChar gc)
        {
            //TO DO未来是否需要缓存机制？
            return GetAllChildren(gc).ToDictionary(c => c.Id);
        }

        /// <summary>
        /// 移动一个物品的一部分到另一个容器。
        /// </summary>
        /// <param name="item"></param>
        /// <param name="count"></param>
        /// <param name="destContainer"></param>
        /// <param name="changesItems">物品变化信息，null或省略则不生成具体的变化信息。</param>
        /// <returns>true成功移动了物品，false是以下情况的一种或多种：物品现存数量小于要求移动的数量，没有可以移动的物品,目标背包已经满,。</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/>应该大于0</exception>
        public bool MoveItem(GameItem item, decimal count, GameItemBase destContainer, ICollection<ChangeItem> changesItems = null)
        {
            var container = GetChildrenCollection(destContainer);
            //TO DO 不会堆叠
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "应大于0");
            var result = false;
            var cap = GetFreeCapacity(destContainer);
            if (cap <= 0)   //若目标背包已经满
                return false;
            if (item.Count < count)
                return false;
            if (!item.IsStc(out var stc) || count == item.Count)  //若不可堆叠或全部移动
            {
                var parent = GetContainer(item);   //获取父容器
                MoveItems(parent, c => c.Id == item.Id, destContainer, changesItems);
                result = true;
            }
            else //若可能堆叠且不是全部移动
            {
                stc = stc == -1 ? decimal.MaxValue : stc;
                var moveItem = new GameItem();
                World.EventsManager.GameItemCreated(moveItem, item.TemplateId, null, null, null);
                moveItem.Count = count;
                item.Count -= count;
                var parent = GetContainer(item);   //获取源父容器
                AddItem(moveItem, destContainer, null, changesItems);  //TO DO 需要处理无法完整放入问题
                if (null != changesItems)
                {
                    //增加变化 
                    changesItems.AddToChanges(item.ParentId ?? item.OwnerId.Value, item);
                    //增加新增
                    //changesItems.AddToAdds(moveItem.ParentId ?? moveItem.OwnerId.Value, moveItem);
                }
            }
            return result;
        }

        #region 堆叠和容纳

        /// <summary>
        /// 获取指定容器数量。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns>如果不是容器将返回null,-1表示没有容量限制。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal? GetCapacity(GameObjectBase container)
        {

            if (!container.Properties.TryGetValue(ProjectConstant.ContainerCapacity, out object obj))   //若没有属性
                return null;
            if (obj is decimal count) //若是固定数值
                return count;
            else
            {
                return null;    //TO DO
            }
        }

        /// <summary>
        /// 获取最大子物品容量。
        /// </summary>
        /// <param name="container"></param>
        /// <returns>不是容器则返回0。不限定容量则返回<see cref="int.MaxValue"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetMaxCapacity(GameItemBase container)
        {
            if (!container.Properties.TryGetValue(ProjectConstant.ContainerCapacity, out object obj))   //若没有属性,视同非容器
                return 0;
            if (!OwConvert.TryToDecimal(obj, out var result))
                return 0;
            return result switch
            {
                -1 => int.MaxValue,
                _ => Convert.ToInt32(result),
            };
        }

        /// <summary>
        /// 获取容器剩余的容量。
        /// </summary>
        /// <param name="container"></param>
        /// <returns>0非容器或容器已满，<see cref="int.MaxValue"/>表示无限制。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetFreeCapacity(GameItemBase container)
        {
            var max = GetMaxCapacity(container);
            return max switch
            {
                0 => 0,
                int.MaxValue => int.MaxValue,
                _ => Math.Max(max - (GetChildrenCollection(container)?.Count ?? 0), 0), //容错
            };
        }

        /// <summary>
        /// 获取最大堆叠数量。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns>1是不可堆叠或最大堆叠数量本就是1。</returns>
        public decimal GetMaxStc(GameItem gameItem)
        {
            if (ProjectConstant.MucaiId == gameItem.TemplateId && GetChar(gameItem.Parent) is GameChar gameChar)    //若是木材且正确的挂接到了对象树
            {
                var stcMucai = gameItem.Properties.GetDecimalOrDefault(ProjectConstant.StackUpperLimit, 1);
                stcMucai = stcMucai == -1 ? decimal.MaxValue : stcMucai;
                var hl = gameChar.GetHomeland();

                var coll = hl.GetAllChildren().Where(c => c.TemplateId == ProjectConstant.MucaiStoreTId).Select(c => GetMaxStc(c)).Append(stcMucai);
                if (coll.Any(c => decimal.MaxValue == c))
                    return decimal.MaxValue;
                else
                    return coll.Sum();
            }
            var stc = (int)gameItem.Properties.GetDecimalOrDefault(ProjectConstant.StackUpperLimit, 1); //无属性表示不可堆叠
            return stc switch
            {
                -1 => decimal.MaxValue, //-1表示不限制
                _ => stc,
            };
        }

        /// <summary>
        /// 获取剩余的可堆叠数量。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns>0不可堆叠或没有剩余堆叠数量，<see cref="decimal.MaxValue"/>是无限堆叠。其他值是堆叠剩余值。</returns>
        public decimal GetFreeStc(GameItem gameItem)
        {
            var max = GetMaxStc(gameItem);  //使用这个函数以覆盖木材堆叠问题
            return max switch
            {
                decimal.MaxValue => decimal.MaxValue,
                _ => Math.Max(0, max - gameItem.Count.Value),
            };
        }

        #endregion 堆叠和容纳

        #endregion 动态属性相关

        #region 物品增减相关

        /// <summary>
        /// 将符合条件的物品对象及其子代从容器中移除并返回。
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="filter"></param>
        /// <param name="removes">变化的数据。可以是null或省略，此时忽略。</param>
        public void RemoveItemsWhere(GameObjectBase parent, Func<GameItem, bool> filter, ICollection<GameItem> removes = null)
        {
            IList<GameItem> lst = (parent as GameItem)?.Children;
            lst ??= (parent as GameChar)?.GameItems;
            if (null != lst)
                for (int i = lst.Count - 1; i >= 0; i--)    //倒序删除
                {
                    var item = lst[i];
                    if (!filter(item))
                        continue;
                    lst.RemoveAt(i);
                    removes?.Add(item);
                    item.Parent = null;
                    item.ParentId = null;
                    item.OwnerId = null;
                }

        }

        /// <summary>
        /// 将符合条件的所有物品移入另一个容器。
        /// </summary>
        /// <param name="src"></param>
        /// <param name="filter"></param>
        /// <param name="dest"></param>
        /// <param name="changes">变化的数据。可以是null或省略，此时忽略。</param>
        /// <param name="remainder">无法移动的物品。</param>
        public void MoveItems(GameObjectBase src, Func<GameItem, bool> filter, GameObjectBase dest, ICollection<ChangeItem> changes = null, ICollection<GameItem> remainder = null)
        {
            var tmp = World.ObjectPoolListGameItem.Get();
            var adds = World.ObjectPoolListGameItem.Get();
            var remainder2 = World.ObjectPoolListGameItem.Get();
            remainder ??= new List<GameItem>();
            try
            {
                //移动物品
                RemoveItemsWhere(src, filter, tmp); //移除物品
                var removeIds = tmp.Select(c => c.Id).ToArray();  //移除物品的Id集合
                AddItems(tmp, dest, remainder, changes);
                //已经增加的物品数据
                var addChanges = (new ChangeItem()
                {
                    ContainerId = dest.Id,
                });
                addChanges.Adds.AddRange(adds);
                changes?.Add(addChanges);

                if (remainder.Count > 0)    //若有一些物品不能加入
                {
                    tmp.Clear();
                    AddItems(remainder, src, remainder2, changes);
                    Trace.Assert(remainder2.Count == 0);    //TO DO当前版本逻辑上不会出现此问题
                                                            //变化物品
                    adds.Clear();
                    List<Guid> guids = new List<Guid>();
                    List<(Guid, GameItem)> l_r = new List<(Guid, GameItem)>();
                    removeIds.ApartWithWithRepeated(tmp, c => c, c => c.Id, guids, l_r, adds);

                    var change = new ChangeItem()
                    {
                        ContainerId = src.Id,
                    };
                    change.Removes.AddRange(guids);
                    change.Changes.AddRange(l_r.Select(c => c.Item2));
                    change.Adds.AddRange(adds);
                    changes?.Add(change);
                }
                else //全部移动了
                {
                    if (null != changes)
                        foreach (var item in removeIds)
                            changes.AddToRemoves(src.Id, item);
                }
            }
            finally
            {
                World.ObjectPoolListGameItem.Return(remainder2);
                World.ObjectPoolListGameItem.Return(adds);
                World.ObjectPoolListGameItem.Return(tmp);
            }
        }


        /// <summary>
        /// 将一组物品加入一个容器下。
        /// 如果合并后数量为0，则会试图删除对象。
        /// </summary>
        /// <param name="gameItems">一组对象。</param>
        /// <param name="parent">容器。</param>
        /// <param name="remainder">追加不能放入的物品到此集合，可以是null或省略，此时忽略。</param>
        /// <param name="changeItems">变化的数据。可以是null或省略，此时忽略。</param>
        /// 
        public void AddItems(IEnumerable<GameItem> gameItems, GameObjectBase parent, ICollection<GameItem> remainder = null, ICollection<ChangeItem> changeItems = null)
        {
            foreach (var item in gameItems) //TO DO 性能优化未做
            {
                AddItem(item, parent, remainder, changeItems);
            }
        }

        /// <summary>
        /// 获取指定物品归属到指定角色时的首选默认容器。
        /// </summary>
        /// <param name="gChar"></param>
        /// <param name="gameItem"></param>
        /// <returns></returns>
        public GameThingBase GetDefaultContainer(GameChar gChar, GameItem gameItem)
        {
            var template = GetTemplate(gameItem);
            GameThingBase result;
            switch (template.CatalogNumber)
            {
                case 0:
                    if (gameItem.TemplateId == ProjectConstant.ZuojiZuheRongqi) //若是坐骑/野兽
                    {
                        result = this.IsExistsMounts(gChar, gameItem) ? gChar.GetShoulanBag() : gChar.GetZuojiBag();
                    }
                    else
                        result = null;
                    break;
                case 10:
                    result = gChar.GetShenwenBag();
                    break;
                case 15:    //神纹强化道具
                case 16:    //神纹强化道具
                case 17:    //神纹强化道具
                case 18:    //道具
                    result = gChar.GetItemBag();
                    break;
                case 26:    //时装
                    result = gChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ShizhuangBagSlotId);
                    break;
                case 99:    //货币
                    result = gChar.GetCurrencyBag();
                    break;
                case 30:
                    result = gChar.GetTujianBag();
                    break;
                default:    //不认识的放入道具背包
                    result = gChar.GetItemBag();
                    break;
            }
            return result;
        }

        /// <summary>
        /// 将一个物品放入容器。根据属性确定是否可以合并堆叠。
        /// </summary>
        /// <param name="gameItem">要放入的物品，返回时属性<see cref="GameItem.Count"/>可能被更改。</param>
        /// <param name="parent">放入的容器事物对象，或是一个角色对象。</param>
        /// <param name="remainder">追加不能放入的物品到此集合，可以是null或省略，此时忽略。</param>
        /// <param name="changeItems">变化的数据。可以是null或省略，此时忽略。
        /// 基于堆叠限制和容量限制，无法放入的部分。实际是<paramref name="gameItem"/>对象或拆分后的对象集合，对于可堆叠对象可能修改了<see cref="GameItem.Count"/>属性。若没有剩余则返回null。
        /// </param>
        /// <returns>放入后的对象，如果是不可堆叠或堆叠后有剩余则是 <paramref name="gameItem"/> 和堆叠对象，否则是容器内原有对象。返回空集合，因容量限制没有放入任何物品。</returns>
        public void AddItem(GameItem gameItem, GameObjectBase parent, ICollection<GameItem> remainder = null, ICollection<ChangeItem> changeItems = null)
        {
            IList<GameItem> children = GetChildrenCollection(parent);
            var stcItem = children.FirstOrDefault(c => c.TemplateId == gameItem.TemplateId) ?? gameItem;
            Debug.Assert(null != children);
            if (!stcItem.IsStc(out var stc)) //若不可堆叠
            {
                if (parent is GameItemBase gib) //若是容器
                {
                    var freeCap = GetFreeCapacity(gib);
                    if (freeCap == 0)   //若不可再放入物品
                    {
                        remainder?.Add(gameItem);
                        return;
                    }
                }
                if (parent is GameItem gi && gi.TemplateId == ProjectConstant.ZuojiBagSlotId && this.IsMounts(gameItem) && this.IsExistsMounts(gi.GetGameChar(), gameItem))  //若要放入坐骑且有同款坐骑
                {
                    var bag = gi.GetGameChar().GetShoulanBag();  //兽栏
                    if (GetFreeCapacity(bag) == 0) //若兽栏满
                    {
                        remainder.Add(gameItem);
                        return;
                    }
                    parent = bag;
                }
                var succ = ForcedAdd(gameItem, parent);
                changeItems?.AddToAdds(parent.Id, gameItem);
                if (this.IsMounts(gameItem)) //若是坐骑
                    World.CombatManager.UpdatePveInfo(gameItem.GetGameChar());
                return;
            }
            else //若可堆叠
            {
                gameItem.Count ??= 0;
                var upper = GetCapacity(parent) ?? -1;    //TO DO 暂时未限制是否是容器
                if (-1 != upper && children.Count >= upper)  //若超过容量
                {
                    remainder?.Add(gameItem);
                    return;
                }
                //处理存在物品的堆叠问题
                var result = new List<GameItem>();
                var dest = children.FirstOrDefault(c => c.TemplateId == gameItem.TemplateId && c.GetNumberOfStackRemainder() > 0);    //找到已有的物品且尚可加入堆叠的
                if (null != dest)  //若存在同类物品
                {
                    var redCount = Math.Min(dest.GetNumberOfStackRemainder(), gameItem.Count ?? 0);   //移动的数量
                    ForcedAddCount(gameItem, -redCount, changeItems);
                    ForcedAddCount(dest, redCount, changeItems);
                    result.Add(dest);
                }
                if (gameItem.Count <= 0)   //若已经全部堆叠进入
                {
                    return;
                }
                else if (gameItem.GetTemplate() == null || !gameItem.GetTemplate().Properties.TryGetDecimal("isuni", out var isuni) || isuni != decimal.One) //放入剩余物品,容错
                {
                    var tmp = new List<GameItem>();
                    SplitItem(gameItem, tmp, parent);
                    var _ = new ChangeItem(parent.Id);
                    for (int i = tmp.Count - 1; i >= 0; i--)
                    {
                        if (-1 != upper && children.Count >= upper)    //若已经满
                            break;
                        var item = tmp[i];
                        if (ForcedAdd(item, parent))    //若成功加入
                        {
                            result.Add(item);
                            _.Adds.Add(item);
                        }
                        else //TO DO当前不会不成功
                            throw new InvalidOperationException("加入物品异常失败。");
                        tmp.RemoveAt(i);
                    }
                    if (_.Adds.Count > 0)
                        changeItems?.Add(_);
                    foreach (var item in tmp)   //未能加入的
                        remainder?.Add(item);
                }
                else
                {
                    remainder?.Add(gameItem);
                }
                //当有剩余物品
                return;
            }
        }

        /// <summary>
        /// 测试指定物品是否可以完整的放入指定容器中。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public bool IsAllowAdd(GameItem gameItem, GameObjectBase parent)
        {
            IList<GameItem> children = GetChildrenCollection(parent);
            var stcItem = children.FirstOrDefault(c => c.TemplateId == gameItem.TemplateId) ?? gameItem;
            if (!stcItem.IsStc(out var stc)) //若不可堆叠
            {
                if (parent is GameItemBase gib) //若是容器
                {
                    var freeCap = GetFreeCapacity(gib);
                    if (freeCap == 0)   //若不可再放入物品
                    {
                        return false;
                    }
                }
                if (parent is GameItem gi && gi.TemplateId == ProjectConstant.ZuojiBagSlotId && this.IsMounts(gameItem) && this.IsExistsMounts(gi.GetGameChar(), gameItem))  //若要放入坐骑且有同款坐骑
                {
                    var bag = gi.GetGameChar().GetShoulanBag();  //兽栏
                    if (GetFreeCapacity(bag) == 0) //若兽栏满
                    {
                        return false;
                    }
                    parent = bag;
                }
                return true;
            }
            else //若可以堆叠
            {
                var dest = children.FirstOrDefault(c => c.TemplateId == gameItem.TemplateId && c.GetNumberOfStackRemainder() > 0);    //找到已有的物品且尚可加入堆叠的
                var re = dest?.GetNumberOfStackRemainder() ?? decimal.MaxValue;
                return re >= gameItem.Count;
            }
        }

        /// <summary>
        /// 加入非堆叠物品，
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="parent"></param>
        /// <param name="changeItems"></param>
        private bool AddItemNoneStack(GameItem gameItem, GameObjectBase parent, ICollection<ChangeItem> changeItems = null)
        {
            Debug.Assert(!gameItem.IsStc(out var stc), "只能针对非堆叠物品。");
            gameItem.Count ??= 1;
            var upper = GetCapacity(parent) ?? -1;    //TO DO 暂时未限制是否是容器

            IList<GameItem> children = GetChildrenCollection(parent);
            if (-1 != upper && children.Count >= upper)  //若超过容量
            {
                return false;
            }
            var succ = ForcedAdd(gameItem, parent);
            changeItems?.AddToAdds(parent.Id, gameItem);
            if (this.IsMounts(gameItem)) //若是坐骑
                World.CombatManager.UpdatePveInfo(gameItem.GetGameChar());
            return true;
        }

        /// <summary>
        /// 按堆叠要求将物品拆分未多个。
        /// </summary>
        /// <param name="gameItem">要拆分的物品。返回时该物品<see cref="GameItem.Count"/>可能被改变。
        /// 若未设置数量，则对不可堆叠物品视同0，可堆叠物品视同1。</param>
        /// <param name="results">拆分后的物品。可能包含<paramref name="gameItem"/>，且其<see cref="GameItem.Count"/>属性被修正。
        /// <paramref name="gameItem"/>总是被放在第一个位置上。</param>
        /// <param name="parent">父对象。</param>
        public void SplitItem(GameItem gameItem, ICollection<GameItem> results, GameObjectBase parent = null)
        {
            var stcItem = gameItem;
            if (gameItem.TemplateId == ProjectConstant.MucaiId)  //若是木材
            {
                var gameCher = parent as GameChar ?? (parent as GameItem)?.GetGameChar();
                stcItem = gameCher.GetMucai();
            }
            if (!stcItem.IsStc(out var stc)) //若不可堆叠
            {
                gameItem.Count ??= 1;
                var count = gameItem.Count.Value - 1;   //记录原始数量少1的值
                gameItem.Count = Math.Min(gameItem.Count.Value, 1); //设置第一堆的数量
                results.Add(gameItem);
                for (; count >= 0; count--)   //分解出多余物品
                {
                    var item = new GameItem();
                    World.EventsManager.GameItemCreated(item, gameItem.TemplateId);
                    item.Count = Math.Min(1, count);
                    results.Add(item);
                }
            }
            else if (-1 == stc)  //若无堆叠上限限制
            {
                results.Add(gameItem);
            }
            else //若可堆叠且有限制
            {
                gameItem.Count ??= 0;
                var count = gameItem.Count.Value - stc;   //记录当前数量减去第一堆以后的数量
                gameItem.Count = Math.Min(gameItem.Count.Value, stc);    //取当前数量，和允许最大堆叠数量中较小的值
                results.Add(gameItem);  //加入第一个堆
                while (count > 0) //当需要拆分
                {
                    var item = new GameItem();
                    World.EventsManager.GameItemCreated(item, gameItem.TemplateId);
                    item.Count = Math.Min(count, stc); //取当前剩余数量，和允许最大堆叠数量中较小的值
                    results.Add(item);
                    count -= stc;
                }
            }
        }

        /// <summary>
        /// 无视容量限制堆叠规则。将物品加入指定容器。
        /// </summary>
        /// <param name="gameItem">无视容量限制堆叠规则，不考虑原有容器。</param>
        /// <param name="container">无视容量限制堆叠规则。</param>
        /// 
        /// <returns>true成功加入.false <paramref name="container"/>不是可以容纳物品的类型。</returns>
        public bool ForcedAdd(GameItem gameItem, GameObjectBase container)
        {
            if (container is GameChar gameChar)  //若容器是角色
            {
                gameItem.GenerateIdIfEmpty();
                gameChar.GameItems.Add(gameItem);
                gameItem.OwnerId = gameChar.Id;
            }
            else if (container is GameItem item)
            {
                gameItem.GenerateIdIfEmpty();
                item.Children.Add(gameItem);
                gameItem.ParentId = item.Id;
                gameItem.Parent = item;
            }
            else
                return false;
            return true;
        }

        /// <summary>
        /// 设置数量属性，并考虑自动删除对象等事项。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="count"></param>
        /// <param name="changes">变化数据，可以是空表示不记录变化数据。</param>
        public virtual bool ForcedSetCount(GameItem gameItem, decimal count, [AllowNull] ICollection<ChangeItem> changes = null)
        {
            gameItem.Count = count;
            if( gameItem.Parent is null)    //若设置的是游离对象
            {

            }
            else if (decimal.Zero == gameItem.Count )   //若已经变为0
            {
                if (!gameItem.IsStc(out _) || gameItem.Parent?.TemplateId != ProjectConstant.CurrencyBagTId)   //若应删除对象
                {
                    var pid = gameItem.ParentId ?? gameItem.OwnerId.Value;
                    if (!ForceDelete(gameItem)) //若无法删除
                        return false;
                    changes?.AddToRemoves(pid, gameItem.Id);
                }
                else //不用删除对象
                {
                    changes?.AddToChanges(gameItem);
                }
            }
            else //非0
            {
                changes?.AddToChanges(gameItem);
            }
            return true;

        }

        /// <summary>
        /// 强制修改数量。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="count"></param>
        /// <param name="changes">变化数据，可以是空表示不记录变化数据。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ForcedAddCount(GameItem gameItem, decimal count, [AllowNull] ICollection<ChangeItem> changes = null) =>
            ForcedSetCount(gameItem, gameItem.Count.GetValueOrDefault() + count, changes);

        /// <summary>
        /// 强制移动物品。无视容量限制堆叠规则。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="container"></param>
        /// <returns>true成功移动，false未知原因没有移动成功。当前不可能失败。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ForceMove(GameItem gameItem, GameItemBase container)
        {
            return ForceRemove(gameItem) && ForcedAdd(gameItem, container);
        }

        /// <summary>
        /// 强制将一个物品从它现有容器中移除。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns>true成功移除，false物品当前没有容器。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="gameItem"/>是null。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ForceRemove(GameItem gameItem)
        {
            bool result;
            var container = GetContainer(gameItem);
            if (null != container)
                result = GetChildrenCollection(container)?.Remove(gameItem) ?? false;
            else
                result = false;
            gameItem.Parent = null; gameItem.ParentId = gameItem.OwnerId = null;
            return result;
        }

        /// <summary>
        /// 强制彻底删除一个物品。会正确设置导航属性。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="db">使用的数据库上下文，如果省略或为null则会在关系中寻找。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ForceDelete(GameItem gameItem, DbContext db = null)
        {
            var gc = gameItem.GetGameChar();   //保存所属角色
            db ??= gc?.GameUser?.DbContext;
            bool result = ForceRemove(gameItem);
            if (result)   //若成功移除关系
            {
                if (!(db is null) && db.Entry(gameItem).State != EntityState.Detached)  //若非新加入的物品
                    db.Remove(gameItem);
            }
            return result;
        }
        #endregion 物品增减相关

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
            foreach (var (tmp, tt) in coll)
            {
                tmp.GenerateIdIfEmpty();
                tmp.SetTemplate(tt);
                adds.Clear();
                tmp.Children.ApartWithWithRepeated(tt.ChildrenTemplateIds, c => c.TemplateId, c => c, null, null, adds);
                foreach (var addItem in adds)
                {
                    var newItem = new GameItem();
                    World.EventsManager.GameItemCreated(newItem, addItem, tmp, null);
                    tmp.Children.Add(newItem);
                }
            }
        }

        /// <summary>
        /// 按Id获取物品/容器对象集合。
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="items">找到的物品或容器。</param>
        /// <param name="parent">仅在这个对象的直接或间接子代中搜索，如果指定一个角色对象可以搜寻角色下所有物品。</param>
        /// <returns>true所有指定Id均被获取，false,至少有一个物品没有找到，</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetItems(IEnumerable<Guid> ids, ICollection<GameItem> items, GameObjectBase parent)
        {
            return GetItems(ids, items, GetAllChildren(parent).Join(ids, c => c.Id, c => c, (l, r) => l));
        }

        /// <summary>
        /// 返回指定Id的对象。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="gameChar">指定所属的角色对象。</param>
        /// <returns>没有找到则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameItem GetItemFromId(Guid id, GameChar gameChar)
        {
            return OwHelper.GetAllSubItemsOfTree(gameChar.GameItems, c => c.Children).FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// 按Id获取物品/容器对象集合。
        /// </summary>
        /// <param name="ids">id的集合。</param>
        /// <param name="items">得到的结果集追加到此集合内，即便没有获取到所有对象，也会追加找到的对象。</param>
        /// <param name="gameItems">仅在该集合中搜索。</param>
        /// <returns></returns>
        public bool GetItems(IEnumerable<Guid> ids, ICollection<GameItem> items, IEnumerable<GameItem> gameItems)
        {
            var coll = gameItems.Join(ids, c => c.Id, c => c, (l, r) => l);
            var lst = World.ObjectPoolListGameItem.Get();
            try
            {
                lst.AddRange(coll);
                var count = ids.Count();
                lst.ForEach(c => items.Add(c));
                return lst.Count == count;
            }
            finally
            {
                World.ObjectPoolListGameItem.Return(lst);
            }
        }

        /// <summary>
        /// 获取指定物品所属的角色对象。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns>物品所属的角色，如果没有找到则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameChar GetChar(GameItem gameItem)
        {
            GameItem tmp;
            for (tmp = gameItem; tmp.Parent != null; tmp = tmp.Parent) ;
            return tmp.OwnerId is null ? null : World.CharManager.GetCharFromId(tmp.OwnerId.Value);
        }

        /// <summary>
        /// 获取指定物品的直接父容器。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="gameChar"></param>
        /// <returns>返回父容器可能是另一个物品或角色对象，没有找到则返回null。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="gameItem"/>是null。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameObjectBase GetContainer(GameItem gameItem, GameChar gameChar = null)
        {
            var result = gameItem.Parent as GameObjectBase ?? (gameItem.OwnerId is null ? null : World.CharManager.GetCharFromId(gameItem.OwnerId.Value));
            if (result is null && gameChar != null)
            {
                var ptid = gameChar.Properties.GetGuidOrDefault("ptid");
                result = gameChar.AllChildren.FirstOrDefault(c => c.TemplateId == ptid);
            }
            return result;
        }

        /// <summary>
        /// 获取容器的子对象的集合接口。
        /// </summary>
        /// <param name="gameThing">容器对象。</param>
        /// <returns>子代容器的接口，null表示没有找到。特别地，当参数是null时也会返回null而不引发异常。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IList<GameItem> GetChildrenCollection(GameObjectBase gameThing)
        {
            var children = (gameThing as GameItem)?.Children;
            return children ?? (gameThing as GameChar)?.GameItems;
        }

        /// <summary>
        /// 获取指定容器所有子代的可枚举对象。这是延迟执行的对象。枚举过程中更改对象可能导致异常。
        /// </summary>
        /// <param name="gameThing"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<GameItem> GetAllChildren(GameObjectBase gameThing)
        {
            var _ = GetChildrenCollection(gameThing);
            return OwHelper.GetAllSubItemsOfTree(_, c => c.Children);
        }

        ///// <summary>
        ///// 变化指定物品的模板。
        ///// 追加新属性，变化序列属性。
        ///// </summary>
        ///// <param name="container"></param>
        ///// <param name="newContainer"></param>
        //public void ChangeTemplate(GameItem container, GameItemTemplate newContainer)
        //{
        //    if (container.TemplateId == newContainer.Id)    //若无需换Id
        //        return;
        //    var keys = container.Properties.Keys.Intersect(newContainer.Properties.Keys).ToArray(); //共有Id
        //    foreach (var key in keys)
        //    {
        //        if (container.TryGetDecimalPropertyValue(key, out var tmp))   //若是数值属性
        //        {
        //            var lv = container.GetIndexPropertyValue(key);
        //            if (lv >= 0)    //若是序列属性
        //                tmp -= seq[lv];
        //            var seq = container.Template.GetSequenceProperty<decimal>(key);
        //            if (null != seq)   //若是序列属性
        //            {
        //                var lv = container.GetIndexPropertyValue(key);
        //                if (lv >= 0)    //若是序列属性
        //                    tmp -= seq[lv];
        //            }
        //            else //非序列属性
        //                newContainer.GetSequenceValueOrValue(key);
        //        }
        //        else //若非数值属性
        //        {

        //        }
        //        //TO DO
        //        container.Properties[key] =;
        //    }
        //    foreach (var key in keys.Except(newContainer.Properties.Keys))  //追加属性值
        //    {

        //    }
        //    container.TemplateId = newContainer.Id;
        //    container.Template = newContainer;
        //}

        /// <summary>
        /// 获取指定阵容的所有坐骑。
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public IEnumerable<GameItem> GetLineup(GameChar gameChar, int number)
        {
            var bag = gameChar.GetZuojiBag();
            var coll = bag.Children.Where(c =>
            {
                return c.Properties.Keys.Any(c1 =>
                {
                    if (!c1.StartsWith(ProjectConstant.ZhenrongPropertyName) || c1.Length <= ProjectConstant.ZhenrongPropertyName.Length) return false;
                    var index = ProjectConstant.ZhenrongPropertyName.Length;
                    if (!int.TryParse(c1[index..], out var num)) return false;
                    return num == number;
                });
            });
            return coll;
        }

        /// <summary>
        /// 卖出物品。
        /// </summary>
        /// <param name="datas">工作参数及返回值封装类。<seealso cref="SellDatas"/></param>
        public void Sell(SellDatas datas)
        {
            using var disposer = datas.LockUser();
            if (disposer is null) return;
            var gc = datas.GameChar;
            var shoulan = gc.GetShoulanBag();
            if (datas.SellIds.Select(c => c.Item1).Distinct().Count() != datas.SellIds.Count)
            {
                datas.ErrorCode = (int)HttpStatusCode.BadRequest;
                datas.ErrorMessage = "物品Id重复。";
                datas.HasError = true;
                return;
            }
            var coll = (from sellItem in datas.SellIds
                        join gi in datas.GameChar.AllChildren
                        on sellItem.Item1 equals gi.Id
                        select (gi, sellItem.Item2));

            List<(GameItem, decimal, decimal, decimal)> list = new List<(GameItem, decimal, decimal, decimal)>();
            var totalGold = 0m; var totalDia = 0m;  //总计价格
            foreach (var item in coll)
            {
                if (ComputeGoldPrice(item.gi, out var gold, out var dia))
                {
                    totalGold += gold * item.Item2;
                    totalDia += dia * item.Item2;
                    list.Add((item.gi, item.Item2, dia, gold));
                }
            }
            if (list.Count != datas.SellIds.Count)
            {
                datas.ErrorCode = (int)HttpStatusCode.BadRequest;
                datas.ErrorMessage = "至少一个指定的Id不存在或不能出售。";
                datas.HasError = true;
                return;
            }
            var gim = World.ItemManager;
            var qiwu = datas.GameChar.GetQiwuBag(); //回收站

            //改写物品对象
            foreach (var item in list)
            {
                //datas.ChangeItems.AddToRemoves(item.Item1.ContainerId.Value, item.Item1.Id);
                gim.MoveItem(item.Item1, item.Item2, qiwu, datas.ChangeItems);
                //datas.ChangeItems.AddToAdds(qiwu.Id, item.Item1);
            }
            //改写金币
            if (totalGold != 0)
            {
                var jinbi = datas.GameChar.GetJinbi();
                jinbi.Count += totalGold;
                datas.ChangeItems.AddToChanges(jinbi);

            }
            //改写钻石
            if (totalDia != 0)
            {
                var zuanshi = datas.GameChar.GetZuanshi();
                zuanshi.Count += totalDia;
                datas.ChangeItems.AddToChanges(zuanshi);
            }
        }

        /// <summary>
        /// 计算某个物品的金币售价。
        /// </summary>
        /// <param name="gold">金币售价。</param>
        /// <param name="dia">钻石售价。</param>
        /// <returns>true表示可以出售，false表示不可出售物品。</returns>
        public bool ComputeGoldPrice(GameItem item, out decimal gold, out decimal dia)
        {
            if (this.IsMounts(item))
            {
                gold = this.GetBody(item).Properties.GetDecimalOrDefault("sg");
                var totalNe = item.Properties.GetDecimalOrDefault("neatk", 0m) +   //总资质值
                    item.Properties.GetDecimalOrDefault("nemhp", 0m) +
                    item.Properties.GetDecimalOrDefault("neqlt", 0m);
                totalNe = Math.Round(totalNe, MidpointRounding.AwayFromZero);  //取整，容错
                decimal mul;

                if (totalNe >= 0 && totalNe <= 60) mul = 1;
                else if (totalNe >= 61 && totalNe <= 120) mul = 1.5m;
                else if (totalNe >= 121 && totalNe <= 180) mul = 2;
                else if (totalNe >= 181 && totalNe <= 240) mul = 3;
                else if (totalNe >= 241 && totalNe <= 300) mul = 4;
                else mul = 0;
                gold = mul * gold;
                dia = 0;
            }
            else
            {
                gold = item.GetDecimalWithFcpOrDefault("sg");
                dia = item.GetDecimalWithFcpOrDefault("sd");
            }
            return true;
        }

        /// <summary>
        /// 设置阵容号，或取消阵容设置。
        /// </summary>
        /// <param name="datas"><see cref="SetLineupDatas"/></param>
        public void SetLineup(SetLineupDatas datas)
        {
            using var disposer = datas.LockUser();
            if (disposer is null)
                return;

            var gc = datas.GameChar;
            var srs = new HashSet<Guid>(gc.GetZuojiBag().Children.Select(c => c.Id));
            if (!srs.IsSupersetOf(datas.Settings.Select(c => c.Item1)))
            {
                datas.ErrorMessage = "至少一个指定的坐骑Id不存在。";
                datas.HasError = true;
                datas.ErrorCode = (int)HttpStatusCode.BadRequest;
                return;
            }
            var pchange = new DynamicPropertyChangedCollection();   //属性变化数据类
            var db = gc.GameUser.DbContext;
            foreach (var item in datas.Settings)    //逐个设置
            {
                var mounts = gc.GetMounetsFromId(item.Item1);
                var key = $"{ProjectConstant.ZhenrongPropertyName}{item.Item2}";
                if (item.Item3 != -1)  //若设置阵容
                {
                    //mounts.Properties[key] = item.Item3;
                    pchange.MarkAndSet(mounts, key, item.Item3);
                    if (item.Item2 == 10)  //若是家园展示
                    {
                        var tid = World.ItemManager.GetBody(mounts).TemplateId; //身体的模板Id
                        var sr = db.Set<GameSocialRelationship>().Find(gc.Id, tid, SocialConstant.HomelandShowKeyType);
                        if (sr is null)
                        {
                            sr = new GameSocialRelationship
                            {
                                Id = gc.Id,
                                Id2 = tid,
                                KeyType = SocialConstant.HomelandShowKeyType,
                            };
                            db.Add(sr);
                        }
                    }
                }
                else //若取消阵容设置
                {
                    //mounts.Properties.Remove(key);
                    pchange.MarkAndRemove(mounts, key);
                    if (item.Item2 == 10)  //若是家园展示
                    {
                        var tid = World.ItemManager.GetBody(mounts).TemplateId; //身体的模板Id
                        var sr = db.Set<GameSocialRelationship>().Find(gc.Id, tid, SocialConstant.HomelandShowKeyType);
                        if (null != sr)
                        {
                            db.Remove(sr);
                        }
                    }
                }
                datas.ChangeItems.AddToChanges(mounts);
            }
            World.EventsManager.OnDynamicPropertyChanged(pchange);
            World.CharManager.NotifyChange(gc.GameUser);
        }

        /// <summary>
        /// 使用道具。
        /// </summary>
        public void UseItems(UseItemsWorkDatas datas)
        {
            using var dwChar = datas.LockUser();
            if (dwChar is null)
            {
                datas.ErrorCode = (int)HttpStatusCode.Unauthorized;
                datas.HasError = true;
                return;
            }
            var gim = World.ItemManager;
            var qiwuBag = datas.GameChar.GetQiwuBag();
            var bpMng = datas.World.BlueprintManager;
            GameItem gi = datas.Item.Item1;
            var item = datas.Item.Item1;
            var bpid = gi.Properties.GetGuidOrDefault("usebpid", Guid.Empty);
            if (bpid != Guid.Empty)    //若指定了蓝图
            {
                var template = bpMng.GetTemplateFromId(bpid) as BlueprintTemplate;
                using ApplyBlueprintDatas bpDatas = new ApplyBlueprintDatas(Service, datas.GameChar)
                {
                    Count = datas.Count,
                    Blueprint = template,
                };
                bpDatas.GameItems.Add(gi);
                World.BlueprintManager.ApplyBluprint(bpDatas);
                datas.ErrorCode = bpDatas.ErrorCode;
                datas.ErrorMessage = bpDatas.ErrorMessage;
                if (!datas.HasError)
                {
                    datas.ChangeItems.AddRange(bpDatas.ChangeItems);
                }
            }
            else //若未指定蓝图
                for (int i = 0; i < datas.Count; i++)   //多次单个使用物品
                {
                    //准备数据
                    var tid = gi.Properties.GetGuidOrDefault("usetid", Guid.Empty);
                    var ptid = gi.Properties.GetGuidOrDefault("useptid", Guid.Empty);
                    if (tid == Guid.Empty || ptid == Guid.Empty || !gi.Properties.ContainsKey("usecount"))  //若数据不齐
                    {
                        //TO DO
                        continue;
                    }
                    var count = gi.Properties.GetDecimalOrDefault("usecount", 0);
                    //校验结构
                    var parent = tid == ProjectConstant.CharTemplateId ? datas.GameChar as GameThingBase : datas.GameChar.AllChildren.FirstOrDefault(c => c.TemplateId == ptid);
                    if (parent is null)  //若找不到容器
                    {
                        //TO DO
                        continue;
                    }
                    //生成新物品
                    var giAdd = new GameItem();
                    World.EventsManager.GameItemCreated(giAdd, tid);
                    giAdd.Count = count;
                    //修改数据
                    if (IsAllowAdd(giAdd, parent))
                    {
                        gim.AddItem(giAdd, parent, null, datas.ChangeItems);    //加入新物品
                        datas.SuccCount++;
                        gi.Count -= 1;
                        if (gi.Count > 0)
                            datas.ChangeItems.AddToChanges(gi);
                        else
                        {
                            datas.ChangeItems.AddToRemoves(gi.ParentId.Value, gi.Id);
                            World.ItemManager.ForceDelete(gi);
                        }
                    }
                    else
                        break;
                }
            ChangeItem.Reduce(datas.ChangeItems);
            if (datas.ChangeItems.Count > 0)
                World.CharManager.NotifyChange(datas.GameChar.GameUser);
        }

        public void GetRankOfTuiguan(GetRankOfTuiguanDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            var dbSet = datas.UserContext.Set<GameExtendProperty>().AsNoTracking();
            var gc = datas.GameChar;
            var gp = gc.ExtendProperties.FirstOrDefault(c => c.Name == ProjectConstant.ZhangLiName);
            if (gp is null)
            {
                gp = new GameExtendProperty()
                {
                    Id = gc.Id,
                    Name = ProjectConstant.ZhangLiName,
                    StringValue = gc.DisplayName,
                    DecimalValue = 0,
                };
                gc.ExtendProperties.Add(gp);
            }
            var coll = from tmp in dbSet    //排名在当前角色之前的角色
                       where tmp.Name == ProjectConstant.ZhangLiName && (tmp.DecimalValue > gp.DecimalValue.Value || tmp.DecimalValue == gp.DecimalValue.Value && string.Compare(tmp.StringValue, gc.DisplayName) < 0)
                       orderby tmp.DecimalValue, tmp.StringValue
                       select tmp;
            var rank = coll.Count();
            datas.Rank = rank;
            datas.Scope = gp.DecimalValue.Value;
            var prv = coll.Take(25).ToList();   //排在前面的的紧邻数据
            datas.Prv.AddRange(prv);

            var collNext = from tmp in dbSet    //排在指定角色之后的
                           where tmp.Name == ProjectConstant.ZhangLiName && (tmp.DecimalValue < gp.DecimalValue.Value || tmp.DecimalValue == gp.DecimalValue && string.Compare(tmp.StringValue, gc.DisplayName) > 0)
                           orderby tmp.DecimalValue descending, tmp.StringValue descending
                           select tmp;
            var next = collNext.Take(25).ToList();
            datas.Next.AddRange(next);
        }

        /// <summary>
        /// 追加物品，如果无法放入则发送邮件邮寄。
        /// </summary>
        /// <param name="datas"></param>
        public void AddItemsOrMail(AddItemsOrMailDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            var container = datas.GameChar.AllChildren.ToLookup(c => c.TemplateId);

            List<GameItem> re = new List<GameItem>();
            var coll = (from tmp in datas.Items
                        group tmp.Item1 by tmp.Item2 into g
                        let parent = container[g.Key].FirstOrDefault()
                        where parent != null
                        select (g, parent)).ToList();

            foreach (var (g, parent) in coll)
            {
                foreach (var item in g)
                {
                    if (parent.TemplateId == ProjectConstant.ZuojiBagSlotId && this.IsMounts(item) && this.IsExistsMounts(datas.GameChar, item))   //若向坐骑背包放入重复坐骑
                        AddItem(item, datas.GameChar.GetShoulanBag(), re, datas.ChangeItems);
                    else
                        AddItem(item, parent, re, datas.ChangeItems);
                }
                //AddItems(g, parent, re, datas.ChangeItems);
            }
            if (re.Count > 0)  //若需要发送邮件
            {
                var mail = new GameMail();
                World.SocialManager.SendMail(mail, new Guid[] { datas.GameChar.Id }, SocialConstant.FromSystemId, re.Select(c => (c, GetDefaultContainer(datas.GameChar, c).TemplateId)));
            }
            ChangeItem.Reduce(datas.ChangeItems);
        }

    }

    public class AddItemsOrMailDatas : ChangeItemsAndMailWorkDatsBase
    {
        public AddItemsOrMailDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public AddItemsOrMailDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public AddItemsOrMailDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 要加入的物品和指定的容器的模板Id。
        /// </summary>
        public List<(GameItem, Guid)> Items { get; } = new List<(GameItem, Guid)>();
    }

    public class UseItemsView
    {
        private readonly VWorld _World;
        private readonly GameItem _GameItem;

        public UseItemsView(GameItem gameItem, VWorld world)
        {
            _World = world;
            _GameItem = gameItem;
        }

        private readonly List<(Guid, decimal, Guid)> _Datas;

        public List<(Guid, decimal, Guid)> Datas
        {
            get
            {
                if (_Datas is null)
                {
                }
                return _Datas;
            }
        }
    }

    public class GetRankOfTuiguanDatas : ComplexWorkDatasBase
    {
        public GetRankOfTuiguanDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public GetRankOfTuiguanDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public GetRankOfTuiguanDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }


        public List<GameExtendProperty> Prv { get; } = new List<GameExtendProperty>();

        public List<GameExtendProperty> Next { get; } = new List<GameExtendProperty>();

        /// <summary>
        /// 自己的战力排名。
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// 自己的战力。
        /// </summary>
        public decimal Scope { get; set; }
    }

    public class UseItemsWorkDatas : ChangeItemsWorkDatasBase
    {
        public UseItemsWorkDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public UseItemsWorkDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public UseItemsWorkDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 要使用物品的唯一Id集合。
        /// </summary>
        public Guid ItemId { get; set; }

        /// <summary>
        /// 要使用的数量。
        /// </summary>
        public int Count { get; set; }

        private Tuple<GameItem, decimal> _Item;
        /// <summary>
        /// 要使用的物品。
        /// </summary>
        public Tuple<GameItem, decimal> Item
        {
            get
            {
                if (_Item is null)
                {
                    var gi = GameChar.AllChildren.FirstOrDefault(c => c.Id == ItemId);
                    if (gi is null)
                        throw new ArgumentException("有一个Id不是有效物品。");
                    _Item = Tuple.Create(gi, (decimal)Count);
                }
                return _Item;
            }
        }

        /// <summary>
        /// 实际成功使用的次数。
        /// </summary>
        public int SuccCount { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SellDatas : ChangeItemsWorkDatasBase
    {
        public SellDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public SellDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public SellDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        public List<(Guid, decimal)> SellIds { get; } = new List<(Guid, decimal)>();
    }

    /// <summary>
    /// <see cref="GameItemManager.SetLineup(SetLineupDatas)"/>使用的参数和返回值封装类。
    /// </summary>
    public class SetLineupDatas : ChangeItemsWorkDatasBase
    {
        public SetLineupDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public SetLineupDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public SetLineupDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// Item3是位置号，-1表示取消该坐骑在该阵容中的设置。
        /// </summary>
        public List<(Guid, int, decimal)> Settings { get; } = new List<(Guid, int, decimal)>();
    }

    public class ActiveStyleDatas
    {
        public ActiveStyleDatas()
        {
        }

        public HomelandFangan Fangan { get; set; }

        public string Message { get; set; }
        public bool HasError { get; set; }

        public List<ChangeItem> ItemChanges { get; set; }
        public GameChar GameChar { get; set; }
    }

    /// <summary>
    /// 封装<see cref="GameItemManager"/>扩展方法。
    /// </summary>
    public static class GameItemManagerExtensions
    {
        public static byte[] ToByteArray([NotNull] this GameItemManager manager, [NotNull] IEnumerable<GameItem> gameItems)
        {
            MemoryStream ms = new MemoryStream();
            using (var sw = new BinaryWriter(ms))
            {
                manager.Fill(gameItems, sw);
                sw.Flush();
            }
            return ms.ToArray();
        }

        public static void Fill([NotNull] this GameItemManager manager, [NotNull] IEnumerable<GameItem> gameItems, [NotNull] BinaryWriter writer)
        {
            var count = gameItems.Count();
            writer.Write(count);
            foreach (var item in gameItems)
            {
                manager.Fill(item, writer);
            }
        }

        public static void Fill(this GameItemManager manager, GameItem gameItem, BinaryWriter writer)
        {
            writer.Write(OwConvert.ToString(gameItem.Properties) ?? string.Empty);  //这个属性需要最先被写入
            writer.Write(gameItem.GetClientString() ?? string.Empty);
            writer.Write(gameItem.Count);
            writer.Write(DateTime.UtcNow);   //TO DO 保留二进制兼容性
            writer.Write(gameItem.Id);
            writer.Write(gameItem.OwnerId);
            writer.Write(gameItem.ParentId);
            writer.Write(gameItem.TemplateId);
            writer.Write(gameItem.Children.Count);
            foreach (var item in gameItem.Children)
            {
                manager.Fill(item, writer);
            }
        }

        public static IEnumerable<GameItem> ToGameItems(this GameItemManager manager, BinaryReader reader)
        {
            var result = new List<GameItem>();
            manager.Fill(reader, result);
            return result;
        }

        public static IEnumerable<GameItem> ToGameItems(this GameItemManager manager, byte[] buffer)
        {
            if (buffer is null || buffer.Length <= 0)
                return Array.Empty<GameItem>();
            using var ms = new MemoryStream(buffer, false);
            using var reader = new BinaryReader(ms);
            return manager.ToGameItems(reader);
        }

        /// <summary>
        /// 填充对象，调用者需要调用<see cref="GameEventsManager.GameItemLoaded(GameItem)"/>以完成加载后的初始化工作。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="reader"></param>
        /// <param name="gameItem"></param>
        public static void Fill(this GameItemManager manager, BinaryReader reader, GameItem gameItem)
        {
            gameItem.PropertiesString = reader.ReadString();    //这个属性需要最先读取
            gameItem.SetClientString(reader.ReadString());
            gameItem.Count = reader.ReadNullableDecimal();
            _ = reader.ReadDateTime();  //TO DO 保留二进制兼容性
            gameItem.Id = reader.ReadGuid();
            gameItem.OwnerId = reader.ReadNullableGuid();
            gameItem.ParentId = reader.ReadNullableGuid();
            gameItem.TemplateId = reader.ReadGuid();
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var gi = new GameItem();
                manager.Fill(reader, gi);
                gameItem.Children.Add(gi);
            }
            gameItem.Children.ForEach(c =>
            {
                c.Parent = gameItem;
            });
        }

        public static void Fill(this GameItemManager manager, BinaryReader reader, ICollection<GameItem> gameItems)
        {
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var gi = new GameItem();
                manager.Fill(reader, gi);
                gameItems.Add(gi);
            }
        }

    }

    public static class BinaryExtensions
    {
        #region 读取器扩展

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid ReadGuid(this BinaryReader reader)
        {
            var guts = reader.ReadBytes(16);
            return new Guid(guts);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ReadDateTime(this BinaryReader reader)
        {
            return new DateTime(reader.ReadInt64());
        }

        public static decimal? ReadNullableDecimal(this BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return null;
            return reader.ReadDecimal();
        }

        public static Guid? ReadNullableGuid(this BinaryReader reader)
        {
            if (!reader.ReadBoolean())
                return null;
            return reader.ReadGuid();
        }

        #endregion 读取器扩展

        #region 写入器扩展

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this BinaryWriter writer, Guid guid)
        {
            writer.Write(guid.ToByteArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this BinaryWriter writer, DateTime dateTime)
        {
            writer.Write(dateTime.Ticks);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this BinaryWriter writer, decimal? obj)
        {
            writer.Write(obj.HasValue);
            if (obj.HasValue)
                writer.Write(obj.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this BinaryWriter writer, Guid? obj)
        {
            writer.Write(obj.HasValue);
            if (obj.HasValue)
                writer.Write(obj.Value);
        }
        #endregion 写入器扩展
    }

    /// <summary>
    /// 带变化物品返回值的类的接口。
    /// </summary>
    public abstract class ChangeItemsWorkDatasBase : ComplexWorkDatasBase
    {
        protected ChangeItemsWorkDatasBase([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        protected ChangeItemsWorkDatasBase([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        protected ChangeItemsWorkDatasBase([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        private List<ChangeItem> _ChangeItems;
        /// <summary>
        /// 工作后，物品变化数据。
        /// 不同操作自行定义该属性内的内容。
        /// </summary>
        public List<ChangeItem> ChangeItems { get => _ChangeItems ??= new List<ChangeItem>(); }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _ChangeItems = null;
                base.Dispose(disposing);
            }
        }
    }

}
