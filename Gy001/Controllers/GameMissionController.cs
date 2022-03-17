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

        /// <summary>
        /// 获取任务状态。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        public ActionResult<GetMissionStateReturnDto> GetMissionState(GetMissionStateParamsDto model)
        {
            var result = new GetMissionStateReturnDto();
            using GetMissionStateDatas datas = new GetMissionStateDatas(World, model.Token) { };
            datas.TIds.AddRange(model.TIds.Select(c => OwConvert.ToGuid(c)));
            World.MissionManager.GetMissionState(datas);
            result.HasError = datas.HasError;
            result.ErrorCode = datas.ErrorCode;
            result.DebugMessage = datas.ErrorMessage;
            if (!result.HasError)
            {
                result.TIds.AddRange(datas.TIds.Select(c => c.ToBase64String()));
                result.State.AddRange(datas.State.Select(c => (int)c));
            }
            return result;
        }

        /// <summary>
        /// 标记完成任务。
        /// </summary>
        /// <param name="model">参见 CompleteMissionParamsDto。</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CompleteMissionReturnDto> Complete(CompleteMissionParamsDto model)
        {
            var result = new CompleteMissionReturnDto();
            using var datas = new MissionCompleteDatas(World, model.Token)
            {
                MissionTId = OwConvert.ToGuid(model.MissionTId),
            };
            World.MissionManager.Complete(datas);
            result.HasError = datas.HasError;
            result.ErrorCode = datas.ErrorCode;
            result.DebugMessage = datas.ErrorMessage;
            if (!result.HasError)
            {
                result.ChangesItems.AddRange(datas.ChangeItems.Select(c => (ChangesItemDto)c));
                result.MailIds.AddRange(datas.MailIds.Select(c => c.ToBase64String()));
            }
            return result;
        }

        /// <summary>
        /// 获取任务模板数据。由于不会变化，会自动缓存(2分钟)。
        /// </summary>
        /// <returns><seealso cref="GetMissionTemplatesReturnDto"/>。</returns>
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 120)]
        [HttpGet]
        public ActionResult<GetMissionTemplatesReturnDto> GetMissionTemplates()
        {
            var result = new GetMissionTemplatesReturnDto { };
            var templates = World.MissionManager.GetMissionTemplates();
            result.Templates.AddRange(templates.Select(c => (GameMissionTemplateDto)c));
            return result;
        }
    }

}
