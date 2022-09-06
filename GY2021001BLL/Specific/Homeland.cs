/*
 * 特定与家园相关的代码
 */

using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Game.Item;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GuangYuan.GY001.BLL.Homeland
{
    /// <summary>
    /// 关于家园的扩展方法封装类。
    /// </summary>
    public static class HomelandExtensions
    {
        #region 基础功能

        /// <summary>
        /// 获取指定用户的家园中主控室对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns>没有找到则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetMainControlRoom(this GameChar gameChar) =>
            gameChar.GetHomeland().GetAllChildren().FirstOrDefault(c => c.ExtraGuid == ProjectConstant.MainControlRoomSlotId);


        /// <summary>
        /// 获取家园对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns>如果没有找到，可能返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetHomeland(this GameChar gameChar) => gameChar.GameItems.FirstOrDefault(c => c.ExtraGuid == ProjectConstant.HomelandSlotId);

        /// <summary>
        /// 获取风格背包。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns>如果没有找到，可能返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetFenggeBag(this GameChar gameChar) => gameChar.GetHomeland()?.Children.FirstOrDefault(c => c.ExtraGuid == ProjectConstant.HomelandStyleBagTId);

        /// <summary>
        /// 获取建筑背包。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetHomelandBuildingBag(this GameChar gameChar) => gameChar.GetHomeland().Children.FirstOrDefault(c => c.ExtraGuid == ProjectConstant.HomelandBuildingBagTId);

        /// <summary>
        /// 获取指定对象是否是地块。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="dikuaiIndex">当是地块时，返回地块的索引号。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDikuai(this GameItem gameItem) =>
            gameItem.GetTemplate().CatalogNumber == (int)ThingGId.家园地块_战斗 / 1000;

        #endregion 基础功能

        /// <summary>
        /// 可移动的物品GIds。
        /// </summary>
        private static readonly int[] moveableGIds = new int[] { 11, 40, 41 };


        #region 方案的缓存与存储

        #endregion 方案的缓存与存储

    }
}