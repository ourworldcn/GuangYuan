using GuangYuan.GY001.UserDb.Social;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OW.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gy001.Controllers
{
    /// <summary>
    /// 联盟/行会相关操作。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class GameAllianceController : GameBaseController
    {
        public GameAllianceController(VWorld world) : base(world)
        {
        }
        #region 行会相关功能

        #region 行会级管理

        /// <summary>
        /// 创建行会。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CreateGuildReturnDto> CreateGuild(CreateGuildParamsDto model)
        {
            using var datas = new CreateGuildContext(World, model.Token)
            {
                DisplayName = model.DisplayName
            };
            World.AllianceManager.CreateGuild(datas);

            var result = new CreateGuildReturnDto();
            result.FillFrom(datas);
            if (!result.HasError)
            {
                var guild = World.AllianceManager.GetGuild(datas.Id);
                result.Guild = guild;
                GameGuildDto.FillMembers(guild, result.Guild, World);

                result.Changes.AddRange(datas.Changes.Select(c => (GamePropertyChangeItemDto)c));
            }
            return result;
        }

        /// <summary>
        /// 转移行会。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<SendGuildReturnDto> SendGuild(SendGuildParamsDto model)
        {
            using var datas = new SendGuildContext(World, model.Token, OwConvert.ToGuid(model.OtherCharId));
            World.AllianceManager.SendGuild(datas);
            var result = new SendGuildReturnDto();
            result.FillFrom(datas);
            return result;
        }

        /// <summary>
        /// 解散行会。
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public ActionResult<DeleteGuildReturnDto> DeleteGuild(DeleteGuildParamsDto model)
        {
            using var datas = new DeleteGuildContext(World, model.Token);
            World.AllianceManager.DeleteGuild(datas);
            var result = new DeleteGuildReturnDto();
            result.FillFrom(datas);
            return result;
        }
        #endregion 行会级管理

        #region 行会信息

        /// <summary>
        /// 获取行会信息。
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        public ActionResult<GetGuildReturnDto> GetGuild(GetGuildParamsDto model)
        {
            using var datas = new GetGuildContext(World, model.Token);
            World.AllianceManager.GetGuild(datas);
            var result = new GetGuildReturnDto();
            result.FillFrom(datas);
            if (!result.HasError)
                GameGuildDto.FillMembers(datas.Guild, result.Guild, World);
            return result;
        }
        #endregion 行会信息

        #region 行会人事管理功能

        /// <summary>
        /// 申请加入工会。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<RequestJoinGuildReturnDto> RequestJoinGuild(RequestJoinGuildParamsDto model)
        {
            using var datas = new RequestJoinContext(World, model.Token)
            {
                GuildId = OwConvert.ToGuid(model.GuildId)
            };
            World.AllianceManager.RequestJoin(datas);
            var result = new RequestJoinGuildReturnDto();
            result.FillFrom(datas);
            return result;
        }

        /// <summary>
        /// 批准入会申请。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<AccepteGuildMemberReturnDto> AccepteGuildMember(AccepteGuildMemberParamsDto model)
        {
            using var datas = new AcceptJoinContext(World, model.Token);
            datas.CharIds.AddRange(model.CharIds.Select(c => OwConvert.ToGuid(c)));
            World.AllianceManager.AcceptJoin(datas);
            var result = new AccepteGuildMemberReturnDto();
            result.FillFrom(datas);
            return result;
        }

        /// <summary>
        /// 移除指定的行会成员。
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public ActionResult<RemoveGuildMemberReturnDto> RemoveGuildMember(RemoveGuildMemberParamsDto model)
        {
            using var datas = new RemoveMembersContext(World, model.Token);
            datas.CharIds.AddRange(model.CharIds.Select(c => OwConvert.ToGuid(c)));
            World.AllianceManager.RemoveMembers(datas);
            var result = new RemoveGuildMemberReturnDto();
            result.FillFrom(datas);
            return result;
        }

        /// <summary>
        /// 修改权限。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ModifyPermissionsReturnDto> ModifyPermissions(ModifyPermissionsParamsDto modle)
        {
            using var datas = new ModifyPermissionsContext(World, modle.Token)
            {
                Division = modle.Division
            };
            OwHelper.Copy(modle.CharIds.Select(c => OwConvert.ToGuid(c)), datas.CharIds);
            World.AllianceManager.ModifyPermissions(datas);
            var result = new ModifyPermissionsReturnDto();
            result.FillFrom(datas);
            return result;
        }

        #endregion 行会人事管理功能

        #region 行会养成

        /// <summary>
        /// 行会捐献。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SupportGuild()
        {
            return NotFound();
        }

        /// <summary>
        /// 行会或建筑升级。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult UpgradeGuild()
        {
            return NotFound();
        }
        #endregion 行会养成

        #region 行会功能
        //任务会尽量扩展现有任务系统，购买肯定是扩展现有商城系统。
        #endregion 行会功能

        #endregion 行会相关功能
    }

    public class ModifyPermissionsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要修改成的权限。10=普通会员，14=管理。
        /// </summary>
        public int Division { get; set; }

        /// <summary>
        /// 要修改的角色id集合。
        /// </summary>
        public List<string> CharIds { get; set; } = new List<string>();
    }

}
