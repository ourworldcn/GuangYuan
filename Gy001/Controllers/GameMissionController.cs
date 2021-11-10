﻿using GuangYuan.GY001.BLL;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using OW.Game;
using OW.Game.Mission;
using System;
using System.Linq;

namespace Gy001.Controllers
{
    /// <summary>
    /// 任务/成就相关操作。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class GameMissionController : GameBaseController
    {
        public GameMissionController(VWorld world) : base(world)
        {
        }

        /// <summary>
        /// 获取指定任务/成就的奖励。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult<GetMissionRewardReturnDto> GetMissionReward(GetMissionRewardParamsDto model)
        {
            var result = new GetMissionRewardReturnDto();
            using var datas = new GetRewardingDatas(World, model.Token)
            {
            };
            datas.ItemIds.AddRange(model.ItemIds.Select(c => OwConvert.ToGuid(c)));
            World.MissionManager.GetRewarding(datas);
            result.HasError = datas.HasError;
            result.ErrorCode = datas.ErrorCode;
            result.DebugMessage = datas.ErrorMessage;
            if (!result.HasError)
            {
                result.ChangesItems.AddRange(datas.ChangeItems.Select(c => (ChangesItemDto)c));
                result.MailIds.AddRange(datas.MailIds.Select(c => c.ToBase64String()));
            }
            if (result.ErrorCode == ErrorCodes.ERROR_INVALID_TOKEN)
                return Unauthorized(result.DebugMessage);
            return result;
        }

    }
}
