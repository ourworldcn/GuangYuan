using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GY2021001BLL;
using Gy2021001Template;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        /// 获取所有模板。
        /// </summary>
        /// <returns>所有模板的集合。</returns>
        [HttpGet][ResponseCache(Location =ResponseCacheLocation.Any,Duration =600)]
        public ActionResult<List<GameItemTemplateDto>> GetTemplates()
        {
            var gitm = HttpContext.RequestServices.GetRequiredService<GameItemTemplateManager>();
            return gitm.Id2Template.Values.Select(c => (GameItemTemplateDto)c).ToList();
        }

        /// <summary>
        /// 获取资源服务器地址。
        /// </summary>
        /// <returns></returns>
        /// <response code="201">调用过于频繁。</response>
        [HttpGet]
        public ActionResult<string> GetResourceServerUrl()
        {
            var config = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            string str = config.GetValue<string>("ResourceServerUrl");
            return str;
        }
    }
}