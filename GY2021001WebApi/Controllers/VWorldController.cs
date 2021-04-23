using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GY2021001BLL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GY2021001WebApi.Controllers
{
    /// <summary>
    /// 世界服务器的相关操作。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class VWorldController : ControllerBase
    {
        public VWorldController()
        {

        }

        /// <summary>
        /// 获取服务器实际运行的时间。
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<TimeSpan> GetStartTime()
        {
            var world = HttpContext.RequestServices.GetService(typeof(VWorld)) as VWorld;
            return world.GetServiceTime();
        }

        /// <summary>
        /// 通告服务器下线。
        /// </summary>
        /// <param name="admin"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        /// <response code="401">管理员账号或密码错误。</response>
        [HttpPost]
        public ActionResult NotifyShutdown(string admin, string pwd)
        {
            if (admin != "gy001" || pwd != "guangyuan123")
                return Unauthorized();
            var world = HttpContext.RequestServices.GetService(typeof(VWorld)) as VWorld;
            world.NotifyShutdown();
            return Ok();
        }

        /// <summary>
        /// 这是一个字典(Dictionary)传输的示例。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public Dictionary<string, float> Test(Dictionary<string, float> model)
        {
            return new Dictionary<string, float>()
            {
                { "return0", 100.1f },
                //{ "return1", 10.1f },
            };
        }
    }
}