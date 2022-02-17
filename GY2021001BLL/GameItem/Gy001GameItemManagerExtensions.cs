using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using OW.Extensions.Game.Store;
using OW.Game;
using OW.Game.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GuangYuan.GY001.BLL
{
    /// <summary>
    /// 针对坐骑/野兽相关的扩展。
    /// </summary>
    public static class Gy001GameItemManagerExtensions
    {
        /// <summary>
        /// 用字典中的属性创建一组对象。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="bag"></param>
        /// <param name="prefix">键的前缀，省略或为null表示没有前缀。</param>
        /// <returns></returns>
        public static IEnumerable<GameItem> ToGameItems(this GameItemManager manager, IReadOnlyDictionary<string, object> bag, string prefix = null)
        {
            var props = bag.GetValuesWithoutPrefix(prefix);
            var dics = props.Select(c => c.ToDictionary(c2 => c2.Item1, c2 => c2.Item2));
            var eventMng = manager.World.EventsManager;
            List<GameItem> result = new List<GameItem>();
            foreach (var item in dics)
            {
                var gi = new GameItem();
                eventMng.GameItemCreated(gi, item);
                result.Add(gi);
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="gameItems"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static Dictionary<string, object> ToDictioary(this GameItemManager manager, IEnumerable<GameItem> gameItems, string prefix = null)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            var count = gameItems.Count();
            var eventMng = manager.World.EventsManager;
            if (count == 1)
            {
                eventMng.Copy(gameItems.First(), result, prefix);
            }
            else
            {
                int index = 1;
                foreach (var item in gameItems)
                {
                    eventMng.Copy(gameItems.First(), result, prefix, (index++).ToString());
                }
            }
            return result;
        }

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
        private static readonly int[] moveableGIds = new int[] { 11, 40, 41 };

        /// <summary>
        /// 激活风格。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="datas"></param>
        public static void ActiveStyle(this GameItemManager manager, ActiveStyleDatas datas)
        {
            var gcManager = manager.Service.GetRequiredService<GameCharManager>();
            if (!gcManager.Lock(datas.GameChar.GameUser))
            {
                datas.HasError = true;
                datas.Message = "无法锁定用户。";
            }
            try
            {
                var gitm = manager.Service.GetRequiredService<GameItemTemplateManager>();
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
                    if (item.NewTemplateId.HasValue && item.NewTemplateId != Guid.Empty) //若需要改变容器模板
                    {
                        var newContainer = gitm.GetTemplateFromeId(item.NewTemplateId.Value);
                        container.ChangeTemplate(newContainer);
                    }
                    if (destParent.IsDikuai())   //若容器是地块
                        foreach (var id in item.ItemIds)    //添加物品
                        {
                            var gameItem = dic.GetValueOrDefault(id);
                            if (gameItem is null)   //若不是家园内物品
                                continue;
                            if (!moveableGIds.Contains(gameItem.GetCatalogNumber()))  //若不可移动
                                continue;
                            manager.ForceMove(gameItem, destParent);
                            //manager.MoveItems(manager.GetContainer(gameItem), c => c.Id == gameItem.Id, destParent, datas.ItemChanges);
                        }
                }
                manager.World.CharManager.NotifyChange(datas.GameChar.GameUser);
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

        /// <summary>
        /// 获取指定双亲符合条件的图鉴。
        /// </summary>
        /// <param name="mng"></param>
        /// <param name="gameChar">角色对象。</param>
        /// <param name="t1Body">第一个身体模板。</param>
        /// <param name="t2Body">第二个身体模板。</param>
        /// <returns>符合要求的模板输出的值元组 (头模板Id,身体模板Id,概率)。没有找到图鉴可能返回空。</returns>
        public static (Guid, Guid, decimal)? GetTujianResult(this GameItemManager mng, GameChar gameChar, GameItemTemplate t1Body, GameItemTemplate t2Body)
        {
            var gitm = mng.Service.GetRequiredService<GameItemTemplateManager>();
            var tujianBag = gameChar.GetTujianBag(); //图鉴背包
            var tujian = tujianBag.Children.FirstOrDefault(c => //图鉴
            {
                var bd1 = c.GetTemplate().Properties.GetDecimalOrDefault("hbab");
                var bd2 = c.GetTemplate().Properties.GetDecimalOrDefault("hbbb");
                return bd1 == t1Body.GId && bd2 == t2Body.GId || bd2 == t1Body.GId && bd1 == t2Body.GId;
            });
            if (tujian is null)
                return null;
            var hTId = tujian.Properties.GetGuidOrDefault("outheadtid", Guid.Empty);
            var bTId = tujian.Properties.GetGuidOrDefault("outbodytid", Guid.Empty);
            var prob = tujian.Properties.GetDecimalOrDefault("hbsr");
            return (hTId, bTId, prob);
        }

        /// <summary>
        /// 用物品信息填充简要信息。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        public static void Fill(this GameItemManager manager, GameItem source, GameItemSummery dest)
        {
            if (source.Id != Guid.Empty)
                dest.Id = source.Id;
            dest.TemplateId = source.TemplateId;
            dest.Count = source.Count;
            foreach (var item in source.Properties)
                dest.Properties[item.Key] = item.Value;
        }

        /// <summary>
        /// 用简要信息填充物品信息。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        public static void Fill(this GameItemManager manager, GameItemSummery source, GameItem dest)
        {
            if (source.Id != Guid.Empty)
                dest.Id = source.Id;
            dest.TemplateId = source.TemplateId;
            dest.Count = source.Count;
            foreach (var item in source.Properties)
                dest.Properties[item.Key] = item.Value;
        }

        #region 获取特定对象的快捷方式

        /// <summary>
        /// 获取孵化槽。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public static GameItem GetFuhuaSlot(this GameChar gameChar) =>
            gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.FuhuaSlotTId);

        /// <summary>
        /// 获取道具背包。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public static GameItem GetItemBag(this GameChar gameChar) =>
            gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.DaojuBagSlotId);

        /// <summary>
        /// 获取时装背包。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public static GameItem GetShizhuangBag(this GameChar gameChar) =>
            gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ShizhuangBagSlotId);

        /// <summary>
        /// 获取坐骑背包。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetZuojiBag(this GameChar gameChar) =>
            gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ZuojiBagSlotId);

        /// <summary>
        /// 获取兽栏对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public static GameItem GetShoulanBag(this GameChar gameChar) =>
            gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ShoulanSlotId);

        /// <summary>
        /// 获取神纹背包。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public static GameItem GetShenwenBag(this GameChar gameChar) =>
            gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ShenWenBagSlotId);

        /// <summary>
        /// 按指定Id获取坐骑。仅从坐骑包中获取。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="id">坐骑的唯一Id。</param>
        /// <returns>如果没有找到则返回null。</returns>
        public static GameItem GetMounetsFromId(this GameChar gameChar, Guid id) =>
            gameChar.GetZuojiBag()?.Children.FirstOrDefault(c => c.Id == id);

        /// <summary>
        /// 获取图鉴背包。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public static GameItem GetTujianBag(this GameChar gameChar) =>
            gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.TujianBagTId);

        #endregion 获取特定对象的快捷方式

        public static void Fill(this GameItemManager manager, GameItem source, GY001GameItemSummery dest)
        {
            manager.Fill(source, dest as GameItemSummery);
            if (source.IsIncludeChildren())
            {
                dest.BodyTId = manager.GetBody(source)?.TemplateId;
                dest.HeadTId = manager.GetHead(source)?.TemplateId;
            }
        }

        public static void Fill(this GameItemManager manager, GY001GameItemSummery source, GameItem dest)
        {
            manager.Fill(source as GameItemSummery, dest);
            if (source.BodyTId.HasValue && source.HeadTId.HasValue)  //若是生物
            {
                var body = new GameItem()
                {
                    TemplateId = source.BodyTId.Value,
                };
                dest.Children.Add(body);

                var head = new GameItem()
                {
                    TemplateId = source.HeadTId.Value,
                };
                dest.Children.Add(head);
            }
        }

        /// <summary>
        /// 创建一个坐骑或野生动物。
        /// </summary>
        /// <param name="manager">管理器。</param>
        /// <param name="headTemplate"></param>
        /// <param name="bodyTemplate"></param>
        /// <returns></returns>
        public static GameItem CreateMounts(this GameItemManager manager, GameItemTemplate headTemplate, GameItemTemplate bodyTemplate) =>
            manager.CreateMounts(headTemplate.Id, bodyTemplate.Id, ProjectConstant.ZuojiZuheRongqi);

        /// <summary>
        /// 根据指定模板新建一个坐骑对象。
        /// </summary>
        /// <param name="manager">管理器。</param>
        /// <param name="headTId">头模板。</param>
        /// <param name="bodyTId">身体模板。</param>
        /// <param name="containerTId">容器模板。</param>
        /// <returns></returns>
        public static GameItem CreateMounts(this GameItemManager manager, Guid headTId, Guid bodyTId, Guid containerTId)
        {
            var result = new GameItem();
            manager.World.EventsManager.GameItemCreated(result, containerTId, null, null, new Dictionary<string, object>()
            {
                {"htid",headTId },
                {"btid",bodyTId },
            });

            return result;
        }

        /// <summary>
        /// 按现有对象的信息创建一个坐骑。
        /// </summary>
        /// <param name="manager">管理器。</param>
        /// <param name="gameItem"></param>
        /// <param name="containerTId">容器模板Id。</param>
        /// <returns>要创建的坐骑。仅复制容器的属性，头和身体对象是初始属性。</returns>
        public static GameItem CreateMounts(this GameItemManager manager, GameItem gameItem, Guid containerTId)
        {
            var result = manager.CreateMounts(manager.GetHead(gameItem).TemplateId, manager.GetBody(gameItem).TemplateId, containerTId);
            foreach (var item in gameItem.Properties)   //复制容器的属性
                result.Properties[item.Key] = item.Value;
            return result;
        }

        /// <summary>
        /// 克隆一个坐骑对象，用指定的容器模板。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="gameItem"></param>
        /// <param name="containerTId"></param>
        /// <returns>创建一个新的坐骑。复制容器，头和身体对象的属性。</returns>
        public static GameItem CloneMounts(this GameItemManager manager, GameItem gameItem, Guid containerTId)
        {
            var result = manager.CreateMounts(gameItem, containerTId);
            var destHead = manager.GetHead(result);
            var destBody = manager.GetBody(result);
            //设置头属性
            var head = manager.GetHead(gameItem);
            foreach (var item in head.Properties)
                destHead.Properties[item.Key] = item.Value;
            //设置身体属性
            var body = manager.GetBody(gameItem);
            foreach (var item in body.Properties)
                destBody.Properties[item.Key] = item.Value;
            return result;
        }

        /// <summary>
        /// 物品是否是一个坐骑。
        /// </summary>
        /// <param name="manager">管理器。</param>
        /// <param name="gameItem"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMounts(this GameItemManager manager, GameItem gameItem) => gameItem.TemplateId == ProjectConstant.ZuojiZuheRongqi;

        /// <summary>
        /// 获取头对象。
        /// </summary>
        /// <param name="manager">管理器。</param>
        /// <param name="mounts">容器对象。不校验此对象的类型。</param>
        /// <returns>返回头对象，如果没有则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetHead(this GameItemManager manager, GameItem mounts)
        {
            var result = mounts.Children.FirstOrDefault(c => manager.GetTemplateFromeId(c.TemplateId).CatalogNumber == 3);
#pragma warning disable CS0618 // 类型或成员已过时
            return result ?? mounts.Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.ZuojiZuheTou)?.Children?.FirstOrDefault();
#pragma warning restore CS0618 // 类型或成员已过时
        }

        /// <summary>
        /// 设置头对象。
        /// </summary>
        /// <param name="manager">管理器。</param>
        /// <param name="mounts">容器对象。不校验此对象的类型。</param>
        /// <param name="head"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SetHead(this GameItemManager manager, GameItem mounts, GameItem head)
        {
#pragma warning disable CS0618 // 类型或成员已过时
            var slot = mounts.Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.ZuojiZuheTou);
#pragma warning restore CS0618 // 类型或成员已过时
            return null != slot ? manager.ForcedAdd(head, slot) : manager.ForcedAdd(head, mounts);
        }

        /// <summary>
        /// 获取身体对象。
        /// </summary>
        /// <param name="manager">管理器。</param>
        /// <param name="mounts">容器对象。不校验此对象的类型。</param>
        /// <returns>身体对象，不是坐骑或没有身体则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetBody(this GameItemManager manager, GameItem mounts)
        {
            var result = mounts.Children.FirstOrDefault(c => manager.GetTemplateFromeId(c.TemplateId).CatalogNumber == 4);
#pragma warning disable CS0618 // 类型或成员已过时
            return result ?? mounts.Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.ZuojiZuheShenti)?.Children?.FirstOrDefault();
#pragma warning restore CS0618 // 类型或成员已过时

        }

        /// <summary>
        /// 设置身体对象。
        /// </summary>
        /// <param name="manager">管理器。</param>
        /// <param name="mounts">容器对象。不校验此对象的类型。</param>
        /// <param name="body"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SetBody(this GameItemManager manager, GameItem mounts, GameItem body)
        {
#pragma warning disable CS0618 // 类型或成员已过时
            var slot = mounts.Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.ZuojiZuheShenti);
#pragma warning restore CS0618 // 类型或成员已过时
            return null != slot ? manager.ForcedAdd(body, slot) : manager.ForcedAdd(body, mounts);
        }

        /// <summary>
        /// 返回坐骑头和身体的模板Id。
        /// </summary>
        /// <param name="manager">管理器。</param>
        /// <param name="mounts">容器对象。不校验此对象的类型。</param>
        /// <returns>返回(头模板Id,身体模板Id),若不是坐骑则返回(<see cref="Guid.Empty"/>,<see cref="Guid.Empty"/>)。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Guid, Guid) GetMountsTIds(this GameItemManager manager, GameItem mounts) =>
            (manager.GetHead(mounts)?.TemplateId ?? Guid.Empty, manager.GetBody(mounts)?.TemplateId ?? Guid.Empty);

        /// <summary>
        /// 获取物品的头模板。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="item"></param>
        /// <returns>不是动物则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItemTemplate GetHeadTemplate(this GameItemManager manager, GameItem item)
        {
            var tmp = manager.GetHead(item);
            if (tmp is null)
                return null;
            return manager.GetTemplate(tmp);
        }

        /// <summary>
        /// 获取是否是纯种坐骑的指示。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="gameItem"></param>
        /// <returns></returns>
        public static bool IsChunzhongMounts(this GameItemManager manager, GameItem gameItem)
        {
            var ht = manager.GetHeadTemplate(gameItem);
            var bt = manager.GetBodyTemplate(gameItem);
            if (ht is null || bt is null)
                return false;
            return ht.Sequence == bt.Sequence;
        }

        /// <summary>
        /// 获取指定角色是否拥有了指定坐骑。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="gChar"></param>
        /// <param name="gameItem"></param>
        /// <returns></returns>
        public static bool IsExistsMounts(this GameItemManager manager, GameChar gChar, GameItem gameItem)
        {
            var bag = gChar.GetZuojiBag();
            var htid = manager.GetHeadTemplate(gameItem)?.Id;
            var btid = manager.GetBodyTemplate(gameItem)?.Id;
            if (!htid.HasValue || !btid.HasValue)  //若缺少Id存在
                return false;
            return bag.Children.Any(c => manager.GetHeadTemplate(c)?.Id == htid && manager.GetBodyTemplate(c)?.Id == btid);
        }
        /// <summary>
        /// 获取物品的身体模板。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="item"></param>
        /// <returns>不是动物则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItemTemplate GetBodyTemplate(this GameItemManager manager, GameItem item)
        {
            var tmp = manager.GetBody(item);
            if (tmp is null)
                return null;
            return manager.GetTemplate(tmp);
        }

        /// <summary>
        /// 复制一个物品对象。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="gameItem">源对象。</param>
        /// <returns>目标对象。</returns>
        public static GameItem Clone(this GameItemManager manager, GameItem gameItem)
        {
            var result = new GameItem();
            manager.World.EventsManager.GameItemCreated(result, gameItem.TemplateId);
            OwHelper.Copy(gameItem.Properties, result.Properties);
            result.Count = gameItem.Count;
            foreach (var item in gameItem.Children)
            {
                var subItem = manager.Clone(item);
                subItem.Parent = result;
                subItem.ParentId = result.Id;
                result.Children.Add(subItem);
            }
            return result;
        }

        /// <summary>
        /// 复制一个对象。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="srcItem">源对象。</param>
        /// <param name="destItem">目标对象。</param>
        public static void Clone(this GameItemManager manager, GameItem srcItem, GameItem destItem)
        {
            destItem.TemplateId = srcItem.TemplateId;
            destItem.SetTemplate(srcItem.GetTemplate());
            OwHelper.Copy(srcItem.Properties, destItem.Properties);
            destItem.Count = srcItem.Count;
            foreach (var item in srcItem.Children)
            {
                var subItem = manager.Clone(item);
                subItem.Parent = destItem;
                subItem.ParentId = destItem.Id;
                destItem.Children.Add(subItem);
            }
        }

    }


}
