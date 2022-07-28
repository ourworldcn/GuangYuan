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
        /// 获取指定用户的主基地(初始地块)对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns>没有找到则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetMainbase(this GameChar gameChar) =>
            gameChar.GetHomeland()?.GetAllChildren()?.FirstOrDefault(c => c.GetDikuaiIndex() == 0);

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
        /// 获取所有主基地模板。
        /// </summary>
        /// <param name="templateManager"></param>
        /// <returns>钻石计价的价格，如果没有价格则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<GameItemTemplate> GetMainBaseTemplates(this GameItemTemplateManager templateManager) => templateManager.GetTemplates(c => c.CatalogNumber == 0100);

        /// <summary>
        /// 同步锁。
        /// </summary>
        private static readonly object ThisLocker = new object();

        /// <summary>
        /// 缓存所有地块信息。
        /// </summary>
        private static ConcurrentDictionary<ValueTuple<int, int>, GameItemTemplate> _AllDikuai;

        /// <summary>
        /// 获取所有地块模板的字典。
        /// </summary>
        /// <param name="manager"></param>
        /// <returns>键二元值类型元组(风格号,地块索引号)，值是地块的模板对象。</returns>
        public static ConcurrentDictionary<ValueTuple<int, int>, GameItemTemplate> GetAllDikuai(this GameItemTemplateManager manager)
        {
            if (_AllDikuai is null)
                lock (ThisLocker)
                    if (_AllDikuai is null)
                    {
                        var coll = manager.GetTemplates(c => c.CatalogNumber / 100 == 1).Select(c => KeyValuePair.Create((c.GetFenggeNumber(), c.GetDikuaiIndex()), c));
                        _AllDikuai = new ConcurrentDictionary<(int, int), GameItemTemplate>(coll);
                        Debug.Assert(_AllDikuai.Keys.All(c => c.Item1 != 0), "风格号不应为0");
                    }
            return _AllDikuai;
        }

        /// <summary>
        /// 获取指定风格号的所有模板对象。
        /// </summary>
        /// <param name="fenggeNumbers"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<GameItemTemplate> GetTemplatesByFenggeNumber(this GameItemTemplateManager manager, int fenggeNumber) =>
           manager.GetAllDikuai().Values.Where(c => c.GetFenggeNumber() == fenggeNumber);

        /// <summary>
        /// 获取指定地块号的模板。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="index">地块号，0是主基地，第一个地块是1，以此类推。</param>
        /// <returns>指定地块号的所有风格的模板。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<GameItemTemplate> GetTemplatesByDikuaiIndex(this GameItemTemplateManager manager, int index) =>
            manager.GetAllDikuai().Values.Where(c => c.GetDikuaiIndex() == index);

        /// <summary>
        /// 按地块索引和风格号返回模板。如果没有找到则返回null。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="fenggeNumber"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItemTemplate GetTemplateByNumberAndIndex(this GameItemTemplateManager manager, int fenggeNumber, int index) =>
            manager.GetAllDikuai().GetValueOrDefault((fenggeNumber, index), null);

        /// <summary>
        /// 获取模板的风格号，如果没有风格号则返回-1.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFenggeNumber(this GameItemTemplate template) => template.CatalogNumber / 100 == 1 ? template.Sequence : -1;

        /// <summary>
        /// 获取模板的地块索引号，如果不是地块则返回-1.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetDikuaiIndex(this GameItemTemplate template) => template.CatalogNumber == 101 ? template.Sequence : -1;

        /// <summary>
        /// 返回用户当前有多少地块，包含主基地。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetDikuaiCount(this GameChar gameChar) =>
            gameChar.GetHomeland().Children.Count(c => (c.GetTemplate()?.GetDikuaiIndex() ?? 0) >= 0);

        /// <summary>
        /// 获得地块信息。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<(int, GameItem)> GetDikuais(this GameChar gameChar) =>
            gameChar.GetHomeland().Children.Where(c => c.IsDikuai()).
                Select(c => (c.GetDikuaiIndex(), c));

        /// <summary>
        /// 获取指定对象是否是地块。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="dikuaiIndex">当是地块时，返回地块的索引号。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDikuai(this GameItem gameItem) =>
            gameItem.GetDikuaiIndex() >= 0;

        /// <summary>
        /// 获取该物品的地块号。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns>地块号，如果不是地块则返回-1。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetDikuaiIndex(this GameItem gameItem) => gameItem.GetTemplate()?.GetDikuaiIndex() ?? -1;

        /// <summary>
        /// 获取模板的风格号，如果不是风格则返回-1。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFenggeNumber(this GameItem gameItem) => gameItem.GetTemplate()?.GetFenggeNumber() ?? -1;

        /// <summary>
        /// 获取角色当前的风格号。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCurrentFenggeNumber(this GameChar gameChar) =>
            gameChar.GetHomeland().Children.FirstOrDefault(c => c.GetDikuaiIndex() == 0)?.GetFenggeNumber() ?? -1;

        #endregion 基础功能

        /// <summary>
        /// 可移动的物品GIds。
        /// </summary>
        private static readonly int[] moveableGIds = new int[] { 11, 40, 41 };


        #region 方案的缓存与存储
        /// <summary>
        /// 获取指定id的物品是否是可移动物品。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="hint">加速搜索的字典。</param>
        /// <returns></returns>
        public static bool IsMoveable(Guid id, Dictionary<Guid, GameItem> hint)
        {
            var gameItem = hint.GetValueOrDefault(id);
            if (gameItem is null)   //若不是家园内物品
                return false;
            if (!moveableGIds.Contains(gameItem.GetCatalogNumber()))  //若不可移动
                return false;
            return true;
        }

        /// <summary>
        /// 获取当前激活的风格号，若没有激活风格则返回-1。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetActiveStyleNumber(this GameChar gameChar)
        {
            return (int)gameChar.GetHomeland().Properties.GetDecimalOrDefault("HomelandActiveNumber", 100);
        }

        #endregion 方案的缓存与存储

    }
}