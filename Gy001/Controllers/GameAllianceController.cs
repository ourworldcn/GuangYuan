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
        public GameAllianceController()
        {
        }

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
        public ActionResult SendGuild()
        {
            return NotFound();
        }

        /// <summary>
        /// 解散行会。
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public ActionResult Delete()
        {
            return NotFound();
        }
        #endregion 行会级管理

        #region 行会信息

        /// <summary>
        /// 获取行会信息。
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        public ActionResult GetGuild()
        {
            return NotFound();
        }
        #endregion 行会信息

        #region 行会人事管理功能

        /// <summary>
        /// 修改权限。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ModifyPermissions()
        {
            return NotFound();
        }

        /// <summary>
        /// 批准入会申请。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AccepteGuildMember()
        {
            return NotFound();
        }

        /// <summary>
        /// 移除指定的行会成员。
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public ActionResult RemoveGuildMember()
        {
            return NotFound();
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

}
