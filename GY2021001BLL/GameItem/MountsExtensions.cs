using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
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
    public static class MountsExtensions
    {

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
            return bag.Children.Any(c => manager.GetHeadTemplate(c)?.Id == htid || manager.GetBodyTemplate(c)?.Id == btid);
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
        /// 从指定字典的数据创建物品对象。
        /// tid=模板id,count=数量,htid=头模板id,btid=身体模板id。这些键前可能添加前缀<paramref name="prefix"/>。
        /// htid和btid要成对出现。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="dic"></param>
        /// <param name="prefix">前缀字符串，默认值是空字符串，表示不考虑前缀。</param>
        /// <returns></returns>
        public static GameItem CreateItemFromDictionary(this GameItemManager manager, IReadOnlyDictionary<string, object> dic, string prefix = "")
        {
            GameItem result;
            var tid = dic.GetGuidOrDefault($"{prefix}tid", Guid.Empty);
            if (tid == Guid.Empty)
                throw new ArgumentException("缺少模板id键值对。", nameof(dic));
            var count = dic.GetDecimalOrDefault($"{prefix}count");
            var htid = dic.GetGuidOrDefault($"{prefix}htid", Guid.Empty);
            var btid = dic.GetGuidOrDefault($"{prefix}btid", Guid.Empty);
            if (htid != Guid.Empty && btid != Guid.Empty)   //若创建生物
            {
                result = manager.CreateMounts(htid, btid, tid);
            }
            else
            {
                result = new GameItem();
                manager.World.EventsManager.GameItemCreated(result,tid);
            }
            if (count == 0)    //若需要设置数量
                if (result.IsStc(out _))    //若可以堆叠
                    result.Count = 0;
                else
                    result.Count = 1;
            else //若明确指定了数量
                result.Count = count;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="gameItem"></param>
        /// <returns></returns>
        public static GameItem Clone(this GameItemManager manager, GameItem gameItem)
        {
            var result = new GameItem();
            manager.World.EventsManager.GameItemCreated(result, gameItem.TemplateId);
            foreach (var item in gameItem.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
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
    }


}
