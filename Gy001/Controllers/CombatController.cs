using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GY2021001BLL;
using GY2021001DAL;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

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
            StartCombatData data = new StartCombatData()
            {
                GameChar = world.CharManager.GetUsreFromToken(GameHelper.FromBase64String(model.Token))?.GameChars[0],
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
                GameChar = world.CharManager.GetUsreFromToken(GameHelper.FromBase64String(model.Token))?.GameChars[0],
                Template = world.ItemTemplateManager.GetTemplateFromeId(GameHelper.FromBase64String(model.DungeonId)),
                GameItems = model.GameItems.Cast<GameItem>(),
            };
            world.CombatManager.EndCombat(result);
            return (CombatEndReturnDto)result;
        }

        [HttpPost]
        public ActionResult<bool> Test()
        {
            var world = HttpContext.RequestServices.GetService<VWorld>();
            string pwd = "123456";
            var loginName = world.CharManager.QuicklyRegister(ref pwd).LoginName;
            var gu = world.CharManager.Login(loginName, pwd, "");
            var sd = new StartCombatData()
            {
                GameChar = gu.GameChars[0],
                Template = world.ItemTemplateManager.Id2Template.Values.First(c => Convert.ToInt32(c.Properties.GetValueOrDefault("sec", -2m)) == -1),
            };
            world.CombatManager.StartCombat(sd);
            return true;
        }
    }
}