/*
 * 特定与家园相关的代码
 */

using GY2021001DAL;
using Gy2021001Template;
using Microsoft.Extensions.DependencyInjection;
using OwGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GY2021001BLL
{
    /// <summary>
    /// 风格。
    /// </summary>
    public class HomelandFengge
    {
        public HomelandFengge()
        {

        }

        /// <summary>
        /// 风格号。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 下属方案对象集合。
        /// </summary>
        public List<HomelandFangan> Fangans { get; } = new List<HomelandFangan>();

        /// <summary>
        /// 客户端记录一些额外信息。服务器不使用。
        /// 记录在风格对象的 ClientString 上。
        /// </summary>
        public string ClientString { get; set; }
    }

    /// <summary>
    /// 方案对象。
    /// </summary>
    public class HomelandFangan
    {
        public HomelandFangan()
        {

        }

        /// <summary>
        /// 唯一Id，暂时无用，但一旦生成则保持不变。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 下属具体加载物品及其位置信息
        /// </summary>
        public List<HomelandFanganItem> FanganItems { get; } = new List<HomelandFanganItem>();

        /// <summary>
        /// 该方案是否被激活。
        /// </summary>
        public bool IsActived { get; set; }

        /// <summary>
        /// 客户端记录一些额外信息。服务器不使用。
        /// </summary>
        public string ClientString { get; set; }

    }

    /// <summary>
    /// 方案中的子项。
    /// </summary>
    public class HomelandFanganItem
    {
        public HomelandFanganItem()
        {

        }

        /// <summary>
        /// 要加入 ContainerId 指出容器的子对象Id。
        /// </summary>
        public List<Guid> ItemIds { get; } = new List<Guid>();

        /// <summary>
        /// 容器的Id。
        /// </summary>
        public Guid ContainerId { get; set; }

        /// <summary>
        /// 要替换的新的模板Id值。空表示不替换。
        /// </summary>
        public Guid? NewTemplateId { get; set; }

        /// <summary>
        /// 客户端记录一些额外信息。服务器不使用。
        /// </summary>
        public string ClientString { get; set; }

    }

    /// <summary>
    /// 关于家园的扩展方法封装类。
    /// </summary>
    public static class HomelandExtensions
    {
        /// <summary>
        /// 获取模板的风格号，如果没有风格号则返回0.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public int GetFenggeNumber(this GameItemTemplate template) => template.Sequence;

        /// <summary>
        /// 获取该物品的价格，指钻石计价的价格。
        /// </summary>
        /// <param name="templat"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public decimal? GetPriceWithDiamond(this GameItemTemplate templat) =>
            templat.TryGetPropertyValue("sd", out var sdObj) && OwHelper.TryGetDecimal(sdObj, out var sd) ? new decimal?(sd) : null;

        /// <summary>
        /// 获取这个模板指出的物品是否是免费的（钻石计价）。
        /// </summary>
        /// <param name="templat"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool GetIsFree(this GameItemTemplate templat) =>
            templat.TryGetPropertyValue("sd", out var sdObj) && OwHelper.TryGetDecimal(sdObj, out var sd) && sd > 0 ? false : true;

        /// <summary>
        /// 获取所有主基地模板。
        /// </summary>
        /// <param name="templateManager"></param>
        /// <returns>钻石计价的价格，如果没有价格则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public IEnumerable<GameItemTemplate> GetMainBaseTemplates(this GameItemTemplateManager templateManager) => templateManager.GetTemplates(c => c.CatalogNumber == 0100);

        /// <summary>
        /// 获取数据库中所有免费的风格号。
        /// TO DO 未来可能加缓存机制。
        /// </summary>
        /// <param name="templateManager"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<int> GetFenggeNumbersWithFree(this GameItemTemplateManager templateManager) =>
           templateManager.GetMainBaseTemplates().Where(c => c.GetIsFree()) //免费的
                .Select(c => c.GetFenggeNumber()).Distinct();

        /// <summary>
        /// 获取指定风格号的所有模板对象。
        /// TO DO 未来可能增加缓存机制。
        /// </summary>
        /// <param name="fenggeNumbers"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public IEnumerable<GameItemTemplate> GetTemplates(this GameItemTemplateManager manager, int fenggeNumber) =>
            manager.GetTemplates(c => c.CatalogNumber == 0100 && c.Sequence == fenggeNumber);

        /// <summary>
        /// 获取指定风格号的风格对象。
        /// </summary>
        /// <param name="services">用到的服务容器。</param>
        /// <param name="gameChar">根据当前解锁的地块生成方案对象。</param>
        /// <param name="fenggeNumber">方案号。</param>
        /// <returns></returns>
        public static HomelandFengge GetFenggeObject(this IServiceProvider services, GameChar gameChar, int fenggeNumber)
        {
            var gitm = services.GetService<GameItemTemplateManager>();
            var gim = services.GetService<GameItemManager>();

            var result = new HomelandFengge()   //风格
            {
                Id = fenggeNumber,
            };
            var coll = gitm.GetTemplates(fenggeNumber).ToArray(); //该风格的所有相关模板
            for (int i = 0; i < 2; i++) //方案
            {
                HomelandFangan fangan = new HomelandFangan()    //方案
                {
                    Id = Guid.NewGuid(),
                };
                foreach (var item in coll)    //方案项
                {

                }
                result.Fangans.Add(fangan);
            }
            return result;
        }

        public static GameItem Find(this GameItemManager manager, IEnumerable<GameItem> parent, Guid templateId, out string msg)
        {
            var result = parent.FirstOrDefault(c => c.TemplateId == templateId);
            if (result is null)
                msg = $"找不到指定模板Id的物品，TemplateId={templateId}";
            else
                msg = null;
            return result;
        }
    }
}