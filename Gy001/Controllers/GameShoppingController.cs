using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using System;
using System.Linq;

namespace Gy001.Controllers
{
    /// <summary>
    /// 商城相关操作。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class GameShoppingController : GameBaseController
    {
        public GameShoppingController(VWorld world) : base(world)
        {
        }

        /// <summary>
        /// 获取销售商品的列表。
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        public ActionResult<GetListResultDto> GetList(GetListParamsDto model)
        {
            GetListResultDto result = new GetListResultDto();
            var datas = new GetListDatas(World, model.Token)
            {
                Genus = model.Genus,
                UserContext = HttpContext.RequestServices.GetService<GY001UserContext>(),
            };
            World.ShoppingManager.GetList(datas);
            result.HasError = datas.HasError;
            result.ErrorCode = datas.ErrorCode;
            result.DebugMessage = datas.ErrorMessage;
            if (!result.HasError)
            {
                result.ShoppingItems.AddRange(datas.ShoppingTemplates.Select(c => ShoppingItemDto.FromGameShoppingTemplate(c, datas.GameChar, datas.Now, World)));
                result.RefreshCost.AddRange(datas.RefreshGold.Select(c => new StringDecimalTuple { Item1 = c.Item1, Item2 = c.Item2 }));
            }
            return result;
        }

        /// <summary>
        /// 购买商品。
        /// 签到礼包的总签到次数在GameChar的动态属性中，键名Day30Count。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<BuyResultDto> Buy(BuyParamsDto model)
        {
            BuyResultDto result = new BuyResultDto();
            using var datas = new BuyDatas(World, model.Token)
            {
                UserContext = HttpContext.RequestServices.GetRequiredService<GY001UserContext>(),
                ShoppingId = OwConvert.ToGuid(model.ShoppingId),
                Count = model.Count,
            };
            World.ShoppingManager.Buy(datas);
            result.HasError = datas.HasError;
            result.ErrorCode = datas.ErrorCode;
            result.DebugMessage = datas.ErrorMessage;
            if (!result.HasError)
            {
                result.ChangesItems.AddRange(datas.ChangeItems.Select(c => (ChangesItemDto)c));
            }
            return result;
        }

        /// <summary>
        /// 刷新商城随机商品的接口。
        /// </summary>
        /// <param name="model">无特别参数。</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<RefreshShopReturnDto> Refresh(RefreshShopParamsDto model)
        {
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            var result = new RefreshShopReturnDto();
            var db = HttpContext.RequestServices.GetRequiredService<GY001UserContext>();
            using var datas = new RefreshDatas(World, model.Token)
            {
                UserContext = HttpContext.RequestServices.GetRequiredService<GY001UserContext>()
            };
            World.ShoppingManager.Refresh(datas);
            result.HasError = datas.HasError;
            result.ErrorCode = datas.ErrorCode;
            result.DebugMessage = datas.ErrorMessage;
            if (!result.HasError)
            {
                result.ChangesItems.AddRange(datas.ChangeItems.Select(c => (ChangesItemDto)c));
            }
            return result;
        }

        #region 卡池相关

        /// <summary>
        /// 获取指定时间点有效的卡池信息。
        /// </summary>
        /// <param name="model"><seealso cref="GetCurrentCardPoolParamsDto"/></param>
        /// <returns><seealso cref="GetCurrentCardPoolReturnDto"/></returns>
        [HttpPut]
        public ActionResult<GetCurrentCardPoolReturnDto> GetCurrentCardPool(GetCurrentCardPoolParamsDto model)
        {
            var result = new GetCurrentCardPoolReturnDto();
            var coll = World.ShoppingManager.GetCurrentCardPoolsCore(model.NowUtc);
            result.Templates.AddRange(coll.Select(c => (GameCardTemplateDto)c));
            return result;
        }

        #endregion 卡池相关
    }

}
