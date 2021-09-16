using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using System;
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
            result.Initialize(manager.Service, containerTId);

            var head = new GameItem();
            head.Initialize(manager.Service, headTId);
            manager.SetHead(result, head);

            var body = new GameItem();
            body.Initialize(manager.Service, bodyTId);
            manager.SetBody(result, body);

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
    }


}
