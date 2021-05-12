using GY2021001BLL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gy001.Controllers
{

    /// <summary>
    /// 外围系统。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BlueprintController : ControllerBase
    {
        private readonly VWorld _World;
        public BlueprintController(VWorld world)
        {
            _World = world;
        }

        /// <summary>
        /// 测试。
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<bool> Test()
        {
            return Ok();
        }
    }
}
