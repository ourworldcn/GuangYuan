using GuangYuan.GY001.BLL.Specific;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using OW.Game;
using OW.Game.Log;
using OW.Game.Mission;
using OW.Game.PropertyChange;
using System;
using System.Collections.Generic;
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
                var mapper = World.GetMapper();
                result.ChangesItems.AddRange(datas.ChangeItems.Select(c => mapper.Map(c)));
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
            result.MissionTId = model.MissionTId;
            result.FillFrom(datas);
            if (!result.HasError)
            {
                var mapper = World.GetMapper();
                datas.PropertyChanges.CopyTo(datas.ChangeItems);
                result.ChangesItems.AddRange(datas.ChangeItems.Select(c => mapper.Map(c)));
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

        /// <summary>
        /// 获取行会任务。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        public ActionResult<GetGuildMissionReturnDto> GetGuildMission(GetGuildMissionParamsDto model)
        {
            var result = new GetGuildMissionReturnDto();
            var gu = World.CharManager.GetUserFromToken(OwConvert.ToGuid(model.Token));
            if (gu is null)
            {
                result.FillFromWorld();
                return result;
            }
            var gc = gu.CurrentChar;
            var guild = World.AllianceManager.GetGuild(gc);
            if (guild is null)
            {
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = "角色不在行会中。";
                return result;
            }
            if (!World.AllianceManager.Lock(guild.Id, World.AllianceManager.Options.DefaultTimeout, out guild))
            {
                result.FillFromWorld();
                return result;
            }
            using var dwGuild = DisposeHelper.Create(c => World.AllianceManager.Unlock(c), guild);
            if (!World.CharManager.Lock(gu))
            {
                result.FillFromWorld();
                return result;
            }
            var now = DateTime.UtcNow;
            using var dw = DisposeHelper.Create(c => World.CharManager.Unlock(c), gu);
            var collDone = World.MissionManager.GetGuildMission(gc);    //已完成任务
            var coll = World.AllianceManager.GetMissionOrCreate(guild, now); //工会任务

            result.GuildMissions.AddRange(coll.Select(c => new GuildMissionDto() { GuildTemplateId = Guid.Parse(c.Params[0]).ToBase64String() })); //已完成任务
            result.CharDones.AddRange(collDone.Select(c => new GuildMissionDto() { GuildTemplateId = Guid.Parse(c.Params[0]).ToBase64String() })); //在当前公会未完成的任务
            return result;
        }
    }

}
