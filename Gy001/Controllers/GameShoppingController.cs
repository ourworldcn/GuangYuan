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
            }
            return result;
        }

        /// <summary>
        /// 购买商品。
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
                ShoppingId=OwConvert.ToGuid( model.ShoppingId),
                Count=model.Count,
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
    }

}
