using GY2021001BLL.Homeland;
using GY2021001DAL;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using OwGame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class GameItemManager : GameManagerBase<GameItemManagerOptions>, IGameThingHelper, IGameItemHelper
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
        GameItemTemplateManager _ItemTemplateManager;
        GameItemTemplateManager ItemTemplateManager { get => _ItemTemplateManager ??= World.ItemTemplateManager; }
        #endregion

        /// <summary>
        /// 按照指定模板Id创建一个对象。
        /// </summary>
        /// <param name="templateId">创建事物所需模板Id。</param>
        /// <param name="ownerId">指定一个父Id,如果不指定或为null则忽略。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameItem CreateGameItem(Guid templateId, Guid? ownerId = null)
        {
            var template = World.ItemTemplateManager.GetTemplateFromeId(templateId);
            if (null == template)
                throw new ArgumentException($"找不到指定Id的模板对象Id={templateId}", nameof(templateId));
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
                Template = template,
                OwnerId = ownerId,
                //Count = 1,
            };
            //初始化属性
            var gitm = World.ItemTemplateManager;
            foreach (var item in template.Properties)   //复制属性
            {
                if (item.Value is decimal[] seq)   //若是属性序列
                {
                    var indexPn = gitm.GetIndexPropName(template, item.Key);
                    var lv = Convert.ToInt32(template.Properties.GetValueOrDefault(indexPn, 0m));
                    result.Properties[item.Key] = seq[Math.Clamp(lv, 0, seq.Length - 1)];
                }
                else if (item.Key.Equals("count", StringComparison.InvariantCultureIgnoreCase)) //若是指定初始数量
                    result.Count = Convert.ToDecimal(item.Value);
                else
                    result.Properties[item.Key] = item.Value;
            }
            if (template.SequencePropertyNames.Length > 0 && !result.Properties.Keys.Any(c => c.StartsWith(GameThingTemplateBase.LevelPrefix))) //若需追加等级属性
                result.Properties[GameThingTemplateBase.LevelPrefix] = 0m;
            result.Count ??= template.TryGetPropertyValue("stc", out _) ? 0 : 1;
            if (result.Properties.Count > 0)    //若需要改写属性字符串。
                result.PropertiesString = OwHelper.ToPropertiesString(result.Properties);   //改写属性字符串
            //递归初始化容器
            result.Children.AddRange(template.ChildrenTemplateIds.Select(c => CreateGameItem(c)));
            try
            {
                var dirty = Options?.ItemCreated?.Invoke(Services, result) ?? false;
            }
            catch (Exception)
            {
            }
#if DEBUG
            result.Properties["tname"] = template.DisplayName;
#endif
            result.InvokeCreated(Services);
            return result;
        }

        #region 坐骑相关

        /// <summary>
        /// 创建一个坐骑或野生动物。
        /// </summary>
        /// <param name="head"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public GameItem CreateMounts(GameItemTemplate headTemplate, GameItemTemplate bodyTemplate)
        {
            var result = CreateGameItem(ProjectConstant.ZuojiZuheRongqi);
            result.Count = 1;
            var head = CreateGameItem(headTemplate);
            head.Count = 1;
            SetHead(result, head);
            var body = CreateGameItem(bodyTemplate);
            body.Count = 1;
            SetBody(result, body);
            return result;
        }

        /// <summary>
        /// 按现有对象的信息创建一个坐骑。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns>要创建的坐骑。</returns>
        public GameItem CreateMounts(GameItem gameItem)
        {
            var result = CreateGameItem(ProjectConstant.ZuojiZuheRongqi);
            var head = CreateGameItem(GetHead(gameItem).TemplateId); head.Count = 1;
            SetHead(result, head);
            var body = CreateGameItem(GetBody(gameItem).TemplateId); body.Count = 1;
            SetBody(result, body);
            foreach (var item in gameItem.Properties)   //复制属性
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }

        /// <summary>
        /// 物品是否是一个坐骑。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMounts(GameItem gameItem) => gameItem.TemplateId == ProjectConstant.ZuojiZuheRongqi;

        /// <summary>
        /// 获取头对象。
        /// </summary>
        /// <param name="mounts"></param>
        /// <returns>返回头对象，如果没有则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameItem GetHead(GameItem mounts)
        {
            var result = mounts.Children.FirstOrDefault(c => World.ItemTemplateManager.GetTemplateFromeId(c.TemplateId).GenusCode == 3);
#pragma warning disable CS0618 // 类型或成员已过时
            return result ?? mounts.Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.ZuojiZuheTou)?.Children?.FirstOrDefault();
#pragma warning restore CS0618 // 类型或成员已过时
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetHead(GameItem mounts, GameItem head)
        {
#pragma warning disable CS0618 // 类型或成员已过时
            var slot = mounts.Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.ZuojiZuheTou);
#pragma warning restore CS0618 // 类型或成员已过时
            return null != slot ? ForcedAdd(head, slot) : ForcedAdd(head, mounts);
        }

        /// <summary>
        /// 获取身体对象。
        /// </summary>
        /// <param name="mounts"></param>
        /// <returns>身体对象，不是坐骑或没有身体则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameItem GetBody(GameItem mounts)
        {
            var result = mounts.Children.FirstOrDefault(c => World.ItemTemplateManager.GetTemplateFromeId(c.TemplateId).GenusCode == 4);
#pragma warning disable CS0618 // 类型或成员已过时
            return result ?? mounts.Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.ZuojiZuheShenti)?.Children?.FirstOrDefault();
#pragma warning restore CS0618 // 类型或成员已过时

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetBody(GameItem mounts, GameItem body)
        {
#pragma warning disable CS0618 // 类型或成员已过时
            var slot = mounts.Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.ZuojiZuheShenti);
#pragma warning restore CS0618 // 类型或成员已过时
            return null != slot ? ForcedAdd(body, slot) : ForcedAdd(body, mounts);
        }

        /// <summary>
        /// 返回坐骑头和身体的模板Id。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="gim"></param>
        /// <returns>返回(头模板Id,身体模板Id),若不是坐骑则返回(<see cref="Guid.Empty"/>,<see cref="Guid.Empty"/>)。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (Guid, Guid) GetMountsTIds(GameItem gameItem) => (GetHead(gameItem)?.TemplateId ?? Guid.Empty, GetBody(gameItem)?.TemplateId ?? Guid.Empty);

        #endregion 坐骑相关


        /// <summary>
        /// 获取对象的模板。
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns>如果无效的模板Id，则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameItemTemplate GetTemplate(GameThingBase gameObject)
        {
            return ItemTemplateManager.GetTemplateFromeId(gameObject.TemplateId);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="tId"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            //TO DO
            if (propName.Equals("tid", StringComparison.InvariantCultureIgnoreCase))
            {
                gameItem.TemplateId = (Guid)val;
                return true;
            }
            else if (propName.Equals("pid", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!OwHelper.TryGetGuid(val, out var pid))
                    return false;
                gameItem.ParentId = pid;
                return true;
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
                var olv = gameItem.GetDecimalOrDefault(propName, 0m);    //当前等级
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
                gameItem.Properties[seqPName] = seq[newLevel] + val - oov;
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
        public bool MoveItem(GameItem item, decimal count, GameThingBase destContainer, ICollection<ChangesItem> changesItems = null)
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

            var stc = GetStackUpper(item);
            if (stc is null || count == item.Count)  //若不可堆叠或全部移动
            {
                var parent = GetContainer(item);   //获取父容器
                MoveItems(parent, c => c.Id == item.Id, destContainer, changesItems);
                result = true;
            }
            else //若可能堆叠且不是全部移动
            {
                stc = stc == -1 ? decimal.MaxValue : stc;
                var moveItem = CreateGameItem(item.TemplateId);
                moveItem.Count = count;
                item.Count -= count;
                var parent = GetContainer(item);   //获取源父容器
                AddItem(moveItem, destContainer);  //TO DO 需要处理无法完整放入问题
#if DEBUG
                var gc = moveItem.GameChar;
                var state = gc.GameUser.DbContext.Entry(moveItem).State;
#endif
                if (null != changesItems)
                {
                    //增加变化 
                    changesItems.AddToChanges(item.ParentId ?? item.OwnerId.Value, item);
                    //增加新增
                    changesItems.AddToAdds(moveItem.ParentId ?? moveItem.OwnerId.Value, moveItem);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取指定物品堆叠上限。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns>null不可堆叠，-1表示无上限。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal? GetStackUpper(GameThingBase gameItem)
        {
            var _ = GetTemplate(gameItem);
            if (!_.Properties.TryGetValue(ProjectConstant.StackUpperLimit, out object obj) || !(obj is decimal count))
                if (!gameItem.TryGetPropertyValue(ProjectConstant.StackUpperLimit, out obj) || !OwHelper.TryGetDecimal(obj, out count))
                    return null;
            return count;
        }

        /// <summary>
        /// 获取指定容器数量。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns>如果不是容器将返回null,-1表示没有容量限制。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal? GetCapacity(GameThingBase container)
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
        public int GetMaxCapacity(GameThingBase container)
        {
            if (!container.Properties.TryGetValue(ProjectConstant.ContainerCapacity, out object obj))   //若没有属性,视同非容器
                return 0;
            if (!OwHelper.TryGetDecimal(obj, out var result))
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
        public int GetFreeCapacity(GameThingBase container)
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
        /// 获取物品堆叠的空余数量。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="stc">堆叠的限制数</param>
        /// <returns>堆叠空余数量，不可堆叠将返回0，不限制将返回<see cref="decimal.MaxValue"/></returns>
        public decimal GetNumberOfStackRemainder(GameItem gameItem, out decimal? stc)
        {
            stc = GetStackUpper(gameItem);
            if (stc is null)    //若不可堆叠
                return decimal.Zero;
            if (-1 == stc)  //若不限制堆叠数量
                return decimal.MaxValue;
            return Math.Max(0, stc.Value - gameItem.Count.Value);
        }

        #endregion 动态属性相关

        #region 物品增减相关

        /// <summary>
        /// 将符合条件的物品对象及其子代从容器中移除并返回。
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="filter"></param>
        /// <param name="removes">变化的数据。可以是null或省略，此时忽略。</param>
        public void RemoveItemsWhere(GameThingBase parent, Func<GameItem, bool> filter, ICollection<GameItem> removes = null)
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
        public void MoveItems(GameThingBase src, Func<GameItem, bool> filter, GameThingBase dest, ICollection<ChangesItem> changes = null)
        {
            var tmp = World.ObjectPoolListGameItem.Get();
            var adds = World.ObjectPoolListGameItem.Get();
            var remainder = World.ObjectPoolListGameItem.Get();
            var remainder2 = World.ObjectPoolListGameItem.Get();
            try
            {
                //移动物品
                RemoveItemsWhere(src, filter, tmp); //移除物品
                var removeIds = tmp.Select(c => c.Id).ToArray();  //移除物品的Id集合
                AddItems(tmp, dest, remainder, changes);
                //已经增加的物品数据
                var addChanges = (new ChangesItem()
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

                    var change = new ChangesItem()
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
                    var _ = new ChangesItem()
                    {
                        ContainerId = src.Id,
                    };
                    _.Removes.AddRange(removeIds);
                    changes?.Add(_);
                }
            }
            finally
            {
                World.ObjectPoolListGameItem.Return(remainder2);
                World.ObjectPoolListGameItem.Return(remainder);
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
        public void AddItems(IEnumerable<GameItem> gameItems, GameThingBase parent, ICollection<GameItem> remainder = null, ICollection<ChangesItem> changeItems = null)
        {
            foreach (var item in gameItems) //TO DO 性能优化未做
            {
                AddItem(item, parent, remainder, changeItems);
            }
        }

        /// <summary>
        /// 将一个物品放入容器。根据属性确定是否可以合并堆叠。
        /// </summary>
        /// <param name="gameItem">要放入的物品，返回时属性<see cref="GameItem.Count"/>可能被更改。</param>
        /// <param name="parent">容器事物对象，或是一个角色对象。</param>
        /// <param name="remainder">追加不能放入的物品到此集合，可以是null或省略，此时忽略。</param>
        /// <param name="changeItems">变化的数据。可以是null或省略，此时忽略。
        /// 基于堆叠限制和容量限制，无法放入的部分。实际是<paramref name="gameItem"/>对象或拆分后的对象集合，对于可堆叠对象可能修改了<see cref="GameItem.Count"/>属性。若没有剩余则返回null。
        /// </param>
        /// <returns>放入后的对象，如果是不可堆叠或堆叠后有剩余则是 <paramref name="gameItem"/>和堆叠对象，否则是容器内原有对象。返回空集合，因容量限制没有放入任何物品。</returns>
        public void AddItem(GameItem gameItem, GameThingBase parent, ICollection<GameItem> remainder = null, ICollection<ChangesItem> changeItems = null)
        {
            IList<GameItem> children = (parent as GameItem)?.Children; //获取容器的接口
            children ??= (parent as GameChar)?.GameItems;    //容器
            Debug.Assert(null != children);
            var stc = GetStackUpper(gameItem);
            if (stc is null) //若不可堆叠
            {
                gameItem.Count ??= 1;
                var upper = GetCapacity(parent) ?? -1;    //TO DO 暂时未限制是否是容器

                if (-1 != upper && children.Count >= upper)  //若超过容量
                {
                    remainder?.Add(gameItem);
                    return;
                }
                var succ = ForcedAdd(gameItem, parent);
                changeItems?.AddToAdds(parent.Id, gameItem);
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
                var dest = children.FirstOrDefault(c => c.TemplateId == gameItem.TemplateId && GetNumberOfStackRemainder(c, out _) > 0);    //找到已有的物品且尚可加入堆叠的
                if (null != dest)  //若存在同类物品
                {
                    var redCount = Math.Min(GetNumberOfStackRemainder(dest, out _), gameItem.Count ?? 0);   //移动的数量
                    dest.Count = dest.Count.Value + redCount;
                    gameItem.Count -= redCount;
                    changeItems?.AddToChanges(parent.Id, dest);
                    result.Add(dest);
                }
                if (gameItem.Count <= 0)   //若已经全部堆叠进入
                {
                    return;
                }
                else  //放入剩余物品,容错
                {
                    var tmp = new List<GameItem>();
                    SplitItem(gameItem, tmp);
                    var _ = new ChangesItem(parent.Id);
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
                //当有剩余物品
                return;
            }
        }

        /// <summary>
        /// 按堆叠要求将物品拆分未多个。
        /// </summary>
        /// <param name="gameItem">要拆分的物品。返回时该物品<see cref="GameItem.Count"/>可能被改变。
        /// 若未设置数量，则对不可堆叠物品视同0，可堆叠物品视同1。</param>
        /// <param name="results">拆分后的物品。可能包含<paramref name="gameItem"/>，且其<see cref="GameItem.Count"/>属性被修正。
        /// <paramref name="gameItem"/>总是被放在第一个位置上。</param>
        public void SplitItem(GameItem gameItem, ICollection<GameItem> results)
        {
            var stc = GetStackUpper(gameItem);
            if (stc is null) //若不可堆叠
            {
                gameItem.Count ??= 1;
                var count = gameItem.Count.Value - 1;   //记录原始数量少1的值
                gameItem.Count = Math.Min(gameItem.Count.Value, 1); //设置第一堆的数量
                results.Add(gameItem);
                for (; count >= 0; count--)   //分解出多余物品
                {
                    var item = CreateGameItem(gameItem.TemplateId);
                    item.Count = Math.Min(1, count);
                    results.Add(item);
                }
            }
            else if (-1 == stc.Value)  //若无堆叠上限限制
            {
                results.Add(gameItem);
            }
            else //若可堆叠且有限制
            {
                gameItem.Count ??= 0;
                var count = gameItem.Count.Value - stc.Value;   //记录当前数量减去第一堆以后的数量
                gameItem.Count = Math.Min(gameItem.Count.Value, stc.Value);    //取当前数量，和允许最大堆叠数量中较小的值
                results.Add(gameItem);  //加入第一个堆
                while (count > 0) //当需要拆分
                {
                    var item = CreateGameItem(gameItem.TemplateId);
                    item.Count = Math.Min(count, stc.Value); //取当前剩余数量，和允许最大堆叠数量中较小的值
                    results.Add(item);
                    count -= stc.Value;
                }
            }
        }

        /// <summary>
        /// 无视容量限制堆叠规则。将物品加入指定容器。
        /// </summary>
        /// <param name="gameItem">无视容量限制堆叠规则，不考虑原有容器。</param>
        /// <param name="container">无视容量限制堆叠规则。</param>
        /// <param name="db">使用的数据库上下文。</param>
        /// <returns>true成功加入.false <paramref name="container"/>不是可以容纳物品的类型。</returns>
        public bool ForcedAdd(GameItem gameItem, GameThingBase container, DbContext db = null)
        {
            if (container is GameChar gameChar)  //若容器是角色
            {
                gameItem.GenerateIdIfEmpty();
                gameChar.GameItems.Add(gameItem);
                gameItem.OwnerId ??= gameChar.Id;
            }
            else if (container is GameItem item)
            {
                gameItem.GenerateIdIfEmpty();
                item.Children.Add(gameItem);
                gameItem.ParentId ??= item.Id;
                gameItem.Parent ??= item;
            }
            else
                return false;
            return true;
        }

        /// <summary>
        /// 强制移动物品。无视容量限制堆叠规则。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="container"></param>
        /// <returns>true成功移动，false未知原因没有移动成功。当前不可能失败。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ForceMove(GameItem gameItem, GameThingBase container)
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
            var gc = gameItem.GameChar;   //保存所属角色
            bool result = ForceRemove(gameItem);
            if (result)   //若成功移除关系
            {
                db ??= gc?.GameUser?.DbContext;
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
                tmp.Template = tt;
                adds.Clear();
                tmp.Children.ApartWithWithRepeated(tt.ChildrenTemplateIds, c => c.TemplateId, c => c, null, null, adds);
                foreach (var addItem in adds)
                {
                    var newItem = gim.CreateGameItem(addItem);
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
        public bool GetItems(IEnumerable<Guid> ids, ICollection<GameItem> items, GameThingBase parent)
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
        /// <returns>返回父容器可能是另一个物品或角色对象，没有找到则返回null。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="gameItem"/>是null。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameThingBase GetContainer(GameItem gameItem)
        {
            return gameItem.Parent as GameThingBase ?? (gameItem.OwnerId is null ? null : World.CharManager.GetCharFromId(gameItem.OwnerId.Value));
        }

        /// <summary>
        /// 获取容器的子对象的集合接口。
        /// </summary>
        /// <param name="gameThing">容器对象。</param>
        /// <returns>子代容器的接口，null表示没有找到。特别地，当参数是null时也会返回null而不引发异常。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IList<GameItem> GetChildrenCollection(GameThingBase gameThing)
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
        public IEnumerable<GameItem> GetAllChildren(GameThingBase gameThing)
        {
            var _ = GetChildrenCollection(gameThing);
            return OwHelper.GetAllSubItemsOfTree(_, c => c.Children);
        }

        public void ChangeTemplate(GameItem container, GameItemTemplate newContainer)
        {
            throw new NotImplementedException();
        }
    }

    public class ActiveStyleDatas
    {
        public ActiveStyleDatas()
        {
        }

        public HomelandFangan Fangan { get; set; }

        public string Message { get; set; }
        public bool HasError { get; set; }

        public List<ChangesItem> ItemChanges { get; set; }
        public GameChar GameChar { get; set; }
    }

    public static class GameItemManagerExtensions
    {
        /// <summary>
        /// 在指定集合中寻找指定模板Id的第一个对象。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="parent"></param>
        /// <param name="templateId"></param>
        /// <param name="msg"></param>
        /// <returns>找到的第一个对象，null没有找到，msg给出提示信息。</returns>
        public static GameItem FindFirstOrDefault(this GameItemManager manager, IEnumerable<GameItem> parent, Guid templateId, out string msg)
        {
            var result = parent.FirstOrDefault(c => c.TemplateId == templateId);
            if (result is null)
                msg = $"找不到指定模板Id的物品，TemplateId={templateId}";
            else
                msg = null;
            return result;
        }

        /// <summary>
        /// 可移动的物品GIds。
        /// </summary>
        static int[] moveableGIds = new int[] { 11, 40, 41 };

        static public void ActiveStyle(this GameItemManager manager, ActiveStyleDatas datas)
        {
            var gcManager = manager.Services.GetRequiredService<GameCharManager>();
            if (!gcManager.Lock(datas.GameChar.GameUser))
            {
                datas.HasError = true;
                datas.Message = "无法锁定用户。";
            }
            try
            {
                var gitm = manager.Services.GetRequiredService<GameItemTemplateManager>();
                var hl = datas.GameChar.GetHomeland();
                var builderBag = hl.Children.First(c => c.TemplateId == ProjectConstant.HomelandBuilderBagTId);
                var dic = datas.Fangan.FanganItems.SelectMany(c => c.ItemIds).Distinct().Join(hl.AllChildren, c => c, c => c.Id, (l, r) => r).ToDictionary(c => c.Id);
                dic[hl.Id] = hl;    //包括家园
                foreach (var item in datas.Fangan.FanganItems)
                {
                    var destParent = dic.GetValueOrDefault(item.ContainerId);
                    if (destParent is null)  //若找不到目标容器
                        continue;
                    //将可移动物品收回包裹（除捕获竿）
                    manager.MoveItems(destParent, c => c.GetCatalogNumber() != 11 && moveableGIds.Contains(c.GetCatalogNumber()), builderBag, datas.ItemChanges);
                    //改变容器模板
                    var container = dic.GetValueOrDefault(item.ContainerId);
                    if (container is null)  //若找不到容器对象
                        continue;
                    if (item.NewTemplateId.HasValue) //若需要改变容器模板
                    {
                        var newContainer = gitm.GetTemplateFromeId(item.NewTemplateId.Value);
                        manager.ChangeTemplate(container, newContainer);
                    }
                    foreach (var id in item.ItemIds)    //添加物品
                    {
                        var gameItem = dic.GetValueOrDefault(id);
                        if (gameItem is null)   //若不是家园内物品
                            continue;
                        if (!moveableGIds.Contains(gameItem.GetCatalogNumber()))  //若不可移动
                            continue;
                        manager.MoveItems(manager.GetContainer(gameItem), c => c.Id == gameItem.Id, destParent, datas.ItemChanges);
                    }
                }
            }
            catch (Exception err)
            {
                datas.HasError = true;
                datas.Message = err.Message;
            }
            finally
            {
                gcManager.Unlock(datas.GameChar.GameUser, true);
            }
        }


    }
}
