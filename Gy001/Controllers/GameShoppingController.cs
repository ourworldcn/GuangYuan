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
        /// 获取所有商品模板数据。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        public ActionResult<GetAllShoppingTemplatesResultDto> GetAllShoppingTemplates(GetAllShoppingTemplatesParamsDto model)
        {
             var result = new GetAllShoppingTemplatesResultDto();
            var coll = World.ShoppingManager.GetAllShoppingTemplates();
            result.Templates.AddRange(coll.Select(c=>(ShoppingItemDto)c));
            return result;
        }

        /// <summary>
        /// 获取销售商品的列表。无效物品不会返回。
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        public ActionResult<GetListResultDto> GetList(GetListParamsDto model)
        {
            GetListResultDto result = new GetListResultDto();
            var datas = new GetListDatas(World, model.Token)
            {
                UserDbContext = HttpContext.RequestServices.GetService<GY001UserContext>(),
            };
            OwHelper.SafeCopy(model.Genus, datas.Genus);
            World.ShoppingManager.GetList(datas);
            result.FillFrom(datas);
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
                UserDbContext = HttpContext.RequestServices.GetRequiredService<GY001UserContext>(),
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
                UserDbContext = HttpContext.RequestServices.GetRequiredService<GY001UserContext>()
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
        /// 获取指定时间点有效的卡池信息。按要求仅获取一等奖信息。
        /// </summary>
        /// <param name="model"><seealso cref="GetCurrentCardPoolParamsDto"/></param>
        /// <returns><seealso cref="GetCurrentCardPoolReturnDto"/></returns>
        [HttpPut]
        public ActionResult<GetCurrentCardPoolReturnDto> GetCurrentCardPool(GetCurrentCardPoolParamsDto model)
        {
            var result = new GetCurrentCardPoolReturnDto();
            var coll = World.ShoppingManager.GetCurrentCardPoolsCore(model.NowUtc);
            result.Templates.AddRange(coll.Where(c => c.SubCardPoolString == "0").Select(c => (GameCardTemplateDto)c));
            return result;
        }

        /// <summary>
        /// 抽奖接口。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LotteryReturnDto> Lottery(LotteryParamsDto model)
        {
            using var datas = new ChoujiangDatas(World, model.Token)
            {
                LotteryTypeCount1 = model.LotteryTypeCount1,
                LotteryTypeCount10 = model.LotteryTypeCount10,
                CardPoolId = model.CardPoolId,
            };
            World.ShoppingManager.Choujiang(datas);
            var result = new LotteryReturnDto();
            if (!datas.HasError)
            {
                result.ChangesItems.AddRange(datas.ChangeItems.Select(c => (ChangesItemDto)c));
                result.TemplateIds.AddRange(datas.ResultTemplateIds.Select(c => c.ToBase64String()));
                result.ResultItems.AddRange(datas.ResultItems.Select(c => (GameItemDto)c));
            }
            result.FillFrom(datas);
            return result;
        }

        #endregion 卡池相关
    }

}
