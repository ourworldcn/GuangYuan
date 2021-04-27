using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
            return Ok();
        }

        /// <summary>
        /// 通知服务器客户端结束了一场战斗。
        /// </summary>
        /// <param name="model">参见 CombatEndParamsDto</param>
        /// <returns>参见 CombatEndReturnDto</returns>
        [HttpPost]
        public ActionResult<CombatEndReturnDto> End(CombatEndParamsDto model)
        {
            return Ok();
        }
    }
}