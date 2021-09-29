using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;

namespace GY2021001WebApi.Controllers
{
    /// <summary>
    /// 战斗相关的操作。这里的功能以后可能依据需求改为Udp。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CombatController : ControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CombatController()
        {

        }

        /// <summary>
        /// 通知服务器，客户端开始一场战斗。
        /// </summary>
        /// <param name="model">参见 CombatStartParamsDto</param>
        /// <returns>参见 CombatStartReturnDto</returns>
        [HttpPost]
        public ActionResult<CombatStartReturnDto> Start(CombatStartParamsDto model)
        {
            var world = HttpContext.RequestServices.GetService<VWorld>();
            var cbm = world.CombatManager;
            using var data = new StartCombatData(world, model.Token)
            {
                Template = world.ItemTemplateManager.GetTemplateFromeId(GameHelper.FromBase64String(model.DungeonId)),
            };
            cbm.StartCombat(data);
            return (CombatStartReturnDto)data;
        }

        /// <summary>
        /// 通知服务器客户端结束了一场战斗。
        /// 如果正常进入下一关，可以不必调用启动战斗的接口。
        /// </summary>
        /// <param name="model">参见 CombatEndParamsDto</param>
        /// <returns>参见 CombatEndReturnDto</returns>
        [HttpPost]
        public ActionResult<CombatEndReturnDto> End(CombatEndParamsDto model)
        {
            var world = HttpContext.RequestServices.GetService<VWorld>();
            var result = new EndCombatData()
            {
                GameChar = world.CharManager.GetUserFromToken(GameHelper.FromBase64String(model.Token))?.CurrentChar,
                Template = world.ItemTemplateManager.GetTemplateFromeId(GameHelper.FromBase64String(model.DungeonId)),
                EndRequested = model.EndRequested,
                OnlyMark = model.OnlyMark,
                IsWin=model.IsWin,
            };
            if (null != model.GameItems)
                result.GameItems.AddRange(model.GameItems.Select(c => (GameItem)c));
            world.CombatManager.EndCombat(result);
            return (CombatEndReturnDto)result;
        }

#if DEBUG
        [HttpPost]
        public ActionResult<CombatEndReturnDto> Test()
        {
            return null;
        }
#endif

        /// <summary>
        /// PVP战斗结算。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CombatEndPvpReturnDto> CombatEndPvp(CombatEndPvpParamsDto model)
        {
            var result = new CombatEndPvpReturnDto();
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            using var datas = new EndCombatPvpWorkData(world, model.Token, GameHelper.FromBase64String(model.OtherGCharId))
            {
                Now = DateTime.UtcNow,
                DungeonId = GameHelper.FromBase64String(model.DungeonId),
                IsWin = model.IsWin,
            };
            datas.DestroyTIds.AddRange(model.Destroies.Select(c => (ValueTuple<Guid, decimal>)c));
            world.CombatManager.EndCombatPvp(datas);
            result.HasError = datas.HasError;
            result.DebugMessage = datas.ErrorMessage;
            result.ChangesItems.AddRange(datas.ChangeItems.Select(c => (ChangesItemDto)c));
            return result;
        }
    }

}